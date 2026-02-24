using Akka.Actor;
using Memorizer.Actors;
using Memorizer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Memorizer.Controllers;

/// <summary>
/// UI Controller for ClaudeCode feature
/// </summary>
[Route("ui/claudecode")]
public class ClaudeCodeViewController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("skill")]
    public IActionResult SkillCreate()
    {
        return View("SkillCreate");
    }

    [HttpGet("share/{shortCode}")]
    public IActionResult Share(string shortCode)
    {
        ViewBag.ShortCode = shortCode;
        return View("Share");
    }

    [HttpGet("shares")]
    public IActionResult SkillShared()
    {
        return View("SkillShared");
    }
}

/// <summary>
/// API Controller for ClaudeCode Skill Maker
/// </summary>
[Route("api/claudecode")]
[ApiController]
[AllowAnonymous]
public class ClaudeCodeController : ControllerBase
{
    private readonly ActorSystem _actorSystem;
    private readonly ILlmExService _llmExService;
    private readonly ILogger<ClaudeCodeController> _logger;
    private readonly Npgsql.NpgsqlDataSource _dataSource;

    // Static dictionaries for session management
    private static readonly ConcurrentDictionary<string, IActorRef> SessionActors = new();
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Channel<SkillMakerStreamingUpdate>>> SessionChannels = new();

    public ClaudeCodeController(
        ActorSystem actorSystem,
        ILlmExService llmExService,
        ILogger<ClaudeCodeController> logger,
        Npgsql.NpgsqlDataSource dataSource)
    {
        _actorSystem = actorSystem;
        _llmExService = llmExService;
        _logger = logger;
        _dataSource = dataSource;
    }

    /// <summary>
    /// SSE endpoint for streaming skill creation updates
    /// </summary>
    [HttpGet("stream")]
    public async Task StreamSkillCreation([FromQuery] string? sessionId = null)
    {
        sessionId = GetOrCreateSessionId(sessionId);
        var connectionId = Guid.NewGuid().ToString();

        _logger.LogInformation("Starting SSE stream for ClaudeCode session {SessionId}", sessionId);

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var channel = Channel.CreateUnbounded<SkillMakerStreamingUpdate>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        var sessionConnections = SessionChannels.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, Channel<SkillMakerStreamingUpdate>>());
        sessionConnections[connectionId] = channel;

        try
        {
            await WriteSSEEvent("connected", new { sessionId, connectionId, timestamp = DateTime.UtcNow });

            // Send welcome message with category options
            var welcomeData = new
            {
                sessionId,
                content = SkillMakerPrompts.GetWelcomeMessage(),
                options = SkillMakerPrompts.GetCategoryOptions(),
                updateType = "options",
                nextMessageType = "category"
            };
            await WriteSSEEvent("options", welcomeData);

            await foreach (var update in channel.Reader.ReadAllAsync(HttpContext.RequestAborted))
            {
                var eventType = update.UpdateType.ToString().ToLower();
                var data = new
                {
                    sessionId = update.SessionId,
                    content = update.Content,
                    options = update.Options,
                    isComplete = update.IsComplete,
                    skillContent = update.SkillContent,
                    usageGuide = update.UsageGuide,
                    insight = update.Insight,
                    example = update.Example,
                    updateType = eventType,
                    nextMessageType = update.NextMessageType,
                    timestamp = update.Timestamp
                };

                await WriteSSEEvent(eventType, data);
                await Response.Body.FlushAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE connection closed for ClaudeCode session {SessionId}", sessionId);
        }
        finally
        {
            if (SessionChannels.TryGetValue(sessionId, out var connections))
            {
                connections.TryRemove(connectionId, out _);
                if (connections.IsEmpty)
                {
                    SessionChannels.TryRemove(sessionId, out _);
                }
            }
            channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Send a message to the skill maker
    /// </summary>
    [HttpPost("message")]
    [AllowAnonymous]
    public IActionResult SendMessage([FromBody] ClaudeCodeMessageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            var sessionId = GetOrCreateSessionId(request.SessionId);

            var actor = GetOrCreateSkillMakerActor(sessionId);

            var messageType = request.MessageType?.ToLower() switch
            {
                "category" => SkillMakerMessageType.Category,
                "subskill" => SkillMakerMessageType.SubSkill,
                "answer" => SkillMakerMessageType.Answer,
                "custominput" => SkillMakerMessageType.CustomInput,
                _ => SkillMakerMessageType.Answer
            };

            actor.Tell(new SkillMakerUserMessage
            {
                SessionId = sessionId,
                Message = request.Message,
                MessageType = messageType
            });

            return Accepted(new { sessionId, status = "processing" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ClaudeCode message");
            return StatusCode(500, new { error = "An error occurred processing your request" });
        }
    }

    /// <summary>
    /// Create a new session
    /// </summary>
    [HttpPost("session/new")]
    [AllowAnonymous]
    public IActionResult CreateNewSession()
    {
        var newSessionId = Guid.NewGuid().ToString();

        Response.Cookies.Delete("claudecode-session-id");
        Response.Cookies.Append("claudecode-session-id", newSessionId, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(90)
        });

        return Ok(new { sessionId = newSessionId });
    }

    /// <summary>
    /// Share a generated skill
    /// </summary>
    [HttpPost("share")]
    [AllowAnonymous]
    public async Task<IActionResult> ShareSkill([FromBody] ClaudeCodeShareRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SkillContent))
            {
                return BadRequest(new { error = "Skill content is required" });
            }

            var shortCode = GenerateShortCode();

            await using var conn = await _dataSource.OpenConnectionAsync();

            var insertQuery = @"
                INSERT INTO claudecode_skill_share_links
                    (short_code, title, skill_content, job_category, skill_name, conversation_data, created_at)
                VALUES
                    (@shortCode, @title, @skillContent, @jobCategory, @skillName, @conversationData::jsonb, @createdAt)";

            await using var cmd = new Npgsql.NpgsqlCommand(insertQuery, conn);
            cmd.Parameters.AddWithValue("shortCode", shortCode);
            cmd.Parameters.AddWithValue("title", request.Title ?? "Claude Skill");
            cmd.Parameters.AddWithValue("skillContent", request.SkillContent);
            cmd.Parameters.AddWithValue("jobCategory", request.JobCategory ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("skillName", request.SkillName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("conversationData",
                request.ConversationData != null ? (object)request.ConversationData : DBNull.Value);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Created ClaudeCode skill share link {ShortCode}", shortCode);

            return Ok(new { shortCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing ClaudeCode skill");
            return StatusCode(500, new { error = "Failed to create share link" });
        }
    }

    /// <summary>
    /// Get shared skill by short code
    /// </summary>
    [HttpGet("share/{shortCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSharedSkill(string shortCode)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            var query = @"
                SELECT title, skill_content, job_category, skill_name, conversation_data, created_at
                FROM claudecode_skill_share_links
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
                    skillContent = reader.GetString(1),
                    jobCategory = reader.IsDBNull(2) ? null : reader.GetString(2),
                    skillName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    conversationData = reader.IsDBNull(4) ? null : reader.GetString(4),
                    createdAt = reader.GetDateTime(5)
                });
            }

            return NotFound(new { error = "Skill not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared skill");
            return StatusCode(500, new { error = "Failed to get shared skill" });
        }
    }

    /// <summary>
    /// Get list of shared skills with pagination
    /// </summary>
    [HttpGet("shares")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSharedSkills(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 12;

            await using var conn = await _dataSource.OpenConnectionAsync();

            var countQuery = "SELECT COUNT(*) FROM claudecode_skill_share_links";
            await using var countCmd = new Npgsql.NpgsqlCommand(countQuery, conn);
            var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            var offset = (page - 1) * pageSize;
            var query = @"
                SELECT short_code, title, job_category, skill_name, created_at
                FROM claudecode_skill_share_links
                ORDER BY created_at DESC
                LIMIT @pageSize OFFSET @offset";

            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("pageSize", pageSize);
            cmd.Parameters.AddWithValue("offset", offset);

            var items = new List<object>();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                items.Add(new
                {
                    shortCode = reader.GetString(0),
                    title = reader.GetString(1),
                    jobCategory = reader.IsDBNull(2) ? null : reader.GetString(2),
                    skillName = reader.IsDBNull(3) ? null : reader.GetString(3),
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
            _logger.LogError(ex, "Error getting shared skills list");
            return StatusCode(500, new { error = "Failed to get shared skills" });
        }
    }

    private IActorRef GetOrCreateSkillMakerActor(string sessionId)
    {
        return SessionActors.GetOrAdd(sessionId, sid =>
        {
            _logger.LogInformation("Creating new SkillMakerActor for session {SessionId}", sid);

            var sseBridgeProps = SkillMakerSSEBridgeActor.Props(sid, async update =>
            {
                await BroadcastToSession(sid, update);
            });
            var sseBridgeActor = _actorSystem.ActorOf(sseBridgeProps, $"skill-sse-bridge-{sid}");

            var props = SkillMakerActor.Props(sid, _llmExService, sseBridgeActor);
            var actor = _actorSystem.ActorOf(props, $"skill-maker-{sid}");

            return actor;
        });
    }

    private Task BroadcastToSession(string sessionId, SkillMakerStreamingUpdate update)
    {
        if (SessionChannels.TryGetValue(sessionId, out var sessionConnections))
        {
            foreach (var channel in sessionConnections.Values)
            {
                channel.Writer.TryWrite(update);
            }
        }
        return Task.CompletedTask;
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

    private string GetOrCreateSessionId(string? providedSessionId)
    {
        if (!string.IsNullOrWhiteSpace(providedSessionId))
        {
            return providedSessionId;
        }

        if (Request.Cookies.TryGetValue("claudecode-session-id", out var sessionCookie))
        {
            if (!string.IsNullOrWhiteSpace(sessionCookie))
            {
                return sessionCookie;
            }
        }

        var newSessionId = Guid.NewGuid().ToString();

        Response.Cookies.Append("claudecode-session-id", newSessionId, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(90)
        });

        return newSessionId;
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
/// Request model for ClaudeCode messages
/// </summary>
public class ClaudeCodeMessageRequest
{
    public string? SessionId { get; set; }
    public required string Message { get; set; }
    public string? MessageType { get; set; }
}

/// <summary>
/// Request model for sharing a skill
/// </summary>
public class ClaudeCodeShareRequest
{
    public string? Title { get; set; }
    public required string SkillContent { get; set; }
    public string? JobCategory { get; set; }
    public string? SkillName { get; set; }
    public string? ConversationData { get; set; }
}
