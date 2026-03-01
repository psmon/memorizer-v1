using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Memorizer.Actors;
using Memorizer.Services;
using Memorizer.Settings;
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
    private readonly ILlmExService? _llmExService;
    private readonly IMultiModalService? _multiModalService;
    private readonly IWebSearchService? _webSearchService;
    private readonly ILogger<AskBotController> _logger;
    private readonly Npgsql.NpgsqlDataSource _dataSource;
    private readonly IStorage _storage;
    private readonly AskBotSettings _askBotSettings;

    // Static dictionary to maintain ChatBotActors per session
    private static readonly ConcurrentDictionary<string, IActorRef> SessionActors = new();

    // SSE connections per session - key is sessionId, value is dictionary of connectionId -> channel
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Channel<StreamingUpdate>>> SessionChannels = new();

    public AskBotController(
        ActorSystem actorSystem,
        IRequiredActor<SearchMemoryActorKey> searchMemoryActor,
        IRequiredActor<DecisionActorKey> decisionActor,
        ILlmService llmService,
        ILogger<AskBotController> logger,
        Npgsql.NpgsqlDataSource dataSource,
        IStorage storage,
        AskBotSettings askBotSettings,
        ILlmExService? llmExService = null,
        IMultiModalService? multiModalService = null,
        IWebSearchService? webSearchService = null)
    {
        _actorSystem = actorSystem;
        _searchMemoryActor = searchMemoryActor.ActorRef;
        _decisionActor = decisionActor.ActorRef;
        _llmService = llmService;
        _llmExService = llmExService;
        _multiModalService = multiModalService;
        _webSearchService = webSearchService;
        _logger = logger;
        _dataSource = dataSource;
        _storage = storage;
        _askBotSettings = askBotSettings;
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
    /// Send a message to the chatbot (with optional image)
    /// </summary>
    [HttpPost("message")]
    [AllowAnonymous]
    [RequestSizeLimit(3 * 1024 * 1024)] // 3MB limit
    public async Task<IActionResult> SendMessage([FromForm] string message, [FromForm] string? sessionId = null, [FromForm] IFormFile? image = null, [FromForm] bool useExtendedModel = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            // Get or create session ID
            sessionId = GetOrCreateSessionId(sessionId);

            byte[]? imageData = null;
            string? imageFormat = null;

            // Process image if provided
            if (image != null)
            {
                // Validate file size (3MB max)
                if (image.Length > 3 * 1024 * 1024)
                {
                    return BadRequest(new { error = "Image size cannot exceed 3MB" });
                }

                // Validate file format
                var allowedFormats = new[] { "image/jpeg", "image/jpg", "image/png" };
                if (!allowedFormats.Contains(image.ContentType.ToLower()))
                {
                    return BadRequest(new { error = "Only JPG and PNG image formats are allowed" });
                }

                // Read image data
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                imageData = ms.ToArray();

                // Determine format
                imageFormat = image.ContentType.ToLower().Contains("png") ? "png" : "jpeg";

                _logger.LogInformation("Processing multi-modal request for session {SessionId} with {ImageFormat} image ({ImageSize} bytes)",
                    sessionId, imageFormat, imageData.Length);
            }
            else
            {
                _logger.LogInformation("Processing text-only request for session {SessionId}", sessionId);
            }

            // Get or create ChatBotActor for this session
            var chatBotActor = GetOrCreateChatBotActor(sessionId);

            // Create user chat request
            var userRequest = new UserChatRequest
            {
                SessionId = sessionId,
                Message = message,
                UserId = "anonymous",
                ImageData = imageData,
                ImageFormat = imageFormat,
                UseExtendedModel = useExtendedModel
            };

            // Send initial processing update via SSE
            await BroadcastToSession(sessionId, new StreamingUpdate
            {
                SessionId = sessionId,
                UpdateType = StreamUpdateType.SearchProgress,
                Content = userRequest.IsMultiModal ? "Processing your multi-modal request..." : "Processing your request..."
            });

            // Send request to ChatBotActor using Tell (fire and forget)
            // The response will come through SSE via StreamingChatBotActor
            chatBotActor.Tell(userRequest);

            // Note: The response will be streamed through SSE events
            // StreamingChatBotActor handles all the messaging through the SSE bridge

            return Accepted(new { sessionId, status = "processing", isMultiModal = userRequest.IsMultiModal });
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

    /// <summary>
    /// Get conversation history for a session
    /// </summary>
    [HttpGet("session/{sessionId}/history")]
    public async Task<IActionResult> GetSessionHistory(string sessionId)
    {
        try
        {
            // Check if session exists
            if (!SessionActors.TryGetValue(sessionId, out var chatBotActor))
            {
                return NotFound(new { error = "Session not found" });
            }

            // Request conversation history from actor
            var historyResponse = await chatBotActor.Ask<GetConversationHistoryResponse>(
                new GetConversationHistoryRequest { SessionId = sessionId },
                TimeSpan.FromSeconds(10)
            );

            // Convert to message format for UI
            var messages = new List<object>();
            foreach (var entry in historyResponse.ConversationEntries)
            {
                messages.Add(new { role = "user", content = entry.UserMessage, timestamp = entry.Timestamp });
                messages.Add(new { role = "assistant", content = entry.BotResponse, timestamp = entry.Timestamp, usedMemorySearch = entry.UsedMemorySearch });
            }

            return Ok(new { sessionId, messages });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session history for {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to retrieve session history" });
        }
    }

    /// <summary>
    /// Create a share link for the current session
    /// </summary>
    [HttpPost("share")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateShareLink([FromBody] ShareRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return BadRequest(new { error = "Session ID is required" });
            }

            // Check if session has any conversation history
            if (!SessionActors.ContainsKey(request.SessionId))
            {
                return BadRequest(new { error = "Session not found or has no conversation history" });
            }

            // Get conversation history from ChatBotActor
            var chatBotActor = SessionActors[request.SessionId];

            // Request conversation history from actor
            GetConversationHistoryResponse? historyResponse = null;
            var messages = new List<object>();

            try
            {
                historyResponse = await chatBotActor.Ask<GetConversationHistoryResponse>(
                    new GetConversationHistoryRequest { SessionId = request.SessionId },
                    TimeSpan.FromSeconds(10)
                );

                // Convert conversation entries to messages
                foreach (var entry in historyResponse.ConversationEntries)
                {
                    messages.Add(new
                    {
                        role = "user",
                        content = entry.UserMessage,
                        timestamp = entry.Timestamp,
                        hasImage = entry.ImageData != null && entry.ImageData.Length > 0
                    });
                    messages.Add(new
                    {
                        role = "assistant",
                        content = entry.BotResponse,
                        timestamp = entry.Timestamp,
                        usedMemorySearch = entry.UsedMemorySearch,
                        referencedMemoryIds = entry.ReferencedMemoryIds ?? new List<Guid>(),
                        webSearchReferences = (entry.WebSearchReferences ?? new List<Actors.WebSearchReference>())
                            .Select(r => new { title = r.Title, url = r.Url, snippet = r.Snippet }).ToList()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve conversation history for session {SessionId}, creating empty share",
                    request.SessionId);
                // Continue with empty messages - still create the share link
            }

            _logger.LogInformation("Creating share link for session {SessionId} with {Count} messages",
                request.SessionId, messages.Count);

            // Check if share link already exists for this session
            await using var conn = await _dataSource.OpenConnectionAsync();

            var existingQuery = @"
                SELECT short_code, content, referenced_memories
                FROM askbot_share_links
                WHERE session_id = @sessionId
                LIMIT 1";

            await using var checkCmd = new Npgsql.NpgsqlCommand(existingQuery, conn);
            checkCmd.Parameters.AddWithValue("sessionId", request.SessionId);

            string? existingShortCode = null;
            await using var reader = await checkCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                existingShortCode = reader.GetString(0);
                _logger.LogInformation("Found existing share link {ShortCode} for session {SessionId}, will update it",
                    existingShortCode, request.SessionId);
            }
            await reader.CloseAsync();

            // Generate or reuse short code
            string shortCode;

            if (existingShortCode != null)
            {
                // Reuse existing short code for update
                shortCode = existingShortCode;
            }
            else
            {
                // Generate new unique short code
                int attempts = 0;
                const int maxAttempts = 10;

                do
                {
                    shortCode = GenerateShortCode();
                    attempts++;

                    if (attempts > maxAttempts)
                    {
                        return StatusCode(500, new { error = "Failed to generate unique short code" });
                    }
                } while (await ShortCodeExists(shortCode, conn));
            }

            // Create conversation snapshot
            var conversationSnapshot = new
            {
                sessionId = request.SessionId,
                capturedAt = DateTime.UtcNow,
                messages = messages
            };

            var contentJson = System.Text.Json.JsonSerializer.Serialize(conversationSnapshot);

            // Extract referenced memories from messages for easier querying
            var referencedMemories = new List<object>();
            int messageIndex = 0;
            foreach (dynamic msg in messages)
            {
                if (msg.GetType().GetProperty("referencedMemoryIds") != null)
                {
                    var memoryIds = msg.referencedMemoryIds as List<Guid>;
                    if (memoryIds != null && memoryIds.Count > 0)
                    {
                        referencedMemories.Add(new
                        {
                            messageIndex = messageIndex,
                            memoryIds = memoryIds
                        });
                    }
                }
                messageIndex++;
            }

            var referencedMemoriesJson = referencedMemories.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(referencedMemories)
                : null;

            // Extract web search references from messages
            var webSearchRefs = new List<object>();
            int webMsgIndex = 0;
            foreach (dynamic msg in messages)
            {
                if (msg.GetType().GetProperty("webSearchReferences") != null)
                {
                    var refs = msg.webSearchReferences as List<Actors.WebSearchReference>;
                    if (refs != null && refs.Count > 0)
                    {
                        webSearchRefs.Add(new
                        {
                            messageIndex = webMsgIndex,
                            references = refs.Select(r => new { title = r.Title, url = r.Url, snippet = r.Snippet })
                        });
                    }
                }
                webMsgIndex++;
            }

            var webSearchRefsJson = webSearchRefs.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(webSearchRefs)
                : null;

            // Save images to disk and create image paths mapping
            var imagePaths = new Dictionary<int, string>();
            if (historyResponse != null)
            {
                int entryIndex = 0;
                foreach (var entry in historyResponse.ConversationEntries)
                {
                    if (entry.ImageData != null && entry.ImageData.Length > 0)
                    {
                        try
                        {
                            // Save image to disk
                            var imagePath = await SaveImageToDisk(shortCode, entryIndex, entry.ImageData, entry.ImageFormat ?? "jpeg");
                            // Store mapping of message index (user message) to image path
                            // User message index is entryIndex * 2 (because each entry has user + assistant messages)
                            imagePaths[entryIndex * 2] = imagePath;

                            _logger.LogInformation("Saved image for session {SessionId}, entry {EntryIndex} to {ImagePath}",
                                request.SessionId, entryIndex, imagePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to save image for session {SessionId}, entry {EntryIndex}",
                                request.SessionId, entryIndex);
                        }
                    }
                    entryIndex++;
                }
            }

            var imagePathsJson = imagePaths.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(imagePaths)
                : null;

            // Insert or update in database
            if (existingShortCode != null)
            {
                // Update existing share
                var updateQuery = @"
                    UPDATE askbot_share_links
                    SET content = @content::jsonb,
                        referenced_memories = @referencedMemories::jsonb,
                        web_search_references = @webSearchRefs::jsonb,
                        image_paths = @imagePaths::jsonb,
                        updated_at = @updatedAt
                    WHERE short_code = @shortCode";

                await using var cmd = new Npgsql.NpgsqlCommand(updateQuery, conn);
                cmd.Parameters.AddWithValue("shortCode", shortCode);
                cmd.Parameters.AddWithValue("content", contentJson);
                cmd.Parameters.AddWithValue("referencedMemories",
                    referencedMemoriesJson != null ? (object)referencedMemoriesJson : DBNull.Value);
                cmd.Parameters.AddWithValue("webSearchRefs",
                    webSearchRefsJson != null ? (object)webSearchRefsJson : DBNull.Value);
                cmd.Parameters.AddWithValue("imagePaths",
                    imagePathsJson != null ? (object)imagePathsJson : DBNull.Value);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Updated share link {ShortCode} for session {SessionId}",
                    shortCode, request.SessionId);
            }
            else
            {
                // Insert new share
                var insertQuery = @"
                    INSERT INTO askbot_share_links (short_code, session_id, content, referenced_memories, web_search_references, image_paths, created_at)
                    VALUES (@shortCode, @sessionId, @content::jsonb, @referencedMemories::jsonb, @webSearchRefs::jsonb, @imagePaths::jsonb, @createdAt)";

                await using var cmd = new Npgsql.NpgsqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("shortCode", shortCode);
                cmd.Parameters.AddWithValue("sessionId", request.SessionId);
                cmd.Parameters.AddWithValue("content", contentJson);
                cmd.Parameters.AddWithValue("referencedMemories",
                    referencedMemoriesJson != null ? (object)referencedMemoriesJson : DBNull.Value);
                cmd.Parameters.AddWithValue("webSearchRefs",
                    webSearchRefsJson != null ? (object)webSearchRefsJson : DBNull.Value);
                cmd.Parameters.AddWithValue("imagePaths",
                    imagePathsJson != null ? (object)imagePathsJson : DBNull.Value);
                cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Created share link {ShortCode} for session {SessionId}",
                    shortCode, request.SessionId);
            }

            return Ok(new { shortCode, sessionId = request.SessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating share link");
            return StatusCode(500, new { error = "Failed to create share link" });
        }
    }

    /// <summary>
    /// Get session by share code
    /// </summary>
    [HttpGet("share/{shortCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSessionByShareCode(string shortCode)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            var query = @"
                SELECT session_id, created_at, content, referenced_memories, image_paths, web_search_references
                FROM askbot_share_links
                WHERE short_code = @shortCode
                LIMIT 1";

            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("shortCode", shortCode);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var sessionId = reader.GetString(0);
                var createdAt = reader.GetDateTime(1);
                var contentJson = reader.IsDBNull(2) ? null : reader.GetString(2);
                var referencedMemoriesJson = reader.IsDBNull(3) ? null : reader.GetString(3);
                var imagePathsJson = reader.IsDBNull(4) ? null : reader.GetString(4);
                var webSearchRefsJson = reader.IsDBNull(5) ? null : reader.GetString(5);

                _logger.LogInformation("Retrieved session {SessionId} for share code {ShortCode}",
                    sessionId, shortCode);

                object? content = null;
                if (!string.IsNullOrEmpty(contentJson))
                {
                    content = System.Text.Json.JsonSerializer.Deserialize<object>(contentJson);
                }

                object? referencedMemories = null;
                if (!string.IsNullOrEmpty(referencedMemoriesJson))
                {
                    referencedMemories = System.Text.Json.JsonSerializer.Deserialize<object>(referencedMemoriesJson);
                }

                object? imagePaths = null;
                if (!string.IsNullOrEmpty(imagePathsJson))
                {
                    imagePaths = System.Text.Json.JsonSerializer.Deserialize<object>(imagePathsJson);
                }

                object? webSearchReferences = null;
                if (!string.IsNullOrEmpty(webSearchRefsJson))
                {
                    webSearchReferences = System.Text.Json.JsonSerializer.Deserialize<object>(webSearchRefsJson);
                }

                return Ok(new { sessionId, createdAt, shortCode, content, referencedMemories, imagePaths, webSearchReferences });
            }

            return NotFound(new { error = "Share link not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving share link");
            return StatusCode(500, new { error = "Failed to retrieve share link" });
        }
    }

    /// <summary>
    /// Serve saved images for shared conversations
    /// </summary>
    [HttpGet("share/{shortCode}/images/{fileName}")]
    [AllowAnonymous]
    public IActionResult GetSharedImage(string shortCode, string fileName)
    {
        try
        {
            // Determine base storage path (absolute or relative)
            var basePath = Path.IsPathRooted(_askBotSettings.ImageStoragePath)
                ? _askBotSettings.ImageStoragePath
                : Path.Combine(Directory.GetCurrentDirectory(), _askBotSettings.ImageStoragePath);

            // Construct the image path
            var imagePath = Path.Combine(basePath, shortCode, fileName);

            _logger.LogInformation("Attempting to serve image from: {ImagePath}", imagePath);

            // Check if file exists
            if (!System.IO.File.Exists(imagePath))
            {
                _logger.LogWarning("Image not found at path: {ImagePath}", imagePath);
                return NotFound(new { error = "Image not found" });
            }

            // Determine content type based on file extension
            var extension = Path.GetExtension(fileName).ToLower();
            var contentType = extension == ".png" ? "image/png" : "image/jpeg";

            // Read and return the image file
            var imageBytes = System.IO.File.ReadAllBytes(imagePath);
            return File(imageBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving image for share code {ShortCode}, file {FileName}", shortCode, fileName);
            return StatusCode(500, new { error = "Failed to retrieve image" });
        }
    }

    /// <summary>
    /// Get all share links with pagination
    /// </summary>
    [HttpGet("share")]
    [AllowAnonymous]
    public async Task<IActionResult> GetShareLinks([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 30;

            await using var conn = await _dataSource.OpenConnectionAsync();

            // Get total count
            var countQuery = "SELECT COUNT(*) FROM askbot_share_links";
            await using var countCmd = new Npgsql.NpgsqlCommand(countQuery, conn);
            var totalCount = (long)(await countCmd.ExecuteScalarAsync() ?? 0L);

            // Get paginated results
            var query = @"
                SELECT short_code, session_id, created_at, content
                FROM askbot_share_links
                ORDER BY created_at DESC
                LIMIT @pageSize OFFSET @offset";

            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("pageSize", pageSize);
            cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);

            await using var reader = await cmd.ExecuteReaderAsync();

            var shareLinks = new List<object>();
            while (await reader.ReadAsync())
            {
                var shortCode = reader.GetString(0);
                var sessionId = reader.GetString(1);
                var createdAt = reader.GetDateTime(2);
                var contentJson = reader.IsDBNull(3) ? null : reader.GetString(3);

                // Extract summary from content
                string summary = "No conversation";
                if (!string.IsNullOrEmpty(contentJson))
                {
                    try
                    {
                        var contentObj = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(contentJson);
                        if (contentObj.TryGetProperty("messages", out var messages) &&
                            messages.GetArrayLength() > 0)
                        {
                            // Get first user message as summary
                            foreach (var msg in messages.EnumerateArray())
                            {
                                if (msg.TryGetProperty("role", out var role) &&
                                    role.GetString() == "user" &&
                                    msg.TryGetProperty("content", out var content))
                                {
                                    var text = content.GetString() ?? "";
                                    summary = text.Length > 100 ? text.Substring(0, 100) + "..." : text;
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        summary = "Unable to load summary";
                    }
                }

                shareLinks.Add(new
                {
                    shortCode,
                    sessionId,
                    createdAt,
                    summary
                });
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            _logger.LogInformation("Retrieved {Count} share links (page {Page} of {TotalPages})",
                shareLinks.Count, page, totalPages);

            return Ok(new
            {
                items = shareLinks,
                page,
                pageSize,
                totalCount,
                totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving share links");
            return StatusCode(500, new { error = "Failed to retrieve share links" });
        }
    }

    private async Task<bool> ShortCodeExists(string shortCode, Npgsql.NpgsqlConnection conn)
    {
        var query = "SELECT COUNT(*) FROM askbot_share_links WHERE short_code = @shortCode";
        await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("shortCode", shortCode);

        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
        return count > 0;
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

    /// <summary>
    /// Save shared conversation as memory
    /// </summary>
    [HttpPost("share/{shortCode}/save-memory")]
    public async Task<IActionResult> SaveConversationAsMemory(string shortCode, [FromBody] SaveMemoryRequest request)
    {
        try
        {
            // Check authentication
            var isAuthenticated = HttpContext.Session.GetString("IsAuthenticated") == "true";
            if (!isAuthenticated)
            {
                return Unauthorized(new { error = "Authentication required to save memories" });
            }

            // Get conversation by short code
            await using var conn = await _dataSource.OpenConnectionAsync();
            var query = @"
                SELECT session_id, content, referenced_memories
                FROM askbot_share_links
                WHERE short_code = @shortCode
                LIMIT 1";

            await using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("shortCode", shortCode);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return NotFound(new { error = "Share link not found" });
            }

            var contentJson = reader.IsDBNull(1) ? null : reader.GetString(1);
            var referencedMemoriesJson = reader.IsDBNull(2) ? null : reader.GetString(2);
            await reader.CloseAsync();

            if (string.IsNullOrEmpty(contentJson))
            {
                return BadRequest(new { error = "No conversation content found" });
            }

            // Parse conversation content
            var content = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(contentJson);
            if (!content.TryGetProperty("messages", out var messages))
            {
                return BadRequest(new { error = "Invalid conversation format" });
            }

            // Extract conversation pairs (user + assistant messages)
            var conversationPairs = new List<(JsonElement userMsg, JsonElement assistantMsg)>();
            JsonElement? currentUserMsg = null;

            foreach (var msg in messages.EnumerateArray())
            {
                if (!msg.TryGetProperty("role", out var role))
                    continue;

                var roleStr = role.GetString();

                if (roleStr == "user")
                {
                    currentUserMsg = msg;
                }
                else if (roleStr == "assistant" && currentUserMsg.HasValue)
                {
                    conversationPairs.Add((currentUserMsg.Value, msg));
                    currentUserMsg = null;
                }
            }

            if (conversationPairs.Count == 0)
            {
                return BadRequest(new { error = "No valid conversation pairs found" });
            }

            _logger.LogInformation("Extracted {Count} conversation pairs from shared conversation {ShortCode}",
                conversationPairs.Count, shortCode);

            // Store each conversation pair as a separate memory
            var savedMemories = new List<Memorizer.Models.Memory>();
            var username = HttpContext.Session.GetString("Username") ?? "user";

            for (int i = 0; i < conversationPairs.Count; i++)
            {
                var (userMsg, assistantMsg) = conversationPairs[i];

                var userContent = userMsg.TryGetProperty("content", out var uc) ? uc.GetString() : "";
                var assistantContent = assistantMsg.TryGetProperty("content", out var ac) ? ac.GetString() : "";

                // Build conversation text for this pair
                var pairText = new System.Text.StringBuilder();
                pairText.AppendLine($"user: {userContent}");
                pairText.AppendLine();
                pairText.AppendLine($"assistant: {assistantContent}");
                pairText.AppendLine();

                // Use LLM (or LLM-EX if enabled) to analyze and structure this conversation pair
                var analysisPrompt = $@"Analyze the following conversation pair and create metadata for storing it as a knowledge memory.

Conversation:
{pairText}

Provide a JSON response with:
1. title: A concise, descriptive title (max 80 characters)
2. tags: Array of 3-7 relevant tags
3. summary: A brief summary (2-3 sentences) highlighting key information
4. type: Memory type (choose one: 'conversation', 'reference', 'how-to', 'document')

Return ONLY valid JSON, no markdown formatting:
{{
  ""title"": ""..."",
  ""tags"": [...],
  ""summary"": ""..."",
  ""type"": ""...""
}}";

                string llmResponse;
                if (request.UseExtendedModel && _llmExService != null)
                {
                    _logger.LogInformation("Using LLM-EX for memory analysis");
                    llmResponse = await _llmExService.CompleteAsync(analysisPrompt);
                }
                else
                {
                    llmResponse = await _llmService.CompleteAsync(analysisPrompt);
                }

                // Clean LLM response (remove markdown if present)
                var jsonResponse = llmResponse.Trim();
                if (jsonResponse.StartsWith("```json"))
                {
                    jsonResponse = jsonResponse.Substring(7);
                }
                if (jsonResponse.StartsWith("```"))
                {
                    jsonResponse = jsonResponse.Substring(3);
                }
                if (jsonResponse.EndsWith("```"))
                {
                    jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
                }
                jsonResponse = jsonResponse.Trim();

                // Parse LLM response
                JsonElement metadata;
                try
                {
                    metadata = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse LLM response as JSON for pair {Index}: {Response}", i, llmResponse);
                    // Use fallback metadata
                    metadata = JsonDocument.Parse($@"{{
                        ""title"": ""Conversation {i + 1}"",
                        ""tags"": [""conversation"", ""shared""],
                        ""summary"": ""Conversation pair {i + 1}"",
                        ""type"": ""conversation""
                    }}").RootElement;
                }

                var title = metadata.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : $"Conversation {i + 1}";
                var tags = new List<string>();
                if (metadata.TryGetProperty("tags", out var tagsProp))
                {
                    foreach (var tag in tagsProp.EnumerateArray())
                    {
                        tags.Add(tag.GetString() ?? "");
                    }
                }
                var summary = metadata.TryGetProperty("summary", out var summaryProp) ? summaryProp.GetString() : "";
                var memoryType = metadata.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "conversation";

                // Build memory content with summary and conversation
                var memoryContent = new System.Text.StringBuilder();
                if (!string.IsNullOrEmpty(summary))
                {
                    memoryContent.AppendLine($"## Summary");
                    memoryContent.AppendLine(summary);
                    memoryContent.AppendLine();
                }
                memoryContent.AppendLine($"## Conversation");
                memoryContent.AppendLine();
                memoryContent.Append(pairText.ToString());

                // Save memory for this conversation pair
                var memory = await _storage.StoreMemory(
                    memoryType ?? "conversation",
                    memoryContent.ToString(),
                    $"shared-by-{username}",
                    tags.ToArray(),
                    0.9,
                    title: title ?? $"Conversation {i + 1}"
                );

                savedMemories.Add(memory);

                _logger.LogInformation("Saved conversation pair {Index}/{Total} as memory {MemoryId}",
                    i + 1, conversationPairs.Count, memory.Id);
            }

            // Create sequential relationships between conversation memories
            if (savedMemories.Count > 1 && request.CreateRelationships)
            {
                for (int i = 0; i < savedMemories.Count - 1; i++)
                {
                    try
                    {
                        await _storage.CreateRelationship(
                            savedMemories[i].Id,
                            savedMemories[i + 1].Id,
                            "continues-to"
                        );

                        _logger.LogInformation("Created relationship: Memory {FromId} continues-to {ToId}",
                            savedMemories[i].Id, savedMemories[i + 1].Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create relationship between memories {FromId} and {ToId}",
                            savedMemories[i].Id, savedMemories[i + 1].Id);
                    }
                }
            }

            // Create relationships with referenced memories if they exist
            if (!string.IsNullOrEmpty(referencedMemoriesJson) && request.CreateRelationships)
            {
                try
                {
                    var referencedMemories = System.Text.Json.JsonSerializer.Deserialize<List<JsonElement>>(referencedMemoriesJson);
                    if (referencedMemories != null)
                    {
                        foreach (var refMem in referencedMemories)
                        {
                            if (refMem.TryGetProperty("messageIndex", out var msgIndexProp) &&
                                refMem.TryGetProperty("memoryIds", out var memoryIds))
                            {
                                var messageIndex = msgIndexProp.GetInt32();

                                // Find which conversation pair this message belongs to
                                // messageIndex is for all messages, we need to map it to assistant messages
                                var pairIndex = messageIndex / 2; // Approximate mapping (assumes user, assistant pairs)

                                if (pairIndex >= 0 && pairIndex < savedMemories.Count)
                                {
                                    foreach (var memId in memoryIds.EnumerateArray())
                                    {
                                        if (Guid.TryParse(memId.GetString(), out var relatedMemoryId))
                                        {
                                            await _storage.CreateRelationship(
                                                savedMemories[pairIndex].Id,
                                                relatedMemoryId,
                                                "references"
                                            );

                                            _logger.LogInformation("Created reference relationship: Memory {FromId} references {ToId}",
                                                savedMemories[pairIndex].Id, relatedMemoryId);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create relationships with referenced memories");
                }
            }

            _logger.LogInformation("Saved shared conversation {ShortCode} as {Count} memories",
                shortCode, savedMemories.Count);

            return Ok(new {
                memoryIds = savedMemories.Select(m => m.Id).ToList(),
                memoryId = savedMemories.First().Id, // For backward compatibility
                title = savedMemories.First().Title,
                count = savedMemories.Count,
                message = $"Conversation saved successfully as {savedMemories.Count} memories"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving conversation as memory for share code {ShortCode}", shortCode);
            return StatusCode(500, new { error = "Failed to save conversation as memory" });
        }
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

            // Create ChatBotActor with SSE bridge, LLM-EX, MultiModalService and WebSearchService
            var props = StreamingChatBotActor.Props(sid, _searchMemoryActor, _decisionActor, _llmService, sseBridgeActor, _llmExService, _multiModalService, _webSearchService);
            var actorName = $"askbot-{sid}";
            var actor = _actorSystem.ActorOf(props, actorName);

            return actor;
        });
    }

    /// <summary>
    /// Save image to disk for shared conversations
    /// </summary>
    private async Task<string> SaveImageToDisk(string shortCode, int entryIndex, byte[] imageData, string imageFormat)
    {
        // Determine base storage path (absolute or relative)
        var basePath = Path.IsPathRooted(_askBotSettings.ImageStoragePath)
            ? _askBotSettings.ImageStoragePath
            : Path.Combine(Directory.GetCurrentDirectory(), _askBotSettings.ImageStoragePath);

        // Ensure base directory exists
        Directory.CreateDirectory(basePath);

        // Create shortCode subdirectory
        var storageDir = Path.Combine(basePath, shortCode);
        Directory.CreateDirectory(storageDir);

        // Generate filename with entry index and extension
        var extension = imageFormat.ToLower() == "png" ? "png" : "jpg";
        var fileName = $"image_{entryIndex}.{extension}";
        var fullPath = Path.Combine(storageDir, fileName);

        _logger.LogInformation("Saving image to: {FullPath}", fullPath);

        // Save image to disk
        await System.IO.File.WriteAllBytesAsync(fullPath, imageData);

        // Return relative path for storage in database
        return Path.Combine(shortCode, fileName).Replace("\\", "/");
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
/// Request model for sharing a session
/// </summary>
public class ShareRequest
{
    /// <summary>
    /// The session ID to share
    /// </summary>
    public required string SessionId { get; set; }
}

/// <summary>
/// Request model for saving conversation as memory
/// </summary>
public class SaveMemoryRequest
{
    /// <summary>
    /// Whether to create relationships with referenced memories
    /// </summary>
    public bool CreateRelationships { get; set; } = true;

    /// <summary>
    /// Use LLM-EX (extended model) for better analysis when saving as memory
    /// </summary>
    public bool UseExtendedModel { get; set; } = false;
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
        IActorRef sseBridge,
        ILlmExService? llmExService = null,
        IMultiModalService? multiModalService = null,
        IWebSearchService? webSearchService = null)
        : base(sessionId, searchMemoryActor, decisionActor, llmService, llmExService, multiModalService, webSearchService)
    {
        _sessionId = sessionId;
        _sseBridge = sseBridge;
        _askBotController = Context.Parent; // Store reference to parent for response notification

        // Re-register the ChatBotResponse handler to use the overridden method
        // This is necessary because the base constructor already registered it
        Receive<ChatBotResponse>(HandleChatBotResponseFromPipeTo);

        // Note: GetConversationHistoryRequest is already handled by base class
        // We don't need to override it because base implementation is sufficient

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
                    referencedMemoryIds = response.ReferencedMemoryIds ?? new List<Guid>(),
                    hasMemorySearch = response.Type == ResponseType.MemoryBased,
                    hasWebSearch = response.Type == ResponseType.WebSearchBased,
                    webSearchReferences = (response.WebSearchReferences ?? new List<Actors.WebSearchReference>())
                        .Select(r => new { title = r.Title, url = r.Url, snippet = r.Snippet }).ToList()
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

    // Expose conversation state for debugging/monitoring
    public int GetConversationCount() => _conversationEntries.Count;
    public string GetShortTermMemory() => _shortTermMemory;
    public string GetLastImportantResponse() => _lastImportantResponse;

    public static Props Props(
        string sessionId,
        IActorRef searchMemoryActor,
        IActorRef decisionActor,
        ILlmService llmService,
        IActorRef sseBridge,
        ILlmExService? llmExService = null,
        IMultiModalService? multiModalService = null,
        IWebSearchService? webSearchService = null)
    {
        return Akka.Actor.Props.Create(() =>
            new StreamingChatBotActor(sessionId, searchMemoryActor, decisionActor, llmService, sseBridge, llmExService, multiModalService, webSearchService));
    }
}