using PCTama.Controller.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add MCP Client service
builder.Services.AddSingleton<McpClientService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<McpClientService>());

// Add HTTP clients for MCP services
builder.Services.AddHttpClient("textmcp", client =>
{
    client.BaseAddress = new Uri("http://textmcp");
});

builder.Services.AddHttpClient("actormcp", client =>
{
    client.BaseAddress = new Uri("http://actormcp");
});

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

app.Run();

// Make Program class accessible for testing
public partial class Program { }
