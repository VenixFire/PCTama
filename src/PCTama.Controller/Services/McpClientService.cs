using Microsoft.Extensions.Options;
using PCTama.Controller.Models;
using System.Text.Json;

namespace PCTama.Controller.Services;

public class McpClientService : BackgroundService
{
    private readonly ILogger<McpClientService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly McpConfiguration _mcpConfig;
    private readonly Dictionary<string, HttpClient> _mcpClients = new();

    public McpClientService(
        ILogger<McpClientService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
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
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MCP processing cycle");
            }
        }
    }

    private async Task InitializeMcpClientsAsync(CancellationToken cancellationToken)
    {
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
        // TODO: Integrate with official .NET MCP SDK
        // This is a placeholder implementation
        _logger.LogInformation("Processing with LLM: {Input}", input);
        
        // Simulate LLM processing
        await Task.Delay(100, cancellationToken);
        
        return $"Processed: {input}";
    }

    private async Task SendToActorAsync(string action, CancellationToken cancellationToken)
    {
        if (!_mcpClients.TryGetValue("actor", out var client))
        {
            return;
        }

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { action }), 
                System.Text.Encoding.UTF8, 
                "application/json");
                
            var response = await client.PostAsync("/api/actor/perform", content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Action sent to actor MCP: {Action}", action);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending action to actor MCP");
        }
    }

    public async Task<Dictionary<string, object>> GetStatusAsync()
    {
        return new Dictionary<string, object>
        {
            ["ConnectedMcps"] = _mcpClients.Count,
            ["McpNames"] = _mcpClients.Keys.ToList(),
            ["LlmEndpoint"] = _mcpConfig.LocalLlmEndpoint,
            ["ModelName"] = _mcpConfig.ModelName
        };
    }
}
