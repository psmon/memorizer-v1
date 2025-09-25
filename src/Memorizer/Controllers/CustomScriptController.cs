using Memorizer.Models;
using Memorizer.Services;
using Microsoft.AspNetCore.Mvc;

namespace Memorizer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomScriptController : ControllerBase
{
    private readonly ICustomScriptService _scriptService;
    private readonly ILogger<CustomScriptController> _logger;

    public CustomScriptController(
        ICustomScriptService scriptService,
        ILogger<CustomScriptController> logger)
    {
        _scriptService = scriptService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomScript>>> GetAllScripts(CancellationToken cancellationToken)
    {
        var isAuthenticated = HttpContext.Items["IsAuthenticated"] as bool? ?? false;
        if (!isAuthenticated)
        {
            return Unauthorized(new { error = "Authentication required" });
        }

        var scripts = await _scriptService.GetAllScriptsAsync(cancellationToken);
        return Ok(scripts);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<CustomScript>> GetScriptByName(string name, CancellationToken cancellationToken)
    {
        var isAuthenticated = HttpContext.Items["IsAuthenticated"] as bool? ?? false;
        if (!isAuthenticated)
        {
            return Unauthorized(new { error = "Authentication required" });
        }

        var script = await _scriptService.GetScriptByNameAsync(name, cancellationToken);
        if (script == null)
        {
            return NotFound(new { error = $"Script '{name}' not found" });
        }

        return Ok(script);
    }

    [HttpPost]
    public async Task<ActionResult<CustomScript>> CreateOrUpdateScript(
        [FromBody] CreateCustomScriptRequest request,
        CancellationToken cancellationToken)
    {
        var isAuthenticated = HttpContext.Items["IsAuthenticated"] as bool? ?? false;
        if (!isAuthenticated)
        {
            return Unauthorized(new { error = "Authentication required" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        if (string.IsNullOrWhiteSpace(request.ScriptContent))
        {
            return BadRequest(new { error = "ScriptContent is required" });
        }

        var script = await _scriptService.CreateOrUpdateScriptAsync(
            request.Name,
            request.ScriptContent,
            request.IsActive,
            cancellationToken);

        return Ok(script);
    }

    [HttpPut("{name}")]
    public async Task<ActionResult<CustomScript>> UpdateScript(
        string name,
        [FromBody] UpdateCustomScriptRequest request,
        CancellationToken cancellationToken)
    {
        var isAuthenticated = HttpContext.Items["IsAuthenticated"] as bool? ?? false;
        if (!isAuthenticated)
        {
            return Unauthorized(new { error = "Authentication required" });
        }

        var existingScript = await _scriptService.GetScriptByNameAsync(name, cancellationToken);
        if (existingScript == null)
        {
            return NotFound(new { error = $"Script '{name}' not found" });
        }

        var script = await _scriptService.CreateOrUpdateScriptAsync(
            name,
            request.ScriptContent,
            request.IsActive,
            cancellationToken);

        return Ok(script);
    }

    [HttpDelete("{name}")]
    public async Task<ActionResult> DeleteScript(string name, CancellationToken cancellationToken)
    {
        var isAuthenticated = HttpContext.Items["IsAuthenticated"] as bool? ?? false;
        if (!isAuthenticated)
        {
            return Unauthorized(new { error = "Authentication required" });
        }

        var deleted = await _scriptService.DeleteScriptAsync(name, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { error = $"Script '{name}' not found" });
        }

        return Ok(new { message = $"Script '{name}' deleted successfully" });
    }
}