using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCTama.Controller.Services;

/// <summary>
/// MCP SDK Client for communicating with local LLM endpoints (e.g., Ollama)
/// </summary>
public class McpSdkClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpSdkClient> _logger;
    private readonly string _modelName;

    public McpSdkClient(HttpClient httpClient, ILogger<McpSdkClient> logger, string modelName)
    {
        _httpClient = httpClient;
        _logger = logger;
        _modelName = modelName;
    }

    /// <summary>
    /// Send a prompt to the LLM and get a response
    /// </summary>
    public async Task<LlmResponse> GenerateAsync(
        string prompt, 
        string? systemPrompt = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OllamaGenerateRequest
            {
                Model = _modelName,
                Prompt = prompt,
                System = systemPrompt,
                Stream = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, JsonSerializerOptions),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("Sending request to LLM: Model={Model}, PromptLength={Length}", 
                _modelName, prompt.Length);

            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorMsg = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.NotFound => 
                        $"404 Not Found - Model '{_modelName}' may not exist or Ollama API is not available. Try 'ollama pull {_modelName}' to download the model.",
                    System.Net.HttpStatusCode.ServiceUnavailable =>
                        "503 Service Unavailable - Ollama server is not responding. Ensure 'ollama serve' is running.",
                    _ => $"HTTP {(int)response.StatusCode} - {errorContent}"
                };
                throw new HttpRequestException(errorMsg);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(
                responseContent, JsonSerializerOptions);

            if (ollamaResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize LLM response");
            }

            _logger.LogInformation("LLM response received: TokenCount={TokenCount}", 
                ollamaResponse.EvalCount);

            return new LlmResponse
            {
                Text = ollamaResponse.Response,
                Model = ollamaResponse.Model,
                Success = true,
                TotalDuration = ollamaResponse.TotalDuration,
                LoadDuration = ollamaResponse.LoadDuration,
                PromptEvalCount = ollamaResponse.PromptEvalCount,
                EvalCount = ollamaResponse.EvalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM response");
            return new LlmResponse
            {
                Text = string.Empty,
                Model = _modelName,
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Send a chat-style conversation to the LLM
    /// </summary>
    public async Task<LlmResponse> ChatAsync(
        List<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OllamaChatRequest
            {
                Model = _modelName,
                Messages = messages.Select(m => new OllamaChatMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList(),
                Stream = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, JsonSerializerOptions),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("Sending chat request to LLM: Model={Model}, MessageCount={Count}", 
                _modelName, messages.Count);

            var response = await _httpClient.PostAsync("/api/chat", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorMsg = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.NotFound => 
                        $"404 Not Found - Model '{_modelName}' may not exist or Ollama API is not available. Try 'ollama pull {_modelName}' to download the model.",
                    System.Net.HttpStatusCode.ServiceUnavailable =>
                        "503 Service Unavailable - Ollama server is not responding. Ensure 'ollama serve' is running.",
                    _ => $"HTTP {(int)response.StatusCode} - {errorContent}"
                };
                throw new HttpRequestException(errorMsg);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(
                responseContent, JsonSerializerOptions);

            if (ollamaResponse == null || ollamaResponse.Message == null)
            {
                throw new InvalidOperationException("Failed to deserialize LLM chat response");
            }

            _logger.LogInformation("LLM chat response received: TokenCount={TokenCount}", 
                ollamaResponse.EvalCount);

            return new LlmResponse
            {
                Text = ollamaResponse.Message.Content,
                Model = ollamaResponse.Model,
                Success = true,
                TotalDuration = ollamaResponse.TotalDuration,
                LoadDuration = ollamaResponse.LoadDuration,
                PromptEvalCount = ollamaResponse.PromptEvalCount,
                EvalCount = ollamaResponse.EvalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM chat response");
            return new LlmResponse
            {
                Text = string.Empty,
                Model = _modelName,
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Check if Ollama is running and the model is available
    /// </summary>
    public async Task<(bool IsAvailable, string? ErrorMessage)> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // First check if Ollama is responding
            var tagsResponse = await _httpClient.GetAsync("/api/tags", cancellationToken);
            if (!tagsResponse.IsSuccessStatusCode)
            {
                return (false, "Ollama server is not responding. Make sure Ollama is installed and run 'ollama serve'.");
            }

            // Parse and check if our model exists
            var tagsContent = await tagsResponse.Content.ReadAsStringAsync(cancellationToken);
            var tagsData = JsonSerializer.Deserialize<OllamaTagsResponse>(tagsContent, JsonSerializerOptions);
            
            if (tagsData?.Models == null || !tagsData.Models.Any(m => m.Name == _modelName || m.Name.StartsWith(_modelName + ":")))
            {
                return (false, $"Model '{_modelName}' not found. Run 'ollama pull {_modelName}' to download it. Available models: {string.Join(", ", tagsData?.Models?.Select(m => m.Name) ?? Array.Empty<string>())}");
            }

            return (true, null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Cannot connect to Ollama at {_httpClient.BaseAddress}. Error: {ex.Message}. Make sure Ollama is running with 'ollama serve'.");
        }
        catch (Exception ex)
        {
            return (false, $"Health check failed: {ex.Message}");
        }
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

// Request/Response Models for Ollama API

public class OllamaGenerateRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

public class OllamaGenerateResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("total_duration")]
    public long TotalDuration { get; set; }

    [JsonPropertyName("load_duration")]
    public long LoadDuration { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvalCount { get; set; }

    [JsonPropertyName("eval_count")]
    public int EvalCount { get; set; }
}

public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

public class OllamaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public OllamaChatMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("total_duration")]
    public long TotalDuration { get; set; }

    [JsonPropertyName("load_duration")]
    public long LoadDuration { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvalCount { get; set; }

    [JsonPropertyName("eval_count")]
    public int EvalCount { get; set; }
}

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelInfo>? Models { get; set; }
}

public class OllamaModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("modified_at")]
    public string? ModifiedAt { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

// Public API Models

public class LlmResponse
{
    public string Text { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public long TotalDuration { get; set; }
    public long LoadDuration { get; set; }
    public int PromptEvalCount { get; set; }
    public int EvalCount { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public static ChatMessage System(string content) => new() { Role = "system", Content = content };
    public static ChatMessage User(string content) => new() { Role = "user", Content = content };
    public static ChatMessage Assistant(string content) => new() { Role = "assistant", Content = content };
}
