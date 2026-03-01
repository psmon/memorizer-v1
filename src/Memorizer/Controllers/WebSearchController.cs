using Memorizer.Services;
using Memorizer.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Memorizer.Controllers;

/// <summary>
/// Public web search API controller (no authentication required).
/// Supports search and page-read flows with Fetch/Headless/API modes.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/websearch")]
public sealed class WebSearchController : ControllerBase
{
    private readonly IWebSearchService _webSearchService;
    private readonly ILogger<WebSearchController> _logger;

    public WebSearchController(
        IWebSearchService webSearchService,
        ILogger<WebSearchController> logger)
    {
        _webSearchService = webSearchService;
        _logger = logger;
    }

    /// <summary>
    /// Searches the web with a provider (google/bing/naver).
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(WebSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Search(
        [FromBody] WebSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "query is required" });
        }

        if (!TryParseProvider(request.Provider, out var provider))
        {
            return BadRequest(new { error = "provider must be one of: google, bing, naver" });
        }

        if (!TryParseMode(request.AccessMode, out var mode, out var modeError))
        {
            return BadRequest(new { error = modeError });
        }

        try
        {
            var result = await _webSearchService.SearchAsync(
                provider,
                request.Query,
                request.MaxResults.GetValueOrDefault(5),
                mode,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web search failed. Provider: {Provider}, Query: {Query}", request.Provider, request.Query);
            return StatusCode(500, new { error = "web search failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Searches and reads the top result page.
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(WebSearchPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchPreview(
        [FromBody] WebSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "query is required" });
        }

        if (!TryParseProvider(request.Provider, out var provider))
        {
            return BadRequest(new { error = "provider must be one of: google, bing, naver" });
        }

        if (!TryParseMode(request.AccessMode, out var mode, out var modeError))
        {
            return BadRequest(new { error = modeError });
        }

        try
        {
            var result = await _webSearchService.SearchAndReadTopResultAsync(
                provider,
                request.Query,
                request.MaxResults.GetValueOrDefault(5),
                mode,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web search preview failed. Provider: {Provider}, Query: {Query}", request.Provider, request.Query);
            return StatusCode(500, new { error = "web search preview failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Reads a specific web page.
    /// </summary>
    [HttpPost("read-page")]
    [ProducesResponseType(typeof(PageReadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReadPage(
        [FromBody] ReadPageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { error = "url is required" });
        }

        if (!TryParseMode(request.AccessMode, out var mode, out var modeError))
        {
            return BadRequest(new { error = modeError });
        }

        try
        {
            var result = await _webSearchService.ReadPageAsync(
                request.Url,
                mode,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Read page failed. Url: {Url}", request.Url);
            return StatusCode(500, new { error = "read page failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Returns supported providers and access modes.
    /// </summary>
    [HttpGet("capabilities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Capabilities()
    {
        return Ok(new
        {
            providers = new[] { "google", "bing", "naver" },
            accessModes = new[] { "api", "fetch", "headless" }
        });
    }

    private static bool TryParseProvider(string? value, out WebSearchProvider provider)
    {
        return Enum.TryParse(value, true, out provider);
    }

    private static bool TryParseMode(string? value, out WebSearchAccessMode? mode, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            mode = null;
            error = null;
            return true;
        }

        if (Enum.TryParse<WebSearchAccessMode>(value, true, out var parsed))
        {
            mode = parsed;
            error = null;
            return true;
        }

        mode = null;
        error = "accessMode must be one of: api, fetch, headless";
        return false;
    }
}

public sealed class WebSearchRequest
{
    public string Provider { get; set; } = "google";
    public string Query { get; set; } = string.Empty;
    public int? MaxResults { get; set; } = 5;
    public string? AccessMode { get; set; }
}

public sealed class ReadPageRequest
{
    public string Url { get; set; } = string.Empty;
    public string? AccessMode { get; set; }
}
