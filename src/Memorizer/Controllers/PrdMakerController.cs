using Memorizer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Memorizer.Controllers;

/// <summary>
/// UI Controller for PRD Maker feature
/// </summary>
[Route("ui/prd")]
public class PrdMakerViewController : Controller
{
    /// <summary>
    /// PRD Maker main page
    /// </summary>
    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Shared PRD view page
    /// </summary>
    [HttpGet]
    [Route("share/{shortCode}")]
    public IActionResult Share(string shortCode)
    {
        ViewBag.ShortCode = shortCode;
        return View("Share");
    }

    /// <summary>
    /// Shared PRD list page
    /// </summary>
    [HttpGet]
    [Route("shares")]
    public IActionResult ShareList()
    {
        return View("ShareList");
    }
}

/// <summary>
/// API Controller for PRD Maker feature
/// Analyzes PRD using Event Storming and Example Mapping with LLM-EX
/// </summary>
[Route("api/prd")]
[ApiController]
[AllowAnonymous]
public class PrdMakerController : ControllerBase
{
    private readonly ILlmExService _llmExService;
    private readonly ILlmService _llmService;
    private readonly ILogger<PrdMakerController> _logger;
    private readonly Npgsql.NpgsqlDataSource _dataSource;

    public PrdMakerController(
        ILlmExService llmExService,
        ILlmService llmService,
        ILogger<PrdMakerController> logger,
        Npgsql.NpgsqlDataSource dataSource)
    {
        _llmExService = llmExService;
        _llmService = llmService;
        _logger = logger;
        _dataSource = dataSource;
    }

    /// <summary>
    /// Generate Event Storming analysis from PRD (streaming)
    /// </summary>
    [HttpPost("event-storming")]
    public async Task GenerateEventStorming([FromBody] PrdAnalysisRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            if (string.IsNullOrWhiteSpace(request.PrdContent))
            {
                await WriteSSEEvent("error", new { message = "PRD content is required" });
                return;
            }

            var prompt = PrdMakerPrompts.GetEventStormingPrompt(request.PrdContent);

            _logger.LogInformation("Generating Event Storming analysis for PRD");

            await foreach (var chunk in _llmExService.CompleteStreamingAsync(prompt, HttpContext.RequestAborted))
            {
                await WriteSSEEvent("chunk", new { content = chunk });
            }

            await WriteSSEEvent("done", new { success = true });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Event Storming generation cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Event Storming analysis");
            await WriteSSEEvent("error", new { message = "Failed to generate Event Storming analysis" });
        }
    }

    /// <summary>
    /// Generate Example Mapping discussion from Event Storming result (streaming)
    /// </summary>
    [HttpPost("example-mapping-discussion")]
    public async Task GenerateExampleMappingDiscussion([FromBody] ExampleMappingRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            if (string.IsNullOrWhiteSpace(request.PrdContent) || string.IsNullOrWhiteSpace(request.EventStormingResult))
            {
                await WriteSSEEvent("error", new { message = "PRD content and Event Storming result are required" });
                return;
            }

            var prompt = PrdMakerPrompts.GetExampleMappingDiscussionPrompt(request.PrdContent, request.EventStormingResult);

            _logger.LogInformation("Generating Example Mapping discussion");

            await foreach (var chunk in _llmExService.CompleteStreamingAsync(prompt, HttpContext.RequestAborted))
            {
                await WriteSSEEvent("chunk", new { content = chunk });
            }

            await WriteSSEEvent("done", new { success = true });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Example Mapping discussion cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Example Mapping discussion");
            await WriteSSEEvent("error", new { message = "Failed to generate Example Mapping discussion" });
        }
    }

    /// <summary>
    /// Generate final Example Mapping result (streaming)
    /// </summary>
    [HttpPost("example-mapping-result")]
    public async Task GenerateExampleMappingResult([FromBody] FinalExampleMappingRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            if (string.IsNullOrWhiteSpace(request.PrdContent) ||
                string.IsNullOrWhiteSpace(request.EventStormingResult) ||
                string.IsNullOrWhiteSpace(request.DiscussionResult))
            {
                await WriteSSEEvent("error", new { message = "All previous results are required" });
                return;
            }

            var prompt = PrdMakerPrompts.GetExampleMappingResultPrompt(
                request.PrdContent,
                request.EventStormingResult,
                request.DiscussionResult);

            _logger.LogInformation("Generating final Example Mapping result");

            await foreach (var chunk in _llmExService.CompleteStreamingAsync(prompt, HttpContext.RequestAborted))
            {
                await WriteSSEEvent("chunk", new { content = chunk });
            }

            await WriteSSEEvent("done", new { success = true });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Example Mapping result generation cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Example Mapping result");
            await WriteSSEEvent("error", new { message = "Failed to generate Example Mapping result" });
        }
    }

    /// <summary>
    /// Generate refined PRD based on all analysis results (streaming)
    /// </summary>
    [HttpPost("refined-prd")]
    public async Task GenerateRefinedPrd([FromBody] RefinedPrdRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            if (string.IsNullOrWhiteSpace(request.PrdContent) ||
                string.IsNullOrWhiteSpace(request.EventStormingResult) ||
                string.IsNullOrWhiteSpace(request.DiscussionResult) ||
                string.IsNullOrWhiteSpace(request.ExampleMappingResult))
            {
                await WriteSSEEvent("error", new { message = "All previous results are required" });
                return;
            }

            var prompt = PrdMakerPrompts.GetRefinedPrdPrompt(
                request.PrdContent,
                request.EventStormingResult,
                request.DiscussionResult,
                request.ExampleMappingResult);

            _logger.LogInformation("Generating refined PRD");

            await foreach (var chunk in _llmExService.CompleteStreamingAsync(prompt, HttpContext.RequestAborted))
            {
                await WriteSSEEvent("chunk", new { content = chunk });
            }

            await WriteSSEEvent("done", new { success = true });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Refined PRD generation cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refined PRD");
            await WriteSSEEvent("error", new { message = "Failed to generate refined PRD" });
        }
    }

    /// <summary>
    /// Generate a title for PRD analysis using LLM
    /// </summary>
    [HttpPost("generate-title")]
    public async Task<ActionResult> GenerateTitle([FromBody] GenerateTitleRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PrdContent))
            {
                return BadRequest(new { error = "PRD content is required" });
            }

            var prompt = $@"다음 PRD(Product Requirements Document) 내용을 읽고, 이 문서를 대표할 수 있는 간결한 제목을 만들어주세요.

## PRD 내용 (앞부분)
{request.PrdContent.Substring(0, Math.Min(request.PrdContent.Length, 500))}

## 규칙
- 제목은 반드시 30자 이내로 작성
- 핵심 기능이나 프로젝트명을 포함
- 한국어로 작성
- 제목만 출력 (설명, 따옴표, 접두사 없이)

제목:";

            var title = await _llmService.CompleteAsync(prompt);

            // Clean up the title
            title = title.Trim()
                .Replace("\"", "")
                .Replace("제목:", "")
                .Replace("Title:", "")
                .Trim();

            // Ensure max 30 characters
            if (title.Length > 30)
            {
                title = title.Substring(0, 27) + "...";
            }

            _logger.LogInformation("Generated PRD title: {Title}", title);

            return Ok(new { title });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PRD title");
            return StatusCode(500, new { error = "Failed to generate title" });
        }
    }

    /// <summary>
    /// Share PRD analysis result
    /// </summary>
    [HttpPost("share")]
    public async Task<ActionResult> SharePrdAnalysis([FromBody] SharePrdRequest request)
    {
        try
        {
            var shortCode = GenerateShortCode();

            await using var conn = await _dataSource.OpenConnectionAsync();

            var insertQuery = @"
                INSERT INTO prd_share_links
                    (short_code, title, prd_content, event_storming_result,
                     discussion_result, example_mapping_result, refined_prd_result, created_at)
                VALUES
                    (@shortCode, @title, @prdContent, @eventStormingResult,
                     @discussionResult, @exampleMappingResult, @refinedPrdResult, @createdAt)";

            await using var cmd = new Npgsql.NpgsqlCommand(insertQuery, conn);
            cmd.Parameters.AddWithValue("shortCode", shortCode);
            cmd.Parameters.AddWithValue("title", request.Title ?? "PRD Analysis");
            cmd.Parameters.AddWithValue("prdContent", request.PrdContent);
            cmd.Parameters.AddWithValue("eventStormingResult", request.EventStormingResult);
            cmd.Parameters.AddWithValue("discussionResult", request.DiscussionResult ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("exampleMappingResult", request.ExampleMappingResult ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("refinedPrdResult", request.RefinedPrdResult ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Created PRD share link {ShortCode}", shortCode);

            return Ok(new { shortCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing PRD analysis");
            return StatusCode(500, new { error = "Failed to create share link" });
        }
    }

    /// <summary>
    /// Get shared PRD by short code
    /// </summary>
    [HttpGet("share/{shortCode}")]
    public async Task<ActionResult> GetSharedPrd(string shortCode)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            var query = @"
                SELECT title, prd_content, event_storming_result,
                       discussion_result, example_mapping_result, refined_prd_result, created_at
                FROM prd_share_links
                WHERE short_code = @shortCode
                LIMIT 1";

            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("shortCode", shortCode);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var content = new
                {
                    title = reader.GetString(0),
                    prdContent = reader.GetString(1),
                    eventStormingResult = reader.GetString(2),
                    discussionResult = reader.IsDBNull(3) ? null : reader.GetString(3),
                    exampleMappingResult = reader.IsDBNull(4) ? null : reader.GetString(4),
                    refinedPrdResult = reader.IsDBNull(5) ? null : reader.GetString(5)
                };
                var createdAt = reader.GetDateTime(6);

                return Ok(new { content, createdAt, shortCode });
            }

            return NotFound(new { error = "PRD share not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared PRD");
            return StatusCode(500, new { error = "Failed to get shared PRD" });
        }
    }

    /// <summary>
    /// Get list of shared PRDs with pagination
    /// </summary>
    [HttpGet("shares")]
    public async Task<ActionResult> GetSharedPrds(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            var countQuery = "SELECT COUNT(*) FROM prd_share_links";
            await using var countCmd = new Npgsql.NpgsqlCommand(countQuery, conn);
            var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            var offset = (page - 1) * pageSize;
            var query = @"
                SELECT short_code, title, prd_content, created_at
                FROM prd_share_links
                ORDER BY created_at DESC
                LIMIT @pageSize OFFSET @offset";

            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("pageSize", pageSize);
            cmd.Parameters.AddWithValue("offset", offset);

            var items = new List<object>();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var prdContent = reader.GetString(2);
                var summary = ExtractSummaryFromContent(prdContent);

                items.Add(new
                {
                    shortCode = reader.GetString(0),
                    title = reader.GetString(1),
                    summary = summary,
                    createdAt = reader.GetDateTime(3)
                });
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return Ok(new
            {
                items,
                page,
                pageSize,
                totalCount,
                totalPages,
                hasMore = page < totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared PRDs list");
            return StatusCode(500, new { error = "Failed to get shared PRDs" });
        }
    }

    private async Task WriteSSEEvent(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var message = $"event: {eventType}\ndata: {json}\n\n";
        var bytes = Encoding.UTF8.GetBytes(message);

        await Response.Body.WriteAsync(bytes, 0, bytes.Length);
        await Response.Body.FlushAsync();
    }

    private string GenerateShortCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var shortCode = new char[6];

        for (int i = 0; i < 6; i++)
        {
            shortCode[i] = chars[random.Next(chars.Length)];
        }

        return new string(shortCode);
    }

    private static string ExtractSummaryFromContent(string content)
    {
        if (string.IsNullOrEmpty(content)) return "";

        var lines = content.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .Take(3);

        var summary = string.Join(" ", lines);
        if (summary.Length > 200)
        {
            summary = summary[..200] + "...";
        }

        return summary;
    }
}

/// <summary>
/// Request for PRD analysis (Event Storming)
/// </summary>
public class PrdAnalysisRequest
{
    public string PrdContent { get; set; } = string.Empty;
}

/// <summary>
/// Request for Example Mapping discussion
/// </summary>
public class ExampleMappingRequest
{
    public string PrdContent { get; set; } = string.Empty;
    public string EventStormingResult { get; set; } = string.Empty;
}

/// <summary>
/// Request for final Example Mapping result
/// </summary>
public class FinalExampleMappingRequest
{
    public string PrdContent { get; set; } = string.Empty;
    public string EventStormingResult { get; set; } = string.Empty;
    public string DiscussionResult { get; set; } = string.Empty;
}

/// <summary>
/// Request for refined PRD generation
/// </summary>
public class RefinedPrdRequest
{
    public string PrdContent { get; set; } = string.Empty;
    public string EventStormingResult { get; set; } = string.Empty;
    public string DiscussionResult { get; set; } = string.Empty;
    public string ExampleMappingResult { get; set; } = string.Empty;
}

/// <summary>
/// Request for generating PRD title
/// </summary>
public class GenerateTitleRequest
{
    public string PrdContent { get; set; } = string.Empty;
}

/// <summary>
/// Request for sharing PRD analysis
/// </summary>
public class SharePrdRequest
{
    public string? Title { get; set; }
    public string PrdContent { get; set; } = string.Empty;
    public string EventStormingResult { get; set; } = string.Empty;
    public string? DiscussionResult { get; set; }
    public string? ExampleMappingResult { get; set; }
    public string? RefinedPrdResult { get; set; }
}
