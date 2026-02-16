using Microsoft.AspNetCore.Mvc;
using PCTama.ActorMCP.Services;
using PCTama.ActorMCP.Models;

namespace PCTama.ActorMCP.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActorController : ControllerBase
{
    private readonly ILogger<ActorController> _logger;

    public ActorController(ILogger<ActorController> logger)
    {
        _logger = logger;
    }

    private ActorService? GetActorService()
    {
        return App.ActorServiceInstance;
    }

    [HttpPost("perform")]
    public async Task<IActionResult> PerformAction([FromBody] ActionRequest request)
    {
        try
        {
            var actorService = GetActorService();
            if (actorService == null)
            {
                return StatusCode(503, new Models.ActionResult
                {
                    Success = false,
                    Message = "Actor service not initialized"
                });
            }

            var result = await actorService.EnqueueActionAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing action");
            return StatusCode(500, new Models.ActionResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    [HttpPost("say")]
    public async Task<IActionResult> Say([FromBody] ActionRequest request)
    {
        request.Action = string.IsNullOrWhiteSpace(request.Action) ? "say" : request.Action;
        return await PerformAction(request);
    }

    [HttpPost("display")]
    public async Task<IActionResult> Display([FromBody] ActionRequest request)
    {
        request.Action = string.IsNullOrWhiteSpace(request.Action) ? "display" : request.Action;
        return await PerformAction(request);
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var actorService = GetActorService();
        if (actorService == null)
        {
            return StatusCode(503, new { status = "not initialized" });
        }

        return Ok(new 
        { 
            queueCount = actorService.GetQueueCount(),
            status = "active",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    // MCP Tool Discovery
    [HttpGet("mcp/tools")]
    public IActionResult GetMcpTools()
    {
        var actorService = GetActorService();
        if (actorService == null)
        {
            return StatusCode(503, new { error = "Actor service not initialized" });
        }

        var tools = actorService.GetAvailableTools();
        return Ok(new McpToolsResponse { Tools = tools });
    }

    // MCP Resource Discovery
    [HttpGet("mcp/resources")]
    public IActionResult GetMcpResources()
    {
        var actorService = GetActorService();
        if (actorService == null)
        {
            return StatusCode(503, new { error = "Actor service not initialized" });
        }

        var resources = actorService.GetAvailableResources();
        return Ok(new McpResourcesResponse { Resources = resources });
    }

    // MCP Resource Access
    [HttpGet("mcp/resources/{resourceId}")]
    public IActionResult GetMcpResource(string resourceId)
    {
        var actorService = GetActorService();
        if (actorService == null)
        {
            return StatusCode(503, new { error = "Actor service not initialized" });
        }

        switch (resourceId.ToLower())
        {
            case "state":
                var state = actorService.GetState();
                return Ok(state);

            case "queue":
                return Ok(new
                {
                    queueDepth = actorService.GetQueueCount(),
                    timestamp = DateTime.UtcNow
                });

            default:
                return NotFound(new { error = $"Resource '{resourceId}' not found" });
        }
    }
}
