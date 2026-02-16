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
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
        { 
            Title = "PCTama Actor MCP API",
            Version = "v1",
            Description = "Actor service for PCTama desktop pet"
        });
    });

    var app = builder.Build();

    app.MapDefaultEndpoints();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "PCTama Actor MCP API v1");
            c.RoutePrefix = "swagger";
        });
        
        // Add resource link for Aspire dashboard
        app.MapGet("/", context =>
        {
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>PCTama Actor MCP</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
                        .container { max-width: 600px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; }
                        h1 { color: #333; }
                        a { display: inline-block; margin: 10px 0; padding: 10px 20px; background: #0066cc; color: white; text-decoration: none; border-radius: 4px; }
                        a:hover { background: #0052a3; }
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <h1>PCTama Actor MCP</h1>
                        <p>Actor service for the PCTama desktop pet.</p>
                        <a href=""/swagger"">📚 API Documentation (Swagger/OpenAPI)</a>
                        <a href=""/health"">🏥 Health Check</a>
                    </div>
                </body>
                </html>
            ");
        });
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
