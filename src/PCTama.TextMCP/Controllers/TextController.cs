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
}
