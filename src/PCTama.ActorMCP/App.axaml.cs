using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PCTama.ActorMCP.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PCTama.ActorMCP.Services;
using PCTama.ActorMCP.Models;

namespace PCTama.ActorMCP;

public class App : Application
{
    public static ActorService? ActorServiceInstance { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var config = configuration.GetSection("ActorMcpConfiguration").Get<ActorConfiguration>() 
                ?? new ActorConfiguration();

            // Create logger
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var logger = loggerFactory.CreateLogger<ActorService>();

            // Create actor service instance
            ActorServiceInstance = new ActorService(logger, configuration);

            // Create and show main window
            var window = new ActorWindow
            {
                Title = config.WindowTitle,
                Width = config.WindowWidth,
                Height = config.WindowHeight,
                Topmost = config.AlwaysOnTop
            };

            ActorServiceInstance.SetWindow(window);

            desktop.MainWindow = window;

            // Start processing actions
            _ = ActorServiceInstance.StartProcessingAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
