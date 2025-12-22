using Memorizer.Models;
using Memorizer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Memorizer.Controllers;

/// <summary>
/// UI Controller for Memory Architecture feature
/// </summary>
[Route("ui/architecture")]
public class ArchitectureViewController : Controller
{
    /// <summary>
    /// Memory Architecture main page
    /// </summary>
    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Shared Architecture view page
    /// </summary>
    [HttpGet]
    [Route("share/{shortCode}")]
    public IActionResult Share(string shortCode)
    {
        ViewBag.ShortCode = shortCode;
        return View("Share");
    }

    /// <summary>
    /// Shared Architecture list page
    /// </summary>
    [HttpGet]
    [Route("shares")]
    public IActionResult ShareList()
    {
        return View("ShareList");
    }
}

/// <summary>
/// API Controller for Memory Architecture feature
/// Combines two memories to generate creative architecture ideas
/// </summary>
[Route("api/architecture")]
[ApiController]
[AllowAnonymous]
public class ArchitectureController : ControllerBase
{
    private readonly IStorage _storage;
    private readonly ILlmService _llmService;
    private readonly ILogger<ArchitectureController> _logger;
    private readonly Npgsql.NpgsqlDataSource _dataSource;

    public ArchitectureController(
        IStorage storage,
        ILlmService llmService,
        ILogger<ArchitectureController> logger,
        Npgsql.NpgsqlDataSource dataSource)
    {
        _storage = storage;
        _llmService = llmService;
        _logger = logger;
        _dataSource = dataSource;
    }

    /// <summary>
    /// Get a random memory
    /// </summary>
    [HttpGet("random")]
    public async Task<ActionResult<Memory>> GetRandomMemory([FromQuery] Guid? excludeId = null)
    {
        try
        {
            var memory = await GetRandomMemoryFromDb(excludeId);
            if (memory == null)
            {
                return NotFound(new { error = "No memories found" });
            }
            return Ok(memory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random memory");
            return StatusCode(500, new { error = "Failed to get random memory" });
        }
    }

    /// <summary>
    /// Generate a creative idea from two memories (streaming)
    /// </summary>
    [HttpPost("generate-idea")]
    public async Task GenerateIdea([FromBody] GenerateIdeaRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            // Get both memories
            var memory1 = await _storage.Get(request.Memory1Id);
            var memory2 = await _storage.Get(request.Memory2Id);

            if (memory1 == null || memory2 == null)
            {
                await WriteSSEEvent("error", new { message = "One or both memories not found" });
                return;
            }

            // Generate prompt
            string prompt;
            if (!string.IsNullOrEmpty(request.PreviousIdea))
            {
                prompt = ArchitecturePrompts.GetIdeaRegenerationPrompt(
                    memory1.Title ?? "Memory A",
                    memory1.Text,
                    memory2.Title ?? "Memory B",
                    memory2.Text,
                    request.PreviousIdea
                );
            }
            else
            {
                prompt = ArchitecturePrompts.GetIdeaGenerationPrompt(
                    memory1.Title ?? "Memory A",
                    memory1.Text,
                    memory2.Title ?? "Memory B",
                    memory2.Text
                );
            }

            _logger.LogInformation("Generating idea from memories {Memory1Id} and {Memory2Id}",
                request.Memory1Id, request.Memory2Id);

            // Stream LLM response directly
            await foreach (var chunk in _llmService.CompleteStreamingAsync(prompt, HttpContext.RequestAborted))
            {
                await WriteSSEEvent("chunk", new { content = chunk });
            }

            await WriteSSEEvent("done", new { success = true });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Idea generation cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating idea");
            await WriteSSEEvent("error", new { message = "Failed to generate idea" });
        }
    }

    /// <summary>
    /// Generate architecture from two memories and an idea (streaming)
    /// </summary>
    [HttpPost("generate-architecture")]
    public async Task GenerateArchitecture([FromBody] GenerateArchitectureRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            // Get both memories
            var memory1 = await _storage.Get(request.Memory1Id);
            var memory2 = await _storage.Get(request.Memory2Id);

            if (memory1 == null || memory2 == null)
            {
                await WriteSSEEvent("error", new { message = "One or both memories not found" });
                return;
            }

            // Validate language
            var language = request.Language ?? "C#";
            if (!ArchitecturePrompts.SupportedLanguages.Contains(language))
            {
                language = "C#";
            }

            // Generate prompt
            var prompt = ArchitecturePrompts.GetArchitectureGenerationPrompt(
                memory1.Title ?? "Memory A",
                memory1.Text,
                memory2.Title ?? "Memory B",
                memory2.Text,
                request.IdeaPrompt,
                language
            );

            _logger.LogInformation("Generating architecture from memories {Memory1Id} and {Memory2Id} in {Language}",
                request.Memory1Id, request.Memory2Id, language);

            // Stream LLM response directly
            await foreach (var chunk in _llmService.CompleteStreamingAsync(prompt, HttpContext.RequestAborted))
            {
                await WriteSSEEvent("chunk", new { content = chunk });
            }

            await WriteSSEEvent("done", new { success = true });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Architecture generation cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating architecture");
            await WriteSSEEvent("error", new { message = "Failed to generate architecture" });
        }
    }

    /// <summary>
    /// Get supported programming languages
    /// </summary>
    [HttpGet("languages")]
    public ActionResult<string[]> GetSupportedLanguages()
    {
        return Ok(ArchitecturePrompts.SupportedLanguages);
    }

    /// <summary>
    /// Share generated architecture
    /// </summary>
    [HttpPost("share")]
    public async Task<ActionResult> ShareArchitecture([FromBody] ShareArchitectureRequest request)
    {
        try
        {
            // Generate short code
            var shortCode = GenerateShortCode();

            // Save to dedicated architecture_share_links table
            await using var conn = await _dataSource.OpenConnectionAsync();

            var insertQuery = @"
                INSERT INTO architecture_share_links
                    (short_code, memory1_id, memory2_id, memory1_title, memory2_title,
                     idea_prompt, architecture, language, created_at)
                VALUES
                    (@shortCode, @memory1Id, @memory2Id, @memory1Title, @memory2Title,
                     @ideaPrompt, @architecture, @language, @createdAt)";

            await using var cmd = new Npgsql.NpgsqlCommand(insertQuery, conn);
            cmd.Parameters.AddWithValue("shortCode", shortCode);
            cmd.Parameters.AddWithValue("memory1Id", request.Memory1Id);
            cmd.Parameters.AddWithValue("memory2Id", request.Memory2Id);
            cmd.Parameters.AddWithValue("memory1Title", request.Memory1Title ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("memory2Title", request.Memory2Title ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("ideaPrompt", request.IdeaPrompt);
            cmd.Parameters.AddWithValue("architecture", request.Architecture);
            cmd.Parameters.AddWithValue("language", request.Language ?? "C#");
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Created architecture share link {ShortCode}", shortCode);

            return Ok(new { shortCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing architecture");
            return StatusCode(500, new { error = "Failed to create share link" });
        }
    }

    /// <summary>
    /// Get shared architecture by short code
    /// </summary>
    [HttpGet("share/{shortCode}")]
    public async Task<ActionResult> GetSharedArchitecture(string shortCode)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            var query = @"
                SELECT memory1_id, memory2_id, memory1_title, memory2_title,
                       idea_prompt, architecture, language, created_at
                FROM architecture_share_links
                WHERE short_code = @shortCode
                LIMIT 1";

            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("shortCode", shortCode);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var content = new
                {
                    memory1Id = reader.GetGuid(0),
                    memory2Id = reader.GetGuid(1),
                    memory1Title = reader.IsDBNull(2) ? null : reader.GetString(2),
                    memory2Title = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ideaPrompt = reader.GetString(4),
                    architecture = reader.GetString(5),
                    language = reader.GetString(6)
                };
                var createdAt = reader.GetDateTime(7);

                return Ok(new { content, createdAt, shortCode });
            }

            return NotFound(new { error = "Architecture share not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared architecture");
            return StatusCode(500, new { error = "Failed to get shared architecture" });
        }
    }

    /// <summary>
    /// Get list of shared architectures with pagination (for infinite scroll)
    /// </summary>
    [HttpGet("shares")]
    public async Task<ActionResult> GetSharedArchitectures(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            // Get total count
            var countQuery = "SELECT COUNT(*) FROM architecture_share_links";
            await using var countCmd = new Npgsql.NpgsqlCommand(countQuery, conn);
            var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            // Get paginated results
            var offset = (page - 1) * pageSize;
            var query = @"
                SELECT short_code, memory1_title, memory2_title,
                       idea_prompt, language, created_at
                FROM architecture_share_links
                ORDER BY created_at DESC
                LIMIT @pageSize OFFSET @offset";

            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("pageSize", pageSize);
            cmd.Parameters.AddWithValue("offset", offset);

            var items = new List<object>();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var ideaPrompt = reader.GetString(3);
                // Extract title from idea prompt (first ### heading)
                var title = ExtractTitleFromMarkdown(ideaPrompt);
                // Extract summary (first paragraph after title)
                var summary = ExtractSummaryFromMarkdown(ideaPrompt);

                items.Add(new
                {
                    shortCode = reader.GetString(0),
                    memory1Title = reader.IsDBNull(1) ? null : reader.GetString(1),
                    memory2Title = reader.IsDBNull(2) ? null : reader.GetString(2),
                    title = title,
                    summary = summary,
                    language = reader.GetString(4),
                    createdAt = reader.GetDateTime(5)
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
            _logger.LogError(ex, "Error getting shared architectures list");
            return StatusCode(500, new { error = "Failed to get shared architectures" });
        }
    }

    /// <summary>
    /// Extract title from markdown (first ### heading)
    /// </summary>
    private static string ExtractTitleFromMarkdown(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return "Untitled";

        var lines = markdown.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("### "))
            {
                return trimmed[4..].Trim();
            }
            if (trimmed.StartsWith("## "))
            {
                return trimmed[3..].Trim();
            }
            if (trimmed.StartsWith("# "))
            {
                return trimmed[2..].Trim();
            }
        }

        // Fallback: first non-empty line
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                return trimmed.Length > 50 ? trimmed[..50] + "..." : trimmed;
            }
        }

        return "Untitled";
    }

    /// <summary>
    /// Extract summary from markdown (content after title, before next heading)
    /// </summary>
    private static string ExtractSummaryFromMarkdown(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return "";

        var lines = markdown.Split('\n');
        var foundTitle = false;
        var summaryLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip until we find the title
            if (!foundTitle)
            {
                if (trimmed.StartsWith("#"))
                {
                    foundTitle = true;
                }
                continue;
            }

            // Stop at next heading
            if (trimmed.StartsWith("#"))
            {
                break;
            }

            // Collect non-empty lines
            if (!string.IsNullOrEmpty(trimmed))
            {
                summaryLines.Add(trimmed);
                if (summaryLines.Count >= 3) break;
            }
        }

        var summary = string.Join(" ", summaryLines);
        if (summary.Length > 200)
        {
            summary = summary[..200] + "...";
        }

        return summary;
    }

    private async Task<Memory?> GetRandomMemoryFromDb(Guid? excludeId)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();

        string query;
        if (excludeId.HasValue)
        {
            query = @"
                SELECT id, type, text, source, title, tags, confidence, created_at
                FROM memories
                WHERE id != @excludeId
                ORDER BY RANDOM()
                LIMIT 1";
        }
        else
        {
            query = @"
                SELECT id, type, text, source, title, tags, confidence, created_at
                FROM memories
                ORDER BY RANDOM()
                LIMIT 1";
        }

        await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
        if (excludeId.HasValue)
        {
            cmd.Parameters.AddWithValue("excludeId", excludeId.Value);
        }

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Memory
            {
                Id = reader.GetGuid(0),
                Type = reader.GetString(1),
                Text = reader.GetString(2),
                Source = reader.GetString(3),
                Title = reader.IsDBNull(4) ? null : reader.GetString(4),
                Tags = reader.IsDBNull(5) ? Array.Empty<string>() : (string[])reader.GetValue(5),
                Confidence = reader.GetDouble(6),
                CreatedAt = reader.GetDateTime(7)
            };
        }

        return null;
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
        var shortCode = new char[6];  // Match askbot_share_links.short_code VARCHAR(6)

        for (int i = 0; i < 6; i++)
        {
            shortCode[i] = chars[random.Next(chars.Length)];
        }

        return new string(shortCode);
    }
}

/// <summary>
/// Request for generating idea
/// </summary>
public class GenerateIdeaRequest
{
    public Guid Memory1Id { get; set; }
    public Guid Memory2Id { get; set; }
    public string? PreviousIdea { get; set; }
}

/// <summary>
/// Request for generating architecture
/// </summary>
public class GenerateArchitectureRequest
{
    public Guid Memory1Id { get; set; }
    public Guid Memory2Id { get; set; }
    public string IdeaPrompt { get; set; } = string.Empty;
    public string? Language { get; set; }
}

/// <summary>
/// Request for sharing architecture
/// </summary>
public class ShareArchitectureRequest
{
    public Guid Memory1Id { get; set; }
    public Guid Memory2Id { get; set; }
    public string? Memory1Title { get; set; }
    public string? Memory2Title { get; set; }
    public string IdeaPrompt { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string? Language { get; set; }
}
