using PCTama.ActorMCP.Services;
using PCTama.ActorMCP;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

// Start web server on background thread
var webServerThread = new Thread(() =>
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    app.MapDefaultEndpoints();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthorization();
    app.MapControllers();

    // Run the web app
    app.Run();
});

webServerThread.IsBackground = true;
webServerThread.Start();

// Give web server time to start
Thread.Sleep(1000);

// Run Avalonia on main thread
BuildAvaloniaApp()
    .StartWithClassicDesktopLifetime(args);

static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();
