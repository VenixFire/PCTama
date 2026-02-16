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
        _logger.LogInformation("Actor MCP Service starting...");
        _logger.LogInformation("Display type: {DisplayType}", _config.DisplayType);

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
                _logger.LogError(ex, "Error processing action");
            }
        }
    }

    private async Task ProcessActionAsync(ActionRequest action, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing action: {Action}", action.Action);

        switch (action.Action.ToLower())
        {
            case "say":
                await SayAsync(action.Text ?? string.Empty);
                break;
            case "display":
                await DisplayAsync(action.Text ?? string.Empty);
                break;
            case "animate":
                await AnimateAsync(action.Parameters);
                break;
            default:
                _logger.LogWarning("Unknown action: {Action}", action.Action);
                break;
        }
    }

    private async Task SayAsync(string text)
    {
        _logger.LogInformation("Say action: {Text}", text);
        UpdateWindowDisplay("Say", text);
        await Task.CompletedTask;
    }

    private async Task DisplayAsync(string text)
    {
        _logger.LogInformation("Display action: {Text}", text);
        UpdateWindowDisplay("Display", text);
        await Task.CompletedTask;
    }

    private async Task AnimateAsync(Dictionary<string, object> parameters)
    {
        _logger.LogInformation("Animate action with parameters: {Parameters}", 
            string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")));
        
        var parameterString = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));
        UpdateWindowDisplay("Animate", parameterString);
        await Task.CompletedTask;
    }

    private void UpdateWindowDisplay(string action, string details)
    {
        if (_window?.ViewModel != null)
        {
            try
            {
                // Dispatcher.UIThread is the Avalonia thread dispatcher
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _window.ViewModel.AddAction(action, details);
                }, Avalonia.Threading.DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating window display");
            }
        }
    }

    public async Task<ActionResult> EnqueueActionAsync(ActionRequest action)
    {
        await _queueSemaphore.WaitAsync();
        try
        {
            _actionQueue.Enqueue(action);
            _logger.LogInformation("Action enqueued: {Action}", action.Action);
            
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
}
