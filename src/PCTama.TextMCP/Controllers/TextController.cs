using Microsoft.AspNetCore.Mvc;
using PCTama.TextMCP.Services;
using PCTama.TextMCP.Models;

namespace PCTama.TextMCP.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TextController : ControllerBase
{
    private readonly ILogger<TextController> _logger;
    private readonly TextStreamService _textStreamService;

    public TextController(
        ILogger<TextController> logger,
        TextStreamService textStreamService)
    {
        _logger = logger;
        _textStreamService = textStreamService;
    }

    [HttpGet("stream")]
    public async Task<IActionResult> GetLatestText()
    {
        var textData = await _textStreamService.GetLatestTextAsync();
        
        if (textData == null)
        {
            return Ok(new { text = string.Empty, message = "No text available" });
        }

        return Ok(textData.Text);
    }

    [HttpGet("buffer")]
    public async Task<IActionResult> GetAllText()
    {
        var allText = await _textStreamService.GetAllTextAsync();
        return Ok(allText);
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new 
        { 
            bufferCount = _textStreamService.GetBufferCount(),
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
        var tools = _textStreamService.GetAvailableTools();
        return Ok(new McpToolsResponse { Tools = tools });
    }

    // MCP Resource Discovery
    [HttpGet("mcp/resources")]
    public IActionResult GetMcpResources()
    {
        var resources = _textStreamService.GetAvailableResources();
        return Ok(new McpResourcesResponse { Resources = resources });
    }

    // MCP Resource Access
    [HttpGet("mcp/resources/{resourceId}")]
    public async Task<IActionResult> GetMcpResource(string resourceId)
    {
        switch (resourceId.ToLower())
        {
            case "state":
                var state = _textStreamService.GetState();
                return Ok(state);

            case "buffer":
                var buffer = await _textStreamService.GetAllTextAsync();
                return Ok(buffer);

            case "latest":
                var latest = await _textStreamService.GetLatestTextAsync();
                return Ok(latest);

            default:
                return NotFound(new { error = $"Resource '{resourceId}' not found" });
        }
    }}