using Microsoft.Extensions.Options;
using PCTama.Controller.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PCTama.Controller.Services;

public class McpClientService : BackgroundService
{
    private readonly ILogger<McpClientService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly McpConfiguration _mcpConfig;
    private readonly Dictionary<string, HttpClient> _mcpClients = new();
    private readonly List<ChatMessage> _chatHistory = new();
    private McpSdkClient? _mcpSdkClient;
    private bool _llmAvailable = false;
    private DateTime _lastLlmCheckTime = DateTime.MinValue;
    private readonly TimeSpan _llmCheckInterval = TimeSpan.FromSeconds(30);

    public McpClientService(
        ILogger<McpClientService> logger,
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _mcpConfig = configuration.GetSection("McpConfiguration").Get<McpConfiguration>() 
            ?? new McpConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MCP Client Service starting...");

        // Initialize MCP clients
        await InitializeMcpClientsAsync(stoppingToken);

        // Main processing loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMcpCycleAsync(stoppingToken);
                await Task.Delay(
                    TimeSpan.FromMilliseconds(_mcpConfig.ProcessingIntervalMs), 
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MCP processing cycle");
            }
        }
    }

    private async Task InitializeMcpClientsAsync(CancellationToken cancellationToken)
    {
        // Initialize MCP SDK client for LLM communication
        if (!string.IsNullOrEmpty(_mcpConfig.LocalLlmEndpoint))
        {
            var llmClient = new HttpClient
            {
                BaseAddress = new Uri(_mcpConfig.LocalLlmEndpoint),
                Timeout = TimeSpan.FromSeconds(_mcpConfig.LlmTimeoutSeconds)
            };
            _mcpSdkClient = new McpSdkClient(
                llmClient, 
                _loggerFactory.CreateLogger<McpSdkClient>(), 
                _mcpConfig.ModelName);
            
            _logger.LogInformation("Initialized MCP SDK client: Endpoint={Endpoint}, Model={Model}, Timeout={Timeout}s",
                _mcpConfig.LocalLlmEndpoint, _mcpConfig.ModelName, _mcpConfig.LlmTimeoutSeconds);
            
            // Check if LLM is available
            await CheckLlmAvailabilityAsync(cancellationToken);
        }
        else
        {
            _logger.LogWarning("No LLM endpoint configured. MCP SDK client not initialized.");
        }

        foreach (var server in _mcpConfig.McpServers.Where(s => s.Enabled))
        {
            try
            {
                var client = server.Name.ToLower() switch
                {
                    "text" => _httpClientFactory.CreateClient("textmcp"),
                    "actor" => _httpClientFactory.CreateClient("actormcp"),
                    _ => new HttpClient { BaseAddress = new Uri(server.Endpoint) }
                };

                _mcpClients[server.Name] = client;
                _logger.LogInformation("Initialized MCP client: {Name} at {Endpoint}", 
                    server.Name, server.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MCP client: {Name}", server.Name);
            }
        }

        // Initialize additional input MCPs
        foreach (var server in _mcpConfig.AdditionalInputMcps.Where(s => s.Enabled))
        {
            try
            {
                var client = new HttpClient { BaseAddress = new Uri(server.Endpoint) };
                _mcpClients[server.Name] = client;
                _logger.LogInformation("Initialized additional MCP client: {Name} at {Endpoint}", 
                    server.Name, server.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize additional MCP client: {Name}", server.Name);
            }
        }
    }

    private async Task ProcessMcpCycleAsync(CancellationToken cancellationToken)
    {
        // Get input from text MCP
        var textInput = await GetTextInputAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(textInput))
        {
            return;
        }

        _logger.LogInformation("Received text input: {Input}", textInput);

        // Process with local LLM (placeholder - integrate with actual MCP SDK)
        var llmResponse = await ProcessWithLlmAsync(textInput, cancellationToken);

        // Send action to actor MCP
        await SendToActorAsync(llmResponse, cancellationToken);
    }

    private async Task<string> GetTextInputAsync(CancellationToken cancellationToken)
    {
        if (!_mcpClients.TryGetValue("text", out var client))
        {
            return string.Empty;
        }

        try
        {
            var response = await client.GetAsync("/api/text/stream", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting text input from MCP");
        }

        return string.Empty;
    }

    private async Task<string> ProcessWithLlmAsync(string input, CancellationToken cancellationToken)
    {
        if (_mcpSdkClient == null)
        {
            _logger.LogWarning("MCP SDK client not initialized. Cannot process input.");
            return string.Empty;
        }

        // Check LLM availability periodically
        if (!_llmAvailable && (DateTime.UtcNow - _lastLlmCheckTime) > _llmCheckInterval)
        {
            await CheckLlmAvailabilityAsync(cancellationToken);
        }

        if (!_llmAvailable)
        {
            _logger.LogDebug("LLM not available. Skipping processing.");
            return string.Empty;
        }

        try
        {
            LlmResponse response;

            if (_mcpConfig.UseChatMode)
            {
                // Use chat mode with conversation history
                _chatHistory.Add(ChatMessage.User(input));

                // Limit chat history
                while (_chatHistory.Count > _mcpConfig.MaxChatHistory)
                {
                    _chatHistory.RemoveAt(0);
                }

                // Build messages with system prompt
                var messages = new List<ChatMessage>();
                if (!string.IsNullOrEmpty(_mcpConfig.PromptConfig.SystemPrompt))
                {
                    messages.Add(ChatMessage.System(_mcpConfig.PromptConfig.SystemPrompt));
                }
                messages.AddRange(_chatHistory);

                response = await _mcpSdkClient.ChatAsync(messages, cancellationToken);

                if (response.Success)
                {
                    _chatHistory.Add(ChatMessage.Assistant(response.Text));
                }
            }
            else
            {
                // Use simple generate mode
                var prompt = string.IsNullOrEmpty(_mcpConfig.PromptConfig.UserPromptTemplate)
                    ? input
                    : _mcpConfig.PromptConfig.UserPromptTemplate.Replace("{input}", input);

                response = await _mcpSdkClient.GenerateAsync(
                    prompt,
                    _mcpConfig.PromptConfig.SystemPrompt,
                    cancellationToken);
            }

            if (!response.Success)
            {
                _logger.LogError("LLM failed to process input: {Error}", response.Error);
                
                // Mark LLM as unavailable if connection error
                if (response.Error?.Contains("Connection refused") == true || 
                    response.Error?.Contains("No connection") == true)
                {
                    _llmAvailable = false;
                    _logger.LogWarning("LLM appears to be unavailable. Will retry in {Seconds} seconds.", _llmCheckInterval.TotalSeconds);
                }
                
                return string.Empty;
            }

            _logger.LogInformation("LLM processed input successfully: ResponseLength={Length}, Tokens={Tokens}",
                response.Text.Length, response.EvalCount);

            // Apply behavior rules if configured
            var processedResponse = ApplyBehaviorRules(input, response.Text);

            return processedResponse;
        }
        catch (HttpRequestException ex)
        {
            _llmAvailable = false;
            _logger.LogWarning(ex, "LLM connection error. Will retry in {Seconds} seconds. Is Ollama running?", _llmCheckInterval.TotalSeconds);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing with LLM");
            return string.Empty;
        }
    }

    private async Task CheckLlmAvailabilityAsync(CancellationToken cancellationToken)
    {
        _lastLlmCheckTime = DateTime.UtcNow;
        
        if (_mcpSdkClient == null)
        {
            _llmAvailable = false;
            return;
        }

        try
        {
            var (isAvailable, errorMessage) = await _mcpSdkClient.CheckHealthAsync(cancellationToken);
            _llmAvailable = isAvailable;
            
            if (_llmAvailable)
            {
                _logger.LogInformation("LLM is available and ready at {Endpoint}", _mcpConfig.LocalLlmEndpoint);
            }
            else
            {
                _logger.LogWarning("LLM health check failed: {Error}", errorMessage);
            }
        }
        catch (Exception ex)
        {
            _llmAvailable = false;
            _logger.LogWarning("LLM health check error: {Message}", ex.Message);
        }
    }

    private string ApplyBehaviorRules(string input, string llmResponse)
    {
        foreach (var rule in _mcpConfig.BehaviorRules.Where(r => r.Enabled))
        {
            try
            {
                if (Regex.IsMatch(input, rule.TriggerPattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogInformation("Behavior rule triggered: {RuleName}", rule.Name);
                    
                    // Apply rule transformations or overrides
                    if (rule.ActionParameters.TryGetValue("ResponseOverride", out var overrideObj) 
                        && overrideObj is string overrideStr)
                    {
                        return overrideStr;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying behavior rule: {RuleName}", rule.Name);
            }
        }

        return llmResponse;
    }

    private async Task SendToActorAsync(string llmResponse, CancellationToken cancellationToken)
    {
        if (!_mcpClients.TryGetValue("actor", out var client))
        {
            _logger.LogWarning("Actor MCP client not found");
            return;
        }

        if (string.IsNullOrWhiteSpace(llmResponse))
        {
            return;
        }

        try
        {
            // Determine action based on action mappings
            var action = DetermineAction(llmResponse);
            
            StringContent content;
            string endpoint;

            // Select appropriate endpoint and build content based on action type
            switch (action.ActionType.ToLower())
            {
                case "say":
                case "display":
                    // These endpoints expect just the text string
                    endpoint = $"/api/actor/{action.ActionType.ToLower()}";
                    content = new StringContent(
                        JsonSerializer.Serialize(llmResponse), 
                        System.Text.Encoding.UTF8, 
                        "application/json");
                    break;
                
                case "animate":
                default:
                    // Perform endpoint expects full ActionRequest object
                    endpoint = "/api/actor/perform";
                    var actionRequest = new
                    {
                        action = action.ActionType,
                        text = llmResponse,
                        parameters = action.Parameters
                    };
                    content = new StringContent(
                        JsonSerializer.Serialize(actionRequest), 
                        System.Text.Encoding.UTF8, 
                        "application/json");
                    break;
            }
                
            var response = await client.PostAsync(endpoint, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Action sent to actor MCP: {Action}, Endpoint={Endpoint}", 
                    action.ActionType, endpoint);
            }
            else
            {
                _logger.LogWarning("Failed to send action to actor MCP: StatusCode={StatusCode}", 
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending action to actor MCP");
        }
    }

    private (string ActionType, Dictionary<string, object> Parameters) DetermineAction(string llmResponse)
    {
        // Check configured action mappings
        foreach (var mapping in _mcpConfig.ActionMappings)
        {
            try
            {
                if (Regex.IsMatch(llmResponse, mapping.Pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogDebug("Action mapping matched: Pattern={Pattern}, Action={Action}", 
                        mapping.Pattern, mapping.ActorAction);
                    return (mapping.ActorAction, mapping.DefaultParameters);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error matching action pattern: {Pattern}", mapping.Pattern);
            }
        }

        // Default action
        return ("say", new Dictionary<string, object>());
    }

    public Task<Dictionary<string, object>> GetStatusAsync()
    {
        return Task.FromResult(new Dictionary<string, object>
        {
            ["ConnectedMcps"] = _mcpClients.Count,
            ["McpNames"] = _mcpClients.Keys.ToList(),
            ["LlmEndpoint"] = _mcpConfig.LocalLlmEndpoint,
            ["ModelName"] = _mcpConfig.ModelName,
            ["McpSdkInitialized"] = _mcpSdkClient != null,
            ["LlmAvailable"] = _llmAvailable,
            ["LastLlmCheck"] = _lastLlmCheckTime,
            ["UseChatMode"] = _mcpConfig.UseChatMode,
            ["ChatHistoryCount"] = _chatHistory.Count,
            ["BehaviorRulesCount"] = _mcpConfig.BehaviorRules.Count(r => r.Enabled),
            ["ActionMappingsCount"] = _mcpConfig.ActionMappings.Count
        });
    }
}
