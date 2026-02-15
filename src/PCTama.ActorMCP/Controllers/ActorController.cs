using Microsoft.AspNetCore.Mvc;
using PCTama.ActorMCP.Services;
using PCTama.ActorMCP.Models;

namespace PCTama.ActorMCP.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActorController : ControllerBase
{
    private readonly ILogger<ActorController> _logger;
    private readonly ActorService _actorService;

    public ActorController(
        ILogger<ActorController> logger,
        ActorService actorService)
    {
        _logger = logger;
        _actorService = actorService;
    }

    [HttpPost("perform")]
    public async Task<IActionResult> PerformAction([FromBody] ActionRequest request)
    {
        try
        {
            var result = await _actorService.EnqueueActionAsync(request);
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
        return Ok(new 
        { 
            queueCount = _actorService.GetQueueCount(),
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
