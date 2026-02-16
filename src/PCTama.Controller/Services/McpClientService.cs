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
        _logger.LogInformation("[MCP] Client Service starting... | Timestamp={Timestamp}", DateTime.UtcNow);
        
        // Log active personality
        var activePersonality = GetActivePersonality();
        _logger.LogInformation("[Personality] Active personality: {PersonalityName} | Description={Description}", 
            activePersonality?.DisplayName ?? "Default (from PromptConfig)", 
            activePersonality?.Description ?? "Using PromptConfig.SystemPrompt");

        // Initialize MCP clients
        await InitializeMcpClientsAsync(stoppingToken);

        _logger.LogInformation("[MCP] Service initialization complete | ProcessingInterval={IntervalMs}ms | Timestamp={Timestamp}", 
            _mcpConfig.ProcessingIntervalMs, DateTime.UtcNow);

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
                _logger.LogError(ex, "[MCP] Error in processing cycle | Exception={ExceptionType}", ex.GetType().Name);
            }
        }

        _logger.LogInformation("[MCP] Client Service stopping... | Timestamp={Timestamp}", DateTime.UtcNow);
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
            
            _logger.LogInformation("[LLM] MCP SDK client initialized | Endpoint={Endpoint} | Model={Model} | Timeout={Timeout}s | ChatMode={ChatMode} | MaxHistory={MaxHistory}",
                _mcpConfig.LocalLlmEndpoint, _mcpConfig.ModelName, _mcpConfig.LlmTimeoutSeconds, _mcpConfig.UseChatMode, _mcpConfig.MaxChatHistory);
            
            // Check if LLM is available
            await CheckLlmAvailabilityAsync(cancellationToken);
        }
        else
        {
            _logger.LogWarning("[LLM] No LLM endpoint configured. MCP SDK client not initialized.");
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
                _logger.LogInformation("[MCP] Client initialized | Server={Name} | Endpoint={Endpoint}", 
                    server.Name, server.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MCP] Failed to initialize client | Server={Name} | Endpoint={Endpoint} | Exception={ExceptionType}", 
                    server.Name, server.Endpoint, ex.GetType().Name);
            }
        }

        // Initialize additional input MCPs
        foreach (var server in _mcpConfig.AdditionalInputMcps.Where(s => s.Enabled))
        {
            try
            {
                var client = new HttpClient { BaseAddress = new Uri(server.Endpoint) };
                _mcpClients[server.Name] = client;
                _logger.LogInformation("[MCP] Additional input client initialized | Server={Name} | Endpoint={Endpoint}", 
                    server.Name, server.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MCP] Failed to initialize additional client | Server={Name} | Endpoint={Endpoint}", 
                    server.Name, server.Endpoint);
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

        _logger.LogInformation("[Input] Received text | Content={InputPreview}... | Length={InputLength}c", 
            textInput.Substring(0, Math.Min(80, textInput.Length)), textInput.Length);

        // Process with local LLM (placeholder - integrate with actual MCP SDK)
        var llmResponse = await ProcessWithLlmAsync(textInput, cancellationToken);

        // Send action to actor MCP
        await SendToActorAsync(llmResponse, textInput, cancellationToken);
    }

    private PersonalityProfile? GetActivePersonality()
    {
        if (string.IsNullOrEmpty(_mcpConfig.ActivePersonality) || !_mcpConfig.Personalities.Any())
        {
            return null;
        }

        return _mcpConfig.Personalities.FirstOrDefault(p => 
            p.Name.Equals(_mcpConfig.ActivePersonality, StringComparison.OrdinalIgnoreCase));
    }

    private string GetSystemPrompt()
    {
        var personality = GetActivePersonality();
        if (personality != null && !string.IsNullOrEmpty(personality.SystemPrompt))
        {
            return personality.SystemPrompt;
        }

        return _mcpConfig.PromptConfig.SystemPrompt;
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
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Try to parse as JSON object first (in case it's {"text":"", "message":"..."})
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(content);
                    var root = doc.RootElement;
                    
                    // Check if it's a JSON object with "text" property
                    if (root.TryGetProperty("text", out var textProp))
                    {
                        var text = textProp.GetString();
                        
                        // Skip if text is empty or null
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            _logger.LogDebug("[TextMCP] Filtered empty text response | Message={Message}", 
                                root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "N/A");
                            return string.Empty;
                        }
                        
                        return text;
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // Not JSON, treat as raw text
                }
                
                // Return raw content if not JSON or if no empty filter applied
                if (string.IsNullOrWhiteSpace(content))
                {
                    return string.Empty;
                }
                
                return content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TextMCP] Error getting text input | Exception={ExceptionType}", ex.GetType().Name);
        }

        return string.Empty;
    }

    private async Task<string> ProcessWithLlmAsync(string input, CancellationToken cancellationToken)
    {
        if (_mcpSdkClient == null)
        {
            _logger.LogWarning("[LLM] MCP SDK client not initialized. Cannot process input.");
            return string.Empty;
        }

        // Check LLM availability periodically
        if (!_llmAvailable && (DateTime.UtcNow - _lastLlmCheckTime) > _llmCheckInterval)
        {
            await CheckLlmAvailabilityAsync(cancellationToken);
        }

        if (!_llmAvailable)
        {
            _logger.LogDebug("[LLM] LLM not available. Skipping processing.");
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
                var systemPrompt = GetSystemPrompt();
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(ChatMessage.System(systemPrompt));
                }
                messages.AddRange(_chatHistory);

                _logger.LogTrace("[LLM] Chat mode | SystemPrompt={HasSystemPrompt} | HistorySize={HistorySize} | TotalMessages={MessageCount}", 
                    !string.IsNullOrEmpty(systemPrompt), _chatHistory.Count, messages.Count);

                response = await _mcpSdkClient.ChatAsync(messages, cancellationToken);

                if (response.Success)
                {
                    _chatHistory.Add(ChatMessage.Assistant(response.Text));
                    _logger.LogDebug("[LLM] Response added to chat history | NewHistorySize={HistorySize}", _chatHistory.Count);
                }
            }
            else
            {
                // Use simple generate mode
                var prompt = string.IsNullOrEmpty(_mcpConfig.PromptConfig.UserPromptTemplate)
                    ? input
                    : _mcpConfig.PromptConfig.UserPromptTemplate.Replace("{input}", input);

                _logger.LogTrace("[LLM] Generate mode | TemplateUsed={TemplateUsed} | FinalPromptLength={PromptLength}c", 
                    !string.IsNullOrEmpty(_mcpConfig.PromptConfig.UserPromptTemplate), prompt.Length);

                response = await _mcpSdkClient.GenerateAsync(
                    prompt,
                    GetSystemPrompt(),
                    cancellationToken);
            }

            if (!response.Success)
            {
                _logger.LogError("[LLM] Processing failed | Error={Error} | InputLength={InputLength}c | Mode={Mode}", 
                    response.Error, input.Length, _mcpConfig.UseChatMode ? "chat" : "generate");
                
                // Mark LLM as unavailable if connection error
                if (response.Error?.Contains("Connection refused") == true || 
                    response.Error?.Contains("No connection") == true)
                {
                    _llmAvailable = false;
                    _logger.LogWarning("[LLM] Connection error detected. Marking LLM unavailable. Will retry in {Seconds}s", _llmCheckInterval.TotalSeconds);
                }
                
                return string.Empty;
            }

            var responseLength = response.Text.Length;
            var totalTokens = response.PromptEvalCount + response.EvalCount;
            var tokensPerSecond = response.EvalCount > 0 
                ? (response.EvalCount * 1_000_000_000.0 / (response.TotalDuration > 0 ? response.TotalDuration : 1)) 
                : 0;
            var totalTimeSeconds = response.TotalDuration / 1_000_000_000.0;

            _logger.LogInformation("[LLM] Processing complete | ResponseLength={Length}c | ResponseTokens={EvalCount} | PromptTokens={PromptCount} | TotalTokens={TotalTokens} | Duration={DurationSeconds:F2}s | Mode={Mode}",
                responseLength, response.EvalCount, response.PromptEvalCount, totalTokens, totalTimeSeconds, _mcpConfig.UseChatMode ? "chat" : "generate");
            _logger.LogDebug("[LLM] Response preview | Text={ResponsePreview}...", 
                response.Text.Substring(0, Math.Min(120, response.Text.Length)));

            // Apply behavior rules if configured
            var processedResponse = ApplyBehaviorRules(input, response.Text);

            return processedResponse;
        }
        catch (HttpRequestException ex)
        {
            _llmAvailable = false;
            _logger.LogWarning(ex, "[LLM] HTTP connection error | Endpoint={Endpoint} | IsOllamaRunning={OllamaHint} | WillRetry={Seconds}s", 
                _mcpConfig.LocalLlmEndpoint, "Run 'ollama serve'?", _llmCheckInterval.TotalSeconds);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LLM] Processing error | Mode={Mode} | InputLength={InputLength}c | Exception={ExceptionType}", 
                _mcpConfig.UseChatMode ? "chat" : "generate", input.Length, ex.GetType().Name);
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
                _logger.LogInformation("[LLM] Health check OK | Endpoint={Endpoint} | Model={Model} | Status=available", 
                    _mcpConfig.LocalLlmEndpoint, _mcpConfig.ModelName);
            }
            else
            {
                _logger.LogWarning("[LLM] Health check failed | Error={Error} | Endpoint={Endpoint}", 
                    errorMessage, _mcpConfig.LocalLlmEndpoint);
            }
        }
        catch (Exception ex)
        {
            _llmAvailable = false;
            _logger.LogWarning(ex, "[LLM] Health check exception | Endpoint={Endpoint} | Message={ExceptionMessage}", 
                _mcpConfig.LocalLlmEndpoint, ex.Message);
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
                    _logger.LogInformation("[Behavior] Rule triggered | RuleName={RuleName} | Pattern={Pattern} | Input={InputPreview}...", 
                        rule.Name, rule.TriggerPattern, input.Substring(0, Math.Min(60, input.Length)));
                    
                    // Apply rule transformations or overrides
                    if (rule.ActionParameters.TryGetValue("ResponseOverride", out var overrideObj) 
                        && overrideObj is string overrideStr)
                    {
                        _logger.LogDebug("[Behavior] Applying response override | OriginalLength={OriginalLength}c | OverrideLength={OverrideLength}c", 
                            llmResponse.Length, overrideStr.Length);
                        return overrideStr;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Behavior] Error applying rule | RuleName={RuleName} | Pattern={Pattern} | Exception={ExceptionType}", 
                    rule.Name, rule.TriggerPattern, ex.GetType().Name);
            }
        }

        return llmResponse;
    }

    private async Task SendToActorAsync(string llmResponse, string textInput, CancellationToken cancellationToken)
    {
        if (!_mcpClients.TryGetValue("actor", out var client))
        {
            _logger.LogWarning("[Actor] MCP client not found | AvailableClients={ClientCount}", _mcpClients.Count);
            return;
        }

        if (string.IsNullOrWhiteSpace(llmResponse))
        {
            _logger.LogDebug("[Actor] Skipping empty LLM response");
            return;
        }

        try
        {
            // Determine action based on action mappings
            var action = DetermineAction(llmResponse);
            
            StringContent content;
            string endpoint;

            // All actions use full ActionRequest object
            var actionRequest = new
            {
                action = action.ActionType,
                text = llmResponse,
                inputText = textInput,
                parameters = action.Parameters
            };

            // Select appropriate endpoint based on action type
            switch (action.ActionType.ToLower())
            {
                case "say":
                case "display":
                    // Say and display endpoints accept ActionRequest
                    endpoint = $"/api/actor/{action.ActionType.ToLower()}";
                    break;
                
                case "animate":
                case "dance":
                default:
                    // Default to perform endpoint
                    endpoint = "/api/actor/perform";
                    break;
            }

            content = new StringContent(
                JsonSerializer.Serialize(actionRequest), 
                System.Text.Encoding.UTF8, 
                "application/json");
                
            var response = await client.PostAsync(endpoint, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Actor] Action sent | Action={Action} | Endpoint={Endpoint} | ResponseLength={ResponseLength}c", 
                    action.ActionType, endpoint, llmResponse.Length);
            }
            else
            {
                _logger.LogWarning("[Actor] Failed to send action | Action={Action} | Endpoint={Endpoint} | StatusCode={StatusCode}", 
                    action.ActionType, endpoint, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Actor] Error sending action to actor MCP | ResponseLength={ResponseLength}c", llmResponse.Length);
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
                    _logger.LogDebug("[Action] Pattern matched | Pattern={Pattern} | MappedAction={Action} | Response={ResponsePreview}...", 
                        mapping.Pattern, mapping.ActorAction, llmResponse.Substring(0, Math.Min(60, llmResponse.Length)));
                    return (mapping.ActorAction, mapping.DefaultParameters);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Action] Error matching pattern | Pattern={Pattern} | Exception={ExceptionType}", 
                    mapping.Pattern, ex.GetType().Name);
            }
        }

        // Default action
        _logger.LogDebug("[Action] No pattern matches found, using default action | Default=say");
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
