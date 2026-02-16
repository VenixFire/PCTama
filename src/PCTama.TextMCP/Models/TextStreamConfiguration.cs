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
