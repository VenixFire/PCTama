namespace PCTama.Controller.Models;

public class McpServerConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class BehaviorRule
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TriggerPattern { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, object> ActionParameters { get; set; } = new();
    public bool Enabled { get; set; } = true;
}

public class PromptConfiguration
{
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPromptTemplate { get; set; } = string.Empty;
    public List<string> Examples { get; set; } = new();
    public Dictionary<string, string> ResponsePatterns { get; set; } = new();
}

public class PersonalityProfile
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public List<string> Examples { get; set; } = new();
}

public class ActionMapping
{
    public string Pattern { get; set; } = string.Empty;
    public string ActorAction { get; set; } = string.Empty;
    public Dictionary<string, object> DefaultParameters { get; set; } = new();
}

public class McpConfiguration
{
    public string LocalLlmEndpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public List<McpServerConfiguration> McpServers { get; set; } = new();
    public List<McpServerConfiguration> AdditionalInputMcps { get; set; } = new();
    public PromptConfiguration PromptConfig { get; set; } = new();
    public List<BehaviorRule> BehaviorRules { get; set; } = new();
    public List<ActionMapping> ActionMappings { get; set; } = new();
    public List<PersonalityProfile> Personalities { get; set; } = new();
    public string ActivePersonality { get; set; } = "default";
    public int ProcessingIntervalMs { get; set; } = 1000;
    public bool UseChatMode { get; set; } = true;
    public int MaxChatHistory { get; set; } = 10;
    public int LlmTimeoutSeconds { get; set; } = 60;
}
