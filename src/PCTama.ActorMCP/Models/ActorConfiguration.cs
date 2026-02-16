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
    public string? InputText { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
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
public class ActorStateResource
{
    public string CurrentState { get; set; } = string.Empty;
    public int QueueDepth { get; set; }
    public string? LastAction { get; set; }
    public DateTime? LastActionTimestamp { get; set; }
    public int TotalActionsProcessed { get; set; }
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