using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Memorizer.Actors;
using Memorizer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Memorizer.Controllers;

[Route("api/askbot")]
[ApiController]
[AllowAnonymous]
public class AskBotController : ControllerBase
{
    private readonly ActorSystem _actorSystem;
    private readonly IActorRef _searchMemoryActor;
    private readonly IActorRef _decisionActor;
    private readonly ILlmService _llmService;
    private readonly ILogger<AskBotController> _logger;

    // Static dictionary to maintain ChatBotActors per session
    private static readonly ConcurrentDictionary<string, IActorRef> SessionActors = new();

    // SSE connections per session - key is sessionId, value is dictionary of connectionId -> channel
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Channel<StreamingUpdate>>> SessionChannels = new();

    public AskBotController(
        ActorSystem actorSystem,
        IRequiredActor<SearchMemoryActorKey> searchMemoryActor,
        IRequiredActor<DecisionActorKey> decisionActor,
        ILlmService llmService,
        ILogger<AskBotController> logger)
    {
        _actorSystem = actorSystem;
        _searchMemoryActor = searchMemoryActor.ActorRef;
        _decisionActor = decisionActor.ActorRef;
        _llmService = llmService;
        _logger = logger;
    }

    /// <summary>
    /// SSE endpoint for streaming chat responses
    /// </summary>
    [HttpGet("stream")]
    public async Task StreamChat([FromQuery] string? sessionId = null)
    {
        // Get or create session ID
        sessionId = GetOrCreateSessionId(sessionId);

        // Create a unique connection ID for this SSE connection
        var connectionId = Guid.NewGuid().ToString();

        _logger.LogInformation("Starting SSE stream for session {SessionId} with connection {ConnectionId}", sessionId, connectionId);

        // Set SSE headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        // Create channel for this connection
        var channel = Channel.CreateUnbounded<StreamingUpdate>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Register channel for this session with connection ID
        var sessionConnections = SessionChannels.GetOrAdd(sessionId, _ => new ConcurrentDictionary<string, Channel<StreamingUpdate>>());
        sessionConnections[connectionId] = channel;

        _logger.LogInformation("SSE channel registered for session {SessionId}, connection {ConnectionId}. Total connections for session: {Count}",
            sessionId, connectionId, sessionConnections.Count);

        try
        {
            // Send initial connection event with connection ID
            await WriteSSEEvent("connected", new { sessionId, connectionId, timestamp = DateTime.UtcNow });

            // Start reading from channel and writing to response
            await foreach (var update in channel.Reader.ReadAllAsync(HttpContext.RequestAborted))
            {
                // Special handling for FinalResponse to send as 'finalresponse' event
                if (update.UpdateType == StreamUpdateType.FinalResponse)
                {
                    // Parse the JSON content back to send it properly
                    var finalData = JsonSerializer.Deserialize<JsonElement>(update.Content);
                    await WriteSSEEvent("finalresponse", finalData);
                }
                else
                {
                    await WriteSSEEvent(update.UpdateType.ToString().ToLower(), new
                    {
                        sessionId = update.SessionId,
                        content = update.Content,
                        timestamp = update.Timestamp,
                        type = update.UpdateType.ToString()
                    });
                }

                await Response.Body.FlushAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE connection closed for session {SessionId}, connection {ConnectionId}", sessionId, connectionId);
        }
        finally
        {
            // Remove this specific connection from session
            if (SessionChannels.TryGetValue(sessionId, out var connections))
            {
                if (connections.TryRemove(connectionId, out _))
                {
                    _logger.LogInformation("Removed connection {ConnectionId} from session {SessionId}. Remaining connections: {Count}",
                        connectionId, sessionId, connections.Count);
                }

                // If no more connections for this session, remove the session entry
                if (connections.IsEmpty)
                {
                    SessionChannels.TryRemove(sessionId, out _);
                    _logger.LogInformation("No more connections for session {SessionId}, removed session from channels", sessionId);
                }
            }

            channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Send a message to the chatbot
    /// </summary>
    [HttpPost("message")]
    [AllowAnonymous]
    public async Task<IActionResult> SendMessage([FromBody] AskBotRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            // Get or create session ID
            var sessionId = GetOrCreateSessionId(request.SessionId);

            _logger.LogInformation("Processing askbot request for session {SessionId}", sessionId);

            // Get or create ChatBotActor for this session
            var chatBotActor = GetOrCreateChatBotActor(sessionId);

            // Create user chat request
            var userRequest = new UserChatRequest
            {
                SessionId = sessionId,
                Message = request.Message,
                UserId = "anonymous"
            };

            // Send initial processing update via SSE
            await BroadcastToSession(sessionId, new StreamingUpdate
            {
                SessionId = sessionId,
                UpdateType = StreamUpdateType.SearchProgress,
                Content = "Processing your request..."
            });

            // Send request to ChatBotActor using Tell (fire and forget)
            // The response will come through SSE via StreamingChatBotActor
            chatBotActor.Tell(userRequest);

            // Note: The response will be streamed through SSE events
            // StreamingChatBotActor handles all the messaging through the SSE bridge

            return Accepted(new { sessionId, status = "processing" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing askbot request");
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

        // Clear old session if exists in cookie
        Response.Cookies.Delete("askbot-session-id");

        // Set new session cookie
        Response.Cookies.Append("askbot-session-id", newSessionId, new CookieOptions
        {
            HttpOnly = false, // Allow JavaScript access
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(90) // Permanent storage
        });

        _logger.LogInformation("Created new askbot session {SessionId}", newSessionId);

        return Ok(new { sessionId = newSessionId });
    }

    /// <summary>
    /// Get session info
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public IActionResult GetSessionInfo(string sessionId)
    {
        var exists = SessionActors.ContainsKey(sessionId);
        var hasActiveConnections = SessionChannels.ContainsKey(sessionId);
        var connectionCount = 0;

        if (SessionChannels.TryGetValue(sessionId, out var sessionConnections))
        {
            connectionCount = sessionConnections.Count;
        }

        return Ok(new
        {
            sessionId,
            exists,
            hasActiveConnections,
            connectionCount,
            timestamp = DateTime.UtcNow
        });
    }

    private async Task StreamResponseMessage(string sessionId, string message, ResponseType responseType, List<Guid>? referencedMemoryIds = null)
    {
        // Split message into chunks for streaming effect
        var chunks = SplitIntoChunks(message, 5); // 5 characters at a time

        foreach (var chunk in chunks)
        {
            await BroadcastToSession(sessionId, new StreamingUpdate
            {
                SessionId = sessionId,
                UpdateType = StreamUpdateType.PartialResponse,
                Content = chunk
            });

            // Small delay for typing animation effect
            await Task.Delay(20);
        }

        // Send final response marker with memory references
        var finalData = new
        {
            sessionId = sessionId,
            type = responseType.ToString(),
            referencedMemoryIds = referencedMemoryIds ?? new List<Guid>()
        };

        await WriteSSEEventToChannels(sessionId, "finalresponse", finalData);
    }

    private async Task WriteSSEEventToChannels(string sessionId, string eventType, object data)
    {
        // For final response with memory IDs, we need to send proper SSE event format
        // This sends the data as a finalresponse event type in SSE format
        await BroadcastToSession(sessionId, new StreamingUpdate
        {
            SessionId = sessionId,
            UpdateType = StreamUpdateType.FinalResponse,
            Content = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        });
    }

    private List<string> SplitIntoChunks(string text, int chunkSize)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        }
        return chunks;
    }

    private async Task BroadcastToSession(string sessionId, StreamingUpdate update)
    {
        if (SessionChannels.TryGetValue(sessionId, out var sessionConnections))
        {
            var connectionCount = sessionConnections.Count;
            if (connectionCount > 0)
            {
                _logger.LogDebug("Broadcasting to session {SessionId} with {ConnectionCount} connections: {UpdateType} - {Content}",
                    sessionId, connectionCount, update.UpdateType, update.Content);

                var tasks = sessionConnections.Values.Select(channel =>
                    channel.Writer.TryWrite(update) ? Task.CompletedTask : Task.CompletedTask
                );
                await Task.WhenAll(tasks);
            }
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

    private string GetOrCreateSessionId(string? providedSessionId)
    {
        // Use provided session ID if valid
        if (!string.IsNullOrWhiteSpace(providedSessionId))
        {
            return providedSessionId;
        }

        // Try to get from cookie
        if (Request.Cookies.TryGetValue("askbot-session-id", out var sessionCookie))
        {
            if (!string.IsNullOrWhiteSpace(sessionCookie))
            {
                return sessionCookie;
            }
        }

        // Generate new session ID
        var newSessionId = Guid.NewGuid().ToString();

        // Set cookie for future requests
        Response.Cookies.Append("askbot-session-id", newSessionId, new CookieOptions
        {
            HttpOnly = false, // Allow JavaScript access
            Secure = true,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(90) // Permanent storage
        });

        return newSessionId;
    }

    private IActorRef GetOrCreateChatBotActor(string sessionId)
    {
        return SessionActors.GetOrAdd(sessionId, sid =>
        {
            _logger.LogInformation("Creating new ChatBotActor for askbot session {SessionId}", sid);

            // Create SSE bridge actor to forward streaming updates
            var sseBridgeProps = SSEBridgeActor.Props(sid, async update =>
            {
                await BroadcastToSession(sid, update);
            });
            var sseBridgeActor = _actorSystem.ActorOf(sseBridgeProps, $"sse-bridge-{sid}");

            // Create ChatBotActor with SSE bridge
            var props = StreamingChatBotActor.Props(sid, _searchMemoryActor, _decisionActor, _llmService, sseBridgeActor);
            var actorName = $"askbot-{sid}";
            var actor = _actorSystem.ActorOf(props, actorName);

            return actor;
        });
    }
}

/// <summary>
/// Request model for AskBot messages
/// </summary>
public class AskBotRequest
{
    /// <summary>
    /// The message to send to the chatbot
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Optional session ID (will be created if not provided)
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// Bridge actor to forward streaming updates to SSE
/// </summary>
public sealed class SSEBridgeActor : ReceiveActor
{
    private readonly string _sessionId;
    private readonly Func<StreamingUpdate, Task> _forwardUpdate;
    private readonly ILoggingAdapter _logger;

    public SSEBridgeActor(string sessionId, Func<StreamingUpdate, Task> forwardUpdate)
    {
        _sessionId = sessionId;
        _forwardUpdate = forwardUpdate;
        _logger = Context.GetLogger();

        ReceiveAsync<StreamingUpdate>(HandleStreamingUpdate);
    }

    private async Task HandleStreamingUpdate(StreamingUpdate update)
    {
        try
        {
            await _forwardUpdate(update);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error forwarding streaming update for session {0}", _sessionId);
        }
    }

    public static Props Props(string sessionId, Func<StreamingUpdate, Task> forwardUpdate)
    {
        return Akka.Actor.Props.Create(() => new SSEBridgeActor(sessionId, forwardUpdate));
    }
}

/// <summary>
/// Extended ChatBotActor that supports streaming updates
/// </summary>
public sealed class StreamingChatBotActor : ChatBotActor
{
    private readonly IActorRef _sseBridge;
    private readonly string _sessionId;
    private readonly IActorRef _askBotController;

    public StreamingChatBotActor(
        string sessionId,
        IActorRef searchMemoryActor,
        IActorRef decisionActor,
        ILlmService llmService,
        IActorRef sseBridge)
        : base(sessionId, searchMemoryActor, decisionActor, llmService)
    {
        _sessionId = sessionId;
        _sseBridge = sseBridge;
        _askBotController = Context.Parent; // Store reference to parent for response notification

        // Re-register the ChatBotResponse handler to use the overridden method
        // This is necessary because the base constructor already registered it
        Receive<ChatBotResponse>(HandleChatBotResponseFromPipeTo);

        Context.GetLogger().Info("StreamingChatBotActor initialized for session {0}", sessionId);
    }

    public IActorRef GetSseBridge() => _sseBridge;

    // Override the base class method to handle ChatBotResponse differently
    protected override void HandleChatBotResponseFromPipeTo(ChatBotResponse response)
    {
        Context.GetLogger().Info("[StreamingChatBotActor] Override HandleChatBotResponseFromPipeTo called for session {0}, ResponseType: {1}, MemoryCount: {2}",
            _sessionId,
            response.Type,
            response.ReferencedMemoryIds?.Count ?? 0);

        Context.GetLogger().Info("Successfully generated response for session {0} using {1} memory/memories.",
            _sessionId,
            response.ReferencedMemoryIds?.Count ?? 0);

        // Stream the response message content character by character first
        StreamResponseMessageAsync(response);

        Context.GetLogger().Info("Streaming final response through SSE for session {0}", _sessionId);
    }

    private async void StreamResponseMessageAsync(ChatBotResponse response)
    {
        try
        {
            // Split message into chunks for streaming effect
            var chunks = SplitIntoChunks(response.Message, 5); // 5 characters at a time

            foreach (var chunk in chunks)
            {
                _sseBridge.Tell(new StreamingUpdate
                {
                    SessionId = _sessionId,
                    UpdateType = StreamUpdateType.PartialResponse,
                    Content = chunk
                });

                await Task.Delay(10); // Small delay for typing effect
            }

            // After streaming content, send the final metadata
            _sseBridge.Tell(new StreamingUpdate
            {
                SessionId = _sessionId,
                UpdateType = StreamUpdateType.FinalResponse,
                Content = System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionId = _sessionId,
                    type = response.Type.ToString(),
                    referencedMemoryIds = response.ReferencedMemoryIds ?? new List<Guid>()
                })
            });
        }
        catch (Exception ex)
        {
            Context.GetLogger().Error(ex, "Error streaming response for session {0}", _sessionId);
        }
    }

    private List<string> SplitIntoChunks(string text, int chunkSize)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        }
        return chunks;
    }


    protected override void AddReasoningStep(string step)
    {
        base.AddReasoningStep(step);

        // Forward to SSE bridge
        _sseBridge.Tell(new StreamingUpdate
        {
            SessionId = _sessionId,
            UpdateType = StreamUpdateType.Reasoning,
            Content = step
        });
    }

    public static Props Props(
        string sessionId,
        IActorRef searchMemoryActor,
        IActorRef decisionActor,
        ILlmService llmService,
        IActorRef sseBridge)
    {
        return Akka.Actor.Props.Create(() =>
            new StreamingChatBotActor(sessionId, searchMemoryActor, decisionActor, llmService, sseBridge));
    }
}