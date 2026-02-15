using PCTama.ActorMCP.Models;
using System.Collections.Concurrent;

namespace PCTama.ActorMCP.Services;

public class ActorService : BackgroundService
{
    private readonly ILogger<ActorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ActorConfiguration _config;
    private readonly ConcurrentQueue<ActionRequest> _actionQueue = new();
    private readonly SemaphoreSlim _queueSemaphore = new(1, 1);
    private Thread? _uiThread;

    public ActorService(
        ILogger<ActorService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _config = configuration.GetSection("ActorMcpConfiguration").Get<ActorConfiguration>() 
            ?? new ActorConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Actor MCP Service starting...");
        _logger.LogInformation("Display type: {DisplayType}", _config.DisplayType);

        // Initialize WinUI3 on a separate thread (required for WinUI)
        if (OperatingSystem.IsWindows() && _config.DisplayType == "WinUI3")
        {
            InitializeWinUI3();
        }

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

    private void InitializeWinUI3()
    {
        _logger.LogInformation("Initializing WinUI3 display...");

        // WinUI3 must run on its own thread with a message pump
        _uiThread = new Thread(() =>
        {
            try
            {
                // TODO: Initialize actual WinUI3 window
                // This is a placeholder - actual implementation would use Microsoft.UI.Xaml
                _logger.LogInformation("WinUI3 window initialized");
                
                // Keep the UI thread alive
                while (true)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WinUI3 thread");
            }
        });

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Start();
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
        
        // TODO: Integrate with speech synthesis or display in WinUI3 window
        await Task.CompletedTask;
    }

    private async Task DisplayAsync(string text)
    {
        _logger.LogInformation("Display action: {Text}", text);
        
        // TODO: Update WinUI3 window with text
        await Task.CompletedTask;
    }

    private async Task AnimateAsync(Dictionary<string, object> parameters)
    {
        _logger.LogInformation("Animate action with parameters: {Parameters}", 
            string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")));
        
        // TODO: Perform animation in WinUI3 window
        await Task.CompletedTask;
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
