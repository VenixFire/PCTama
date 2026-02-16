namespace PCTama.ActorMCP.Models;

public class ActorConfiguration
{
    public string DisplayType { get; set; } = "WinUI3";
    public int WindowWidth { get; set; } = 400;
    public int WindowHeight { get; set; } = 300;
    public string WindowTitle { get; set; } = "PCTama Actor";
    public bool AlwaysOnTop { get; set; } = true;
    public bool EnableAnimations { get; set; } = true;
    public string DefaultAction { get; set; } = "Display";
}

public class ActionRequest
{
    public string Action { get; set; } = string.Empty;
    public string? Text { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
