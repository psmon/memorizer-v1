using Akka.Actor;
using Akka.Hosting;
using Memorizer.Actors;
using Memorizer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Memorizer.Controllers;

[Route("api/chat")]
[ApiController]
public class ChatBotController : ControllerBase
{
    private readonly ActorSystem _actorSystem;
    private readonly IActorRef _searchMemoryActor;
    private readonly IActorRef _decisionActor;
    private readonly ILlmService _llmService;
    private readonly ILogger<ChatBotController> _logger;

    // Static dictionary to maintain ChatBotActors per session
    private static readonly ConcurrentDictionary<string, IActorRef> SessionActors = new();

    public ChatBotController(
        ActorSystem actorSystem,
        IRequiredActor<SearchMemoryActorKey> searchMemoryActor,
        IRequiredActor<DecisionActorKey> decisionActor,
        ILlmService llmService,
        ILogger<ChatBotController> logger)
    {
        _actorSystem = actorSystem;
        _searchMemoryActor = searchMemoryActor.ActorRef;
        _decisionActor = decisionActor.ActorRef;
        _llmService = llmService;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message to the bot
    /// </summary>
    /// <param name="request">Chat request containing the message</param>
    /// <returns>Chat response from the bot</returns>
    [HttpPost("message")]
    [Authorize]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            // Generate or retrieve session ID
            var sessionId = GetOrCreateSessionId();
            var userId = User.Identity?.Name ?? "Anonymous";

            _logger.LogInformation("Processing chat request for session {SessionId} from user {UserId}",
                sessionId, userId);

            // Get or create ChatBotActor for this session
            var chatBotActor = GetOrCreateChatBotActor(sessionId);

            // Create user chat request
            var userRequest = new UserChatRequest
            {
                SessionId = sessionId,
                Message = request.Message,
                UserId = userId
            };

            // Send request to ChatBotActor and wait for response
            var response = await chatBotActor.Ask<ChatBotResponse>(userRequest, TimeSpan.FromSeconds(30));

            // Convert to API response format
            var apiResponse = new ChatApiResponse
            {
                SessionId = response.SessionId,
                Message = response.Message,
                ResponseType = response.Type.ToString(),
                ReferencedMemoryIds = response.ReferencedMemoryIds,
                ReasoningSteps = response.ReasoningSteps,
                Timestamp = response.Timestamp
            };

            return Ok(apiResponse);
        }
        catch (TimeoutException)
        {
            _logger.LogError("Chat request timed out");
            return StatusCode(504, new { error = "Request timed out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new { error = "An error occurred processing your request" });
        }
    }

    /// <summary>
    /// Get current active sessions (admin only)
    /// </summary>
    [HttpGet("sessions")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetActiveSessions()
    {
        var sessions = SessionActors.Keys.ToList();
        return Ok(new
        {
            totalSessions = sessions.Count,
            sessionIds = sessions
        });
    }

    /// <summary>
    /// Clear a specific session (admin only)
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    [Authorize(Roles = "Admin")]
    public IActionResult ClearSession(string sessionId)
    {
        if (SessionActors.TryRemove(sessionId, out var actor))
        {
            _actorSystem.Stop(actor);
            _logger.LogInformation("Cleared session {SessionId}", sessionId);
            return Ok(new { message = $"Session {sessionId} cleared" });
        }

        return NotFound(new { error = $"Session {sessionId} not found" });
    }

    /// <summary>
    /// Clear all sessions (admin only)
    /// </summary>
    [HttpDelete("sessions")]
    [Authorize(Roles = "Admin")]
    public IActionResult ClearAllSessions()
    {
        var sessions = SessionActors.Keys.ToList();
        foreach (var sessionId in sessions)
        {
            if (SessionActors.TryRemove(sessionId, out var actor))
            {
                _actorSystem.Stop(actor);
            }
        }

        _logger.LogInformation("Cleared {Count} sessions", sessions.Count);
        return Ok(new { message = $"Cleared {sessions.Count} sessions" });
    }

    /// <summary>
    /// Health check endpoint for the chat service
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            var llmHealth = await _llmService.CheckHealthAsync();
            return Ok(new
            {
                status = "healthy",
                activeSessions = SessionActors.Count,
                llmStatus = llmHealth.IsHealthy ? "healthy" : "unhealthy",
                llmMessage = llmHealth.Message
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                status = "degraded",
                activeSessions = SessionActors.Count,
                error = ex.Message
            });
        }
    }

    private string GetOrCreateSessionId()
    {
        // Try to get session ID from header first
        if (Request.Headers.TryGetValue("X-Session-Id", out var sessionIdHeader))
        {
            var sessionId = sessionIdHeader.ToString();
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                return sessionId;
            }
        }

        // Try to get from cookie
        if (Request.Cookies.TryGetValue("chat-session-id", out var sessionCookie))
        {
            if (!string.IsNullOrWhiteSpace(sessionCookie))
            {
                return sessionCookie;
            }
        }

        // Generate new session ID
        var newSessionId = Guid.NewGuid().ToString();

        // Set cookie for future requests
        Response.Cookies.Append("chat-session-id", newSessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(3)
        });

        return newSessionId;
    }

    private IActorRef GetOrCreateChatBotActor(string sessionId)
    {
        return SessionActors.GetOrAdd(sessionId, sid =>
        {
            _logger.LogInformation("Creating new ChatBotActor for session {SessionId}", sid);

            var props = ChatBotActor.Props(sid, _searchMemoryActor, _decisionActor, _llmService);
            var actorName = $"chatbot-{sid}";
            var actor = _actorSystem.ActorOf(props, actorName);

            return actor;
        });
    }
}

/// <summary>
/// Request model for chat messages
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// The message to send to the chatbot
    /// </summary>
    public required string Message { get; set; }
}

/// <summary>
/// Response model for chat API
/// </summary>
public class ChatApiResponse
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// The chatbot's response message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Type of response (MemoryBased, General, Error)
    /// </summary>
    public required string ResponseType { get; set; }

    /// <summary>
    /// Referenced memory IDs if response is based on memories
    /// </summary>
    public List<Guid>? ReferencedMemoryIds { get; set; }

    /// <summary>
    /// Reasoning steps taken to generate the response
    /// </summary>
    public List<string>? ReasoningSteps { get; set; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; set; }
}