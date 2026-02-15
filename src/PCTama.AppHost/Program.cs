var builder = DistributedApplication.CreateBuilder(args);

// Add MCP services
var textMcp = builder.AddProject<Projects.PCTama_TextMCP>("textmcp")
    .WithHttpEndpoint(port: 5001, name: "http");

var actorMcp = builder.AddProject<Projects.PCTama_ActorMCP>("actormcp")
    .WithHttpEndpoint(port: 5002, name: "http");

// Add main controller service with references to MCPs
var controller = builder.AddProject<Projects.PCTama_Controller>("controller")
    .WithHttpEndpoint(port: 5003, name: "http")
    .WithReference(textMcp)
    .WithReference(actorMcp);

builder.Build().Run();
