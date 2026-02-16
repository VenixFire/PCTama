using Avalonia.Controls;
using PCTama.ActorMCP.ViewModels;

namespace PCTama.ActorMCP.Views;

public partial class ActorWindow : Window
{
    private ActorWindowViewModel? _viewModel;

    public ActorWindow()
    {
        InitializeComponent();
        _viewModel = new ActorWindowViewModel();
        DataContext = _viewModel;
    }

    public ActorWindowViewModel? ViewModel => _viewModel;
}
