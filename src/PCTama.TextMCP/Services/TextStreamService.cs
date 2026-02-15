using PCTama.TextMCP.Models;
using System.Collections.Concurrent;

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

        // TODO: Implement actual OBS LocalVoice WebSocket connection
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
