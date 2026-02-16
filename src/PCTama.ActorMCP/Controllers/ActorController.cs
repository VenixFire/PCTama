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
    public async Task<IActionResult> Say([FromBody] string text)
    {
        var request = new ActionRequest
        {
            Action = "say",
            Text = text
        };

        return await PerformAction(request);
    }

    [HttpPost("display")]
    public async Task<IActionResult> Display([FromBody] string text)
    {
        var request = new ActionRequest
        {
            Action = "display",
            Text = text
        };

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
}
