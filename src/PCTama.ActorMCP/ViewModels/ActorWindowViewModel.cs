using System.Collections.ObjectModel;
using ReactiveUI;

namespace PCTama.ActorMCP.ViewModels;

public class ActionHistoryItem
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public class ActorWindowViewModel : ReactiveObject
{
    private string _currentAction = "Ready";
    private string _actionDetails = "Waiting for actions...";
    private string _lastUpdated = DateTime.Now.ToString("HH:mm:ss");
    private ObservableCollection<ActionHistoryItem> _actionHistory;

    public string CurrentAction
    {
        get => _currentAction;
        set => this.RaiseAndSetIfChanged(ref _currentAction, value);
    }

    public string ActionDetails
    {
        get => _actionDetails;
        set => this.RaiseAndSetIfChanged(ref _actionDetails, value);
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        set => this.RaiseAndSetIfChanged(ref _lastUpdated, value);
    }

    public ObservableCollection<ActionHistoryItem> ActionHistory
    {
        get => _actionHistory;
        set => this.RaiseAndSetIfChanged(ref _actionHistory, value);
    }

    public ActorWindowViewModel()
    {
        _actionHistory = new ObservableCollection<ActionHistoryItem>();
    }

    public void AddAction(string action, string details = "")
    {
        CurrentAction = action;
        ActionDetails = details;
        LastUpdated = DateTime.Now.ToString("HH:mm:ss");

        var historyItem = new ActionHistoryItem
        {
            Timestamp = DateTime.Now,
            Action = action,
            Details = details
        };

        _actionHistory.Insert(0, historyItem);

        // Keep only last 10 items
        while (_actionHistory.Count > 10)
        {
            _actionHistory.RemoveAt(_actionHistory.Count - 1);
        }
    }

    public void Reset()
    {
        CurrentAction = "Ready";
        ActionDetails = "Waiting for actions...";
        LastUpdated = DateTime.Now.ToString("HH:mm:ss");
    }
}
