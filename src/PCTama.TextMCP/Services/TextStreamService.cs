using PCTama.TextMCP.Models;
using System.Collections.Concurrent;
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
            await StartFileMonitoringAsync(stoppingToken);
        }

        // Process additional sources
        foreach (var source in _config.AdditionalSources.Where(s => s.Enabled))
        {
            _ = Task.Run(() => ProcessAdditionalSourceAsync(source, stoppingToken), stoppingToken);
        }
    }

    private async Task StartFileMonitoringAsync(CancellationToken cancellationToken)
    {
        // Resolve file path - if relative, make it relative to the app directory
        var configuredPath = _config.LocalVocalFilePath;
        var filePath = Path.IsPathRooted(configuredPath) 
            ? configuredPath 
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
        
        _logger.LogInformation("📁 Monitoring file for OBS LocalVocal text: {FilePath}", filePath);
        _logger.LogInformation("💡 Working directory: {WorkingDir}", Directory.GetCurrentDirectory());
        _logger.LogInformation("💡 OBS LocalVocal should write transcriptions to this file");
        
        // Ensure file exists
        if (!File.Exists(filePath))
        {
            _logger.LogInformation("📝 Creating new file: {FilePath}", filePath);
            await File.WriteAllTextAsync(filePath, "", cancellationToken);
        }
        else
        {
            _logger.LogInformation("✅ File exists with {Size} bytes", new FileInfo(filePath).Length);
        }
        
        long lastPosition = 0;
        long lastFileSize = new FileInfo(filePath).Length;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    _logger.LogWarning("⚠️  File disappeared: {FilePath}. Recreating...", filePath);
                    await File.WriteAllTextAsync(filePath, "", cancellationToken);
                    lastPosition = 0;
                    await Task.Delay(TimeSpan.FromMilliseconds(_config.FilePollingIntervalMs), cancellationToken);
                    continue;
                }
                
                // Check if file has new content
                if (fileInfo.Length > lastPosition)
                {
                    await ReadNewContentAsync(filePath, lastPosition, cancellationToken);
                    lastPosition = fileInfo.Length;
                }
                else if (fileInfo.Length < lastFileSize)
                {
                    // File was truncated or reset
                    _logger.LogDebug("🔄 File was reset, starting from beginning");
                    lastPosition = 0;
                }
                
                lastFileSize = fileInfo.Length;
                await Task.Delay(TimeSpan.FromMilliseconds(_config.FilePollingIntervalMs), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring file: {FilePath}", filePath);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private async Task ReadNewContentAsync(string filePath, long startPosition, CancellationToken cancellationToken)
    {
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fileStream.Seek(startPosition, SeekOrigin.Begin);
            
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var newContent = await reader.ReadToEndAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(newContent))
                return;
            
            _logger.LogDebug("📬 Read {Length} bytes from file", newContent.Length);
            
            // Process each line as a separate transcription
            var lines = newContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;
                
                // Try to parse as JSON first (if OBS outputs JSON)
                if (trimmedLine.StartsWith("{") && trimmedLine.EndsWith("}"))
                {
                    await ProcessJsonLineAsync(trimmedLine, cancellationToken);
                }
                else
                {
                    // Plain text
                    await ProcessPlainTextLineAsync(trimmedLine, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file content");
        }
    }

    private async Task ProcessJsonLineAsync(string jsonLine, CancellationToken cancellationToken)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonLine);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("text", out var textElement))
            {
                var text = textElement.GetString();
                if (string.IsNullOrWhiteSpace(text))
                    return;

                var confidence = root.TryGetProperty("confidence", out var confElement) 
                    ? confElement.GetDouble() 
                    : 1.0;

                var textData = new TextData
                {
                    Text = text,
                    Source = "OBSLocalVoice",
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["confidence"] = confidence,
                        ["format"] = "json"
                    }
                };

                if (root.TryGetProperty("language", out var langElement))
                {
                    textData.Metadata["language"] = langElement.GetString() ?? "unknown";
                }

                await AddTextToBufferAsync(textData);
                _logger.LogInformation("📝 Received from file (JSON): {Text} (confidence: {Confidence:P0})", 
                    text, confidence);
            }
        }
        catch (JsonException)
        {
            // If JSON parsing fails, treat as plain text
            await ProcessPlainTextLineAsync(jsonLine, cancellationToken);
        }
    }

    private async Task ProcessPlainTextLineAsync(string text, CancellationToken cancellationToken)
    {
        var textData = new TextData
        {
            Text = text,
            Source = "OBSLocalVoice",
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["confidence"] = 1.0,
                ["format"] = "plaintext"
            }
        };

        await AddTextToBufferAsync(textData);
        _logger.LogInformation("📝 Received from file (text): {Text}", text);
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
