using Memorizer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Memorizer.Controllers;

/// <summary>
/// UI Controller for Shape Up Whiteboard feature
/// </summary>
[Route("ui/shapeup")]
public class ShapeUpViewController : Controller
{
    /// <summary>
    /// Shape Up Whiteboard main page
    /// </summary>
    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Shared Shape Up board view page
    /// </summary>
    [HttpGet]
    [Route("share/{shortCode}")]
    public IActionResult Share(string shortCode)
    {
        ViewBag.ShortCode = shortCode;
        return View("Share");
    }

    /// <summary>
    /// Shared Shape Up board list page
    /// </summary>
    [HttpGet]
    [Route("shares")]
    public IActionResult ShareList()
    {
        return View("ShareList");
    }
}

/// <summary>
/// API Controller for Shape Up Whiteboard feature
/// Generates Shape Up boards using LLM-EX
/// </summary>
[Route("api/shapeup")]
[ApiController]
[AllowAnonymous]
public class ShapeUpController : ControllerBase
{
    private readonly ILlmExService _llmExService;
    private readonly ILlmService _llmService;
    private readonly ILogger<ShapeUpController> _logger;
    private readonly Npgsql.NpgsqlDataSource _dataSource;

    public ShapeUpController(
        ILlmExService llmExService,
        ILlmService llmService,
        ILogger<ShapeUpController> logger,
        Npgsql.NpgsqlDataSource dataSource)
    {
        _llmExService = llmExService;
        _llmService = llmService;
        _logger = logger;
        _dataSource = dataSource;
    }

    /// <summary>
    /// Generate Shape Up board using AI (streaming)
    /// </summary>
    [HttpPost("generate")]
    public async Task GenerateBoard([FromBody] GenerateBoardRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                await WriteSSEEvent("error", new { message = "Prompt is required" });
                return;
            }

            var prompt = ShapeUpPrompts.GetBoardGenerationPrompt(request.Prompt, request.BoardType, request.IsIntegrated);

            _logger.LogInformation("Generating Shape Up board of type {BoardType}, integrated: {IsIntegrated}",
                request.BoardType, request.IsIntegrated);

            await foreach (var chunk in _llmExService.CompleteStreamingAsync(prompt, HttpContext.RequestAborted))
            {
                await WriteSSEEvent("chunk", new { content = chunk });
            }

            await WriteSSEEvent("done", new { success = true });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Shape Up board generation cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Shape Up board");
            await WriteSSEEvent("error", new { message = "Failed to generate Shape Up board" });
        }
    }

    /// <summary>
    /// Generate a title for Shape Up board using LLM
    /// </summary>
    [HttpPost("generate-title")]
    public async Task<ActionResult> GenerateTitle([FromBody] GenerateShapeUpTitleRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { error = "Prompt is required" });
            }

            var llmPrompt = $@"다음 프롬프트를 읽고, Shape Up 보드의 제목을 만들어주세요.

## 프롬프트
{request.Prompt.Substring(0, Math.Min(request.Prompt.Length, 300))}

## 규칙
- 제목은 반드시 30자 이내로 작성
- 핵심 기능이나 프로젝트명을 포함
- 한국어로 작성
- 제목만 출력 (설명, 따옴표, 접두사 없이)

제목:";

            var title = await _llmService.CompleteAsync(llmPrompt);

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

            _logger.LogInformation("Generated Shape Up title: {Title}", title);

            return Ok(new { title });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Shape Up title");
            return StatusCode(500, new { error = "Failed to generate title" });
        }
    }

    /// <summary>
    /// Share Shape Up board
    /// </summary>
    [HttpPost("share")]
    public async Task<ActionResult> ShareBoard([FromBody] ShareBoardRequest request)
    {
        try
        {
            var shortCode = GenerateShortCode();

            await using var conn = await _dataSource.OpenConnectionAsync();

            var insertQuery = @"
                INSERT INTO shapeup_share_links
                    (short_code, title, board_data, board_type, original_prompt, created_at)
                VALUES
                    (@shortCode, @title, @boardData::jsonb, @boardType, @originalPrompt, @createdAt)";

            await using var cmd = new Npgsql.NpgsqlCommand(insertQuery, conn);
            cmd.Parameters.AddWithValue("shortCode", shortCode);
            cmd.Parameters.AddWithValue("title", request.Title ?? "Shape Up Board");
            cmd.Parameters.AddWithValue("boardData", request.BoardData);
            cmd.Parameters.AddWithValue("boardType", request.BoardType ?? "mixed");
            cmd.Parameters.AddWithValue("originalPrompt", request.OriginalPrompt ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Created Shape Up share link {ShortCode}", shortCode);

            return Ok(new { shortCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing Shape Up board");
            return StatusCode(500, new { error = "Failed to create share link" });
        }
    }

    /// <summary>
    /// Get shared Shape Up board by short code
    /// </summary>
    [HttpGet("share/{shortCode}")]
    public async Task<ActionResult> GetSharedBoard(string shortCode)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            var query = @"
                SELECT title, board_data, board_type, original_prompt, created_at
                FROM shapeup_share_links
                WHERE short_code = @shortCode
                LIMIT 1";

            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("shortCode", shortCode);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Ok(new
                {
                    shortCode,
                    title = reader.GetString(0),
                    boardData = reader.GetString(1),
                    boardType = reader.IsDBNull(2) ? null : reader.GetString(2),
                    originalPrompt = reader.IsDBNull(3) ? null : reader.GetString(3),
                    createdAt = reader.GetDateTime(4)
                });
            }

            return NotFound(new { error = "Shape Up board not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared Shape Up board");
            return StatusCode(500, new { error = "Failed to get shared board" });
        }
    }

    /// <summary>
    /// Get list of shared Shape Up boards with pagination
    /// </summary>
    [HttpGet("shares")]
    public async Task<ActionResult> GetSharedBoards(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            var countQuery = "SELECT COUNT(*) FROM shapeup_share_links";
            await using var countCmd = new Npgsql.NpgsqlCommand(countQuery, conn);
            var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            var offset = (page - 1) * pageSize;
            var query = @"
                SELECT short_code, title, board_type, original_prompt, created_at
                FROM shapeup_share_links
                ORDER BY created_at DESC
                LIMIT @pageSize OFFSET @offset";

            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("pageSize", pageSize);
            cmd.Parameters.AddWithValue("offset", offset);

            var items = new List<object>();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var originalPrompt = reader.IsDBNull(3) ? "" : reader.GetString(3);
                var summary = originalPrompt.Length > 100 ? originalPrompt.Substring(0, 100) + "..." : originalPrompt;

                items.Add(new
                {
                    shortCode = reader.GetString(0),
                    title = reader.GetString(1),
                    boardType = reader.IsDBNull(2) ? null : reader.GetString(2),
                    summary = summary,
                    createdAt = reader.GetDateTime(4)
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
            _logger.LogError(ex, "Error getting shared Shape Up boards list");
            return StatusCode(500, new { error = "Failed to get shared boards" });
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
}

/// <summary>
/// Request for generating Shape Up board
/// </summary>
public class GenerateBoardRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string BoardType { get; set; } = "pitch";  // problem, breadboard, fat-marker, risk, pitch
    public bool IsIntegrated { get; set; } = false;  // For integrated generation (step-by-step)
}

/// <summary>
/// Request for generating Shape Up title
/// </summary>
public class GenerateShapeUpTitleRequest
{
    public string Prompt { get; set; } = string.Empty;
}

/// <summary>
/// Request for sharing Shape Up board
/// </summary>
public class ShareBoardRequest
{
    public string? Title { get; set; }
    public string BoardData { get; set; } = string.Empty;  // Fabric.js JSON
    public string? BoardType { get; set; }
    public string? OriginalPrompt { get; set; }
}
