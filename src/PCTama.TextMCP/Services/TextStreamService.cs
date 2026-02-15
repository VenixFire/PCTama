using PCTama.TextMCP.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace PCTama.TextMCP.Services;

public class TextStreamService : BackgroundService
{
    private readonly ILogger<TextStreamService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TextStreamConfiguration _config;
    private readonly ConcurrentQueue<TextData> _textBuffer = new();
    private readonly SemaphoreSlim _bufferSemaphore = new(1, 1);

    public TextStreamService(
        ILogger<TextStreamService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _config = configuration.GetSection("TextMcpConfiguration").Get<TextStreamConfiguration>() 
            ?? new TextStreamConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Text MCP Service starting...");
        _logger.LogInformation("Configured source: {Source}", _config.Source);

        if (_config.Source == "OBSLocalVoice")
        {
            await StartOBSLocalVoiceStreamAsync(stoppingToken);
        }

        // Process additional sources
        foreach (var source in _config.AdditionalSources.Where(s => s.Enabled))
        {
            _ = Task.Run(() => ProcessAdditionalSourceAsync(source, stoppingToken), stoppingToken);
        }
    }

    private async Task StartOBSLocalVoiceStreamAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting to OBS LocalVoice at {Endpoint}", _config.OBSLocalVoiceEndpoint);
        _logger.LogInformation("⚠️  Ensure OBS LocalVocal plugin is installed and WebSocket is enabled in OBS settings");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndListenToOBSLocalVocalAsync(cancellationToken);
            }
            catch (WebSocketException wsEx)
            {
                _logger.LogError(wsEx, "❌ WebSocket connection failed to OBS LocalVocal");
                _logger.LogError("📋 Required OBS Setup:");
                _logger.LogError("   1. Install LocalVocal plugin (https://github.com/occ-ai/obs-localvocal)");
                _logger.LogError("   2. In OBS: Tools → LocalVocal Settings");
                _logger.LogError("   3. Enable 'WebSocket Server' and set port to 4455");
                _logger.LogError("   4. Enable 'Send Transcription to WebSocket'");
                _logger.LogError("   5. Add LocalVocal filter to your microphone source");
                _logger.LogError("⏱️  Retrying connection in 10 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OBS LocalVoice connection. Reconnecting in 10 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }

    private async Task ConnectAndListenToOBSLocalVocalAsync(CancellationToken cancellationToken)
    {
        using var ws = new ClientWebSocket();
        
        try
        {
            var uri = new Uri(_config.OBSLocalVoiceEndpoint);
            _logger.LogInformation("Attempting WebSocket connection to {Uri}", uri);
            
            await ws.ConnectAsync(uri, cancellationToken);
            _logger.LogInformation("✅ Connected to OBS LocalVocal WebSocket");

            var buffer = new byte[4096];
            var messageBuilder = new StringBuilder();

            while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("WebSocket close message received");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuilder.Append(message);

                    if (result.EndOfMessage)
                    {
                        var fullMessage = messageBuilder.ToString();
                        messageBuilder.Clear();

                        await ProcessOBSLocalVocalMessageAsync(fullMessage, cancellationToken);
                    }
                }
            }
        }
        catch (WebSocketException wsEx)
        {
            // Provide detailed error message about OBS configuration
            if (wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely || 
                wsEx.Message.Contains("connect", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("⚠️  Cannot connect to OBS LocalVocal WebSocket at {Endpoint}", _config.OBSLocalVoiceEndpoint);
                _logger.LogWarning("💡 Check that OBS LocalVocal plugin WebSocket settings are enabled:");
                _logger.LogWarning("   → OBS → Tools → LocalVocal Settings → Enable WebSocket Server");
            }
            else
            {
                _logger.LogWarning(wsEx, "WebSocket error occurred. Verify OBS LocalVocal configuration.");
            }
            throw;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogWarning(httpEx, "❌ HTTP connection failed - OBS LocalVocal WebSocket server may not be running");
            _logger.LogWarning("💡 Verify: OBS is running AND LocalVocal WebSocket is enabled in plugin settings");
            throw;
        }
        finally
        {
            if (ws.State == WebSocketState.Open)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Service stopping", CancellationToken.None);
            }
        }
    }

    private async Task ProcessOBSLocalVocalMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            // OBS LocalVocal sends JSON messages with transcription data
            // Expected format: { "text": "...", "confidence": 0.95, "is_final": true }
            var jsonDoc = JsonDocument.Parse(message);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("text", out var textElement))
            {
                var text = textElement.GetString();
                if (string.IsNullOrWhiteSpace(text))
                    return;

                var isFinal = root.TryGetProperty("is_final", out var finalElement) && finalElement.GetBoolean();
                
                // Only process final transcriptions to avoid duplicates
                if (!isFinal)
                    return;

                var confidence = root.TryGetProperty("confidence", out var confElement) 
                    ? confElement.GetDouble() 
                    : 0.0;

                var textData = new TextData
                {
                    Text = text,
                    Source = "OBSLocalVoice",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["confidence"] = confidence,
                        ["is_final"] = isFinal
                    }
                };

                // Add language if present
                if (root.TryGetProperty("language", out var langElement))
                {
                    textData.Metadata["language"] = langElement.GetString() ?? "unknown";
                }

                await AddTextToBufferAsync(textData);
                _logger.LogInformation("📝 Received from OBS LocalVocal: {Text} (confidence: {Confidence:P0})", 
                    text, confidence);
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "Failed to parse OBS LocalVocal message: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OBS LocalVocal message");
        }
    }

    private async Task StartOBSLocalVoiceStreamAsync_Simulation(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running OBS LocalVoice in SIMULATION mode");

        // TODO: Remove this simulation method once real WebSocket is confirmed working
        // This is a placeholder implementation that simulates streaming
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Simulate receiving text data from OBS LocalVoice
                // In production, this would be a WebSocket listener
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                
                // Placeholder: simulate periodic text input
                var textData = new TextData
                {
                    Text = $"Simulated text from OBS LocalVoice at {DateTime.UtcNow:HH:mm:ss}",
                    Source = "OBSLocalVoice",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["confidence"] = 0.95,
                        ["language"] = "en-US"
                    }
                };

                await AddTextToBufferAsync(textData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OBS LocalVoice stream");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }

    private async Task ProcessAdditionalSourceAsync(AdditionalSource source, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing additional source: {Name} ({Type})", source.Name, source.Type);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: Implement different source type handlers
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing additional source: {Name}", source.Name);
            }
        }
    }

    private async Task AddTextToBufferAsync(TextData textData)
    {
        await _bufferSemaphore.WaitAsync();
        try
        {
            _textBuffer.Enqueue(textData);
            
            // Maintain buffer size limit
            while (_textBuffer.Count > _config.BufferSize)
            {
                _textBuffer.TryDequeue(out _);
            }

            _logger.LogDebug("Added text to buffer: {Text}", textData.Text);
        }
        finally
        {
            _bufferSemaphore.Release();
        }
    }

    public async Task<TextData?> GetLatestTextAsync()
    {
        await _bufferSemaphore.WaitAsync();
        try
        {
            if (_textBuffer.TryDequeue(out var textData))
            {
                return textData;
            }
            return null;
        }
        finally
        {
            _bufferSemaphore.Release();
        }
    }

    public async Task<List<TextData>> GetAllTextAsync()
    {
        await _bufferSemaphore.WaitAsync();
        try
        {
            return _textBuffer.ToList();
        }
        finally
        {
            _bufferSemaphore.Release();
        }
    }

    public int GetBufferCount() => _textBuffer.Count;
}
