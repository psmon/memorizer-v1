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

    // SSE connections per session
    private static readonly ConcurrentDictionary<string, List<Channel<StreamingUpdate>>> SessionChannels = new();

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

        _logger.LogInformation("Starting SSE stream for session {SessionId}", sessionId);

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

        // Register channel for this session
        SessionChannels.AddOrUpdate(sessionId,
            new List<Channel<StreamingUpdate>> { channel },
            (key, list) =>
            {
                list.Add(channel);
                return list;
            });

        try
        {
            // Send initial connection event
            await WriteSSEEvent("connected", new { sessionId, timestamp = DateTime.UtcNow });

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
            _logger.LogInformation("SSE connection closed for session {SessionId}", sessionId);
        }
        finally
        {
            // Remove channel from session
            if (SessionChannels.TryGetValue(sessionId, out var channels))
            {
                channels.Remove(channel);
                if (channels.Count == 0)
                {
                    SessionChannels.TryRemove(sessionId, out _);
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

            // Send request to ChatBotActor
            var responseTask = chatBotActor.Ask<ChatBotResponse>(userRequest, TimeSpan.FromSeconds(60));

            // Handle the response asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    var response = await responseTask;

                    // Send reasoning steps
                    if (response.ReasoningSteps != null)
                    {
                        foreach (var step in response.ReasoningSteps)
                        {
                            await BroadcastToSession(sessionId, new StreamingUpdate
                            {
                                SessionId = sessionId,
                                UpdateType = StreamUpdateType.Reasoning,
                                Content = step
                            });
                            await Task.Delay(100); // Small delay for animation effect
                        }
                    }

                    // Stream the response message character by character for typing effect
                    await StreamResponseMessage(sessionId, response.Message, response.Type, response.ReferencedMemoryIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing chat response for session {SessionId}", sessionId);
                    await BroadcastToSession(sessionId, new StreamingUpdate
                    {
                        SessionId = sessionId,
                        UpdateType = StreamUpdateType.Error,
                        Content = "An error occurred processing your request."
                    });
                }
            });

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

        return Ok(new
        {
            sessionId,
            exists,
            hasActiveConnections,
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
        if (SessionChannels.TryGetValue(sessionId, out var channels))
        {
            var tasks = channels.Select(channel =>
                channel.Writer.TryWrite(update) ? Task.CompletedTask : Task.CompletedTask
            );
            await Task.WhenAll(tasks);
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