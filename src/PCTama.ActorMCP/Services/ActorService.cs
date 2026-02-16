using PCTama.ActorMCP.Models;
using PCTama.ActorMCP.Views;
using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace PCTama.ActorMCP.Services;

public class ActorService
{
    private readonly ILogger<ActorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ActorConfiguration _config;
    private readonly ConcurrentQueue<ActionRequest> _actionQueue = new();
    private readonly SemaphoreSlim _queueSemaphore = new(1, 1);
    private ActorWindow? _window;
    private CancellationTokenSource? _processingCts;
    private string? _lastAction;
    private DateTime? _lastActionTimestamp;
    private int _totalActionsProcessed;
    private readonly DateTime _serviceStartTime = DateTime.UtcNow;

    public ActorService(
        ILogger<ActorService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _config = configuration.GetSection("ActorMcpConfiguration").Get<ActorConfiguration>() 
            ?? new ActorConfiguration();
    }

    public void SetWindow(ActorWindow window)
    {
        _window = window;
        _logger.LogInformation("Actor window set successfully");
    }

    public async Task StartProcessingAsync()
    {
        _logger.LogInformation("Actor MCP Service starting... Timestamp={Timestamp}", DateTime.UtcNow);
        _logger.LogInformation("Display type: {DisplayType} Timestamp={Timestamp}", _config.DisplayType, DateTime.UtcNow);

        _processingCts = new CancellationTokenSource();
        var stoppingToken = _processingCts.Token;

        // Process action queue
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_actionQueue.TryDequeue(out var action))
                {
                    await ProcessActionAsync(action, stoppingToken);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing action Timestamp={Timestamp}", DateTime.UtcNow);
            }
        }
    }

    private async Task ProcessActionAsync(ActionRequest action, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(action.Action))
        {
            return;
        }

        if (action.Action.StartsWith("Processed:", StringComparison.OrdinalIgnoreCase) &&
            action.Action.Contains("No text available", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _logger.LogInformation("Processing action: {Action} Timestamp={Timestamp}", action.Action, DateTime.UtcNow);

        // Update state tracking
        _lastAction = action.Action;
        _lastActionTimestamp = DateTime.UtcNow;
        _totalActionsProcessed++;

        switch (action.Action.ToLower())
        {
            case "say":
                await SayAsync(action.Text ?? string.Empty, action.InputText, action.Timestamp);
                break;
            case "display":
                await DisplayAsync(action.Text ?? string.Empty, action.Timestamp);
                break;
            case "animate":
                await AnimateAsync(action.Parameters, action.Timestamp);
                break;
            default:
                _logger.LogWarning("Unknown action: {Action}", action.Action);
                UpdateWindowDisplay("Unknown Action", action.Action, action.Timestamp);
                break;
        }
    }

    private async Task SayAsync(string text, string? inputText, DateTime? timestamp)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            _logger.LogInformation("Say action: {Text} Timestamp={Timestamp}", text, DateTime.UtcNow);
            UpdateWindowDisplay("Say", text, timestamp);
        }
        else
        {
            _logger.LogInformation("Say action: {Text} (replying to: {InputText}) Timestamp={Timestamp}", text, inputText, DateTime.UtcNow);
            var displayText = $"{text}\n[Replying to: {inputText}]";
            UpdateWindowDisplay("Say", displayText, timestamp);
        }
        await Task.CompletedTask;
    }

    private async Task DisplayAsync(string text, DateTime? timestamp)
    {
        _logger.LogInformation("Display action: {Text} Timestamp={Timestamp}", text, DateTime.UtcNow);
        UpdateWindowDisplay("Display", text, timestamp);
        await Task.CompletedTask;
    }

    private async Task AnimateAsync(Dictionary<string, object> parameters, DateTime? timestamp)
    {
        _logger.LogInformation("Animate action with parameters: {Parameters} Timestamp={Timestamp}", 
            string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")),
            DateTime.UtcNow);
        
        var parameterString = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));
        UpdateWindowDisplay("Animate", parameterString, timestamp);
        await Task.CompletedTask;
    }

    private void UpdateWindowDisplay(string action, string details, DateTime? timestamp = null)
    {
        if (_window == null)
        {
            _logger.LogWarning("Window is null, cannot update display. Action={Action}", action);
            return;
        }

        if (_window.ViewModel == null)
        {
            _logger.LogWarning("Window ViewModel is null, cannot update display. Action={Action}", action);
            return;
        }

        try
        {
            // Dispatcher.UIThread is the Avalonia thread dispatcher
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _window.ViewModel.AddAction(action, details, timestamp);
                    _logger.LogDebug("Action added to window history: {Action}", action);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in UI thread action handler. Action={Action}", action);
                }
            }, Avalonia.Threading.DispatcherPriority.Normal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating window display Timestamp={Timestamp}", DateTime.UtcNow);
        }
    }

    public async Task<ActionResult> EnqueueActionAsync(ActionRequest action)
    {
        if (action.Action.StartsWith("Processed:", StringComparison.OrdinalIgnoreCase) &&
            action.Action.Contains("No text available", StringComparison.OrdinalIgnoreCase))
        {
            return new ActionResult
            {
                Success = true,
                Message = "No-op: no text available",
                Timestamp = DateTime.UtcNow
            };
        }

        await _queueSemaphore.WaitAsync();
        try
        {
            if (action.Timestamp == default)
            {
                action.Timestamp = DateTime.UtcNow;
            }

            _actionQueue.Enqueue(action);
            _logger.LogInformation("Action enqueued: {Action} Timestamp={Timestamp}", action.Action, DateTime.UtcNow);
            
            return new ActionResult
            {
                Success = true,
                Message = $"Action '{action.Action}' enqueued successfully"
            };
        }
        finally
        {
            _queueSemaphore.Release();
        }
    }

    public int GetQueueCount() => _actionQueue.Count;

    // MCP Tool Discovery
    public List<McpTool> GetAvailableTools()
    {
        return new List<McpTool>
        {
            new McpTool
            {
                Name = "actor_say",
                Description = "Make the desktop pet say text with optional reply context",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "The text to say" },
                        inputText = new { type = "string", description = "Optional: the text being replied to" }
                    },
                    required = new[] { "text" }
                }
            },
            new McpTool
            {
                Name = "actor_display",
                Description = "Display text without speech",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "The text to display" }
                    },
                    required = new[] { "text" }
                }
            },
            new McpTool
            {
                Name = "actor_animate",
                Description = "Perform an animation with parameters",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        animationType = new { type = "string", description = "Type of animation (e.g., dance, celebrate)" },
                        duration = new { type = "number", description = "Duration in milliseconds" }
                    },
                    required = new[] { "animationType" }
                }
            }
        };
    }

    // MCP State Resources
    public ActorStateResource GetState()
    {
        var currentState = _processingCts != null && !_processingCts.IsCancellationRequested
            ? (_actionQueue.Count > 0 ? "busy" : "idle")
            : "stopped";

        return new ActorStateResource
        {
            CurrentState = currentState,
            QueueDepth = _actionQueue.Count,
            LastAction = _lastAction,
            LastActionTimestamp = _lastActionTimestamp,
            TotalActionsProcessed = _totalActionsProcessed,
            ServiceStartTime = _serviceStartTime
        };
    }

    public List<McpResource> GetAvailableResources()
    {
        return new List<McpResource>
        {
            new McpResource
            {
                Uri = "actor://state",
                Name = "Actor State",
                Description = "Current state of the actor service including queue depth and last action",
                MimeType = "application/json"
            },
            new McpResource
            {
                Uri = "actor://queue",
                Name = "Action Queue",
                Description = "Information about the action queue",
                MimeType = "application/json"
            }
        };
    }
}
