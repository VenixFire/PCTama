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
    private int _lastProcessedCaptionIndex = 0;
    private DateTime _lastProcessedTimestamp = DateTime.MinValue;

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
        _logger.LogInformation("File format: {Format}", _config.FileFormat);

        if (_config.StreamingEnabled)
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
        var configuredPath = _config.TranscriptionFilePath;
        var filePath = Path.IsPathRooted(configuredPath) 
            ? configuredPath 
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
        
        _logger.LogInformation("📁 Monitoring transcription file: {FilePath}", filePath);
        _logger.LogInformation("💡 Working directory: {WorkingDir}", Directory.GetCurrentDirectory());
        _logger.LogInformation("💡 File format: {Format}", _config.FileFormat);
        
        // Ensure file exists
        if (!File.Exists(filePath))
        {
            _logger.LogInformation("📝 Creating new file: {FilePath}", filePath);
            await File.WriteAllTextAsync(filePath, "", cancellationToken);
        }
        else
        {
            _logger.LogInformation("✅ File exists with {Size} bytes", new FileInfo(filePath).Length);
            
            // On startup, initialize by reading existing captions and skipping them
            if (_config.FileFormat.Equals("srt", StringComparison.OrdinalIgnoreCase))
            {
                await InitializeSrtTrackingAsync(filePath, cancellationToken);
            }
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

    private async Task InitializeSrtTrackingAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogInformation("📋 No existing captions found in file");
                return;
            }
            
            var captions = SrtParser.ParseSrtContent(content);
            if (captions.Count > 0)
            {
                // Find the highest caption index
                var maxIndex = captions.Max(c => c.Index);
                _lastProcessedCaptionIndex = maxIndex;
                _logger.LogInformation("🔄 Initialized caption tracking. Skipping {Count} existing captions (up to index {MaxIndex})",
                    captions.Count, maxIndex);
            }
            else
            {
                _logger.LogInformation("📋 No valid captions found in file");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SRT tracking. Will process all captions.");
        }
    }

    private async Task ReadNewContentAsync(string filePath, long startPosition, CancellationToken cancellationToken)
    {
        try
        {
            // For SRT format, we need to read the entire file to parse captions properly
            string content;
            if (_config.FileFormat.Equals("srt", StringComparison.OrdinalIgnoreCase))
            {
                content = await File.ReadAllTextAsync(filePath, cancellationToken);
                if (string.IsNullOrWhiteSpace(content))
                    return;
                
                await ProcessSrtContentAsync(content, cancellationToken);
            }
            else
            {
                // For other formats, read incrementally from last position
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileStream.Seek(startPosition, SeekOrigin.Begin);
                
                using var reader = new StreamReader(fileStream, Encoding.UTF8);
                content = await reader.ReadToEndAsync(cancellationToken);
                
                if (string.IsNullOrWhiteSpace(content))
                    return;
                
                _logger.LogDebug("📬 Read {Length} bytes from file", content.Length);
                
                // Process each line as a separate transcription
                var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine))
                        continue;
                    
                    // Try to parse as JSON first (if source outputs JSON)
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file content");
        }
    }

    private async Task ProcessSrtContentAsync(string content, CancellationToken cancellationToken)
    {
        try
        {
            var captions = SrtParser.ParseSrtContent(content);
            
            if (captions.Count == 0)
                return;
            
            // Process only new captions (with index greater than last processed)
            // This automatically filters out stale data from startup or file resets
            var newCaptions = captions
                .Where(c => c.Index > _lastProcessedCaptionIndex)
                .OrderBy(c => c.Index)
                .ToList();
            
            if (newCaptions.Count == 0)
                return;
            
            _logger.LogDebug("📬 Processing {Count} new SRT captions (starting from index {Index})", 
                newCaptions.Count, newCaptions.First().Index);
            
            foreach (var caption in newCaptions)
            {
                var textData = new TextData
                {
                    Text = caption.Text,
                    Source = _config.Source,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["format"] = "srt",
                        ["captionIndex"] = caption.Index,
                        ["startTime"] = caption.StartTime.ToString(@"hh\:mm\:ss\.fff"),
                        ["endTime"] = caption.EndTime.ToString(@"hh\:mm\:ss\.fff")
                    }
                };
                
                await AddTextToBufferAsync(textData);
                _logger.LogInformation("📝 Received caption #{Index}: {Text}", caption.Index, caption.Text);
                
                // Update last processed index
                if (caption.Index > _lastProcessedCaptionIndex)
                {
                    _lastProcessedCaptionIndex = caption.Index;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SRT content");
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
                    Source = _config.Source,
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
            Source = _config.Source,
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
