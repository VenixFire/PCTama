using Microsoft.AspNetCore.Mvc;
using PCTama.Controller.Services;

namespace PCTama.Controller.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ControllerController : ControllerBase
{
    private readonly ILogger<ControllerController> _logger;
    private readonly McpClientService _mcpClientService;

    public ControllerController(
        ILogger<ControllerController> logger,
        McpClientService mcpClientService)
    {
        _logger = logger;
        _mcpClientService = mcpClientService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _mcpClientService.GetStatusAsync();
        return Ok(status);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
