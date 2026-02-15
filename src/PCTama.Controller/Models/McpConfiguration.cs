namespace PCTama.Controller.Models;

public class McpServerConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class McpConfiguration
{
    public string LocalLlmEndpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public List<McpServerConfiguration> McpServers { get; set; } = new();
    public List<McpServerConfiguration> AdditionalInputMcps { get; set; } = new();
}
