namespace PCTama.TextMCP.Models;

public class TextStreamConfiguration
{
    public string Source { get; set; } = string.Empty;
    public string TranscriptionFilePath { get; set; } = "/tmp/AudioTranscription.srt";
    public string FileFormat { get; set; } = "srt"; // "srt", "plaintext", "json"
    public bool StreamingEnabled { get; set; } = true;
    public int BufferSize { get; set; } = 4096;
    public int FilePollingIntervalMs { get; set; } = 100;
    public int StaleDataThresholdSeconds { get; set; } = 30;
    public List<AdditionalSource> AdditionalSources { get; set; } = new();
}

public class AdditionalSource
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class TextData
{
    public string Text { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SrtCaption
{
    public int Index { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
}
// MCP Tool Models
public class McpTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object InputSchema { get; set; } = new { };
}

public class McpToolsResponse
{
    public List<McpTool> Tools { get; set; } = new();
}

// MCP Resource Models
public class TextStreamStateResource
{
    public string CurrentState { get; set; } = string.Empty;
    public int BufferCount { get; set; }
    public DateTime? LastTextTimestamp { get; set; }
    public string ActiveSource { get; set; } = string.Empty;
    public List<string> ActiveSources { get; set; } = new();
    public int TotalTextsProcessed { get; set; }
    public DateTime ServiceStartTime { get; set; }
}

public class McpResource
{
    public string Uri { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/json";
}

public class McpResourcesResponse
{
    public List<McpResource> Resources { get; set; } = new();
}