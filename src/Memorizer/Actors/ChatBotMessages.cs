namespace Memorizer.Actors;

/// <summary>
/// Base interface for all chatbot actor messages
/// </summary>
public interface IChatBotMessage
{
}

/// <summary>
/// User request to the chatbot
/// </summary>
public sealed record UserChatRequest : IChatBotMessage
{
    /// <summary>
    /// Unique session identifier for the user
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The user's message/query
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// User who initiated the request
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Optional image data for multi-modal requests (byte array)
    /// </summary>
    public byte[]? ImageData { get; init; }

    /// <summary>
    /// Image format (e.g., "jpeg", "png") when ImageData is provided
    /// </summary>
    public string? ImageFormat { get; init; }

    /// <summary>
    /// Timestamp of the request
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if this is a multi-modal request (has image)
    /// </summary>
    public bool IsMultiModal => ImageData != null && ImageData.Length > 0;
}

/// <summary>
/// Response from the chatbot to the user
/// </summary>
public sealed record ChatBotResponse : IChatBotMessage
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The chatbot's response message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Type of response (memory-based or general)
    /// </summary>
    public required ResponseType Type { get; init; }

    /// <summary>
    /// Referenced memory IDs if response is based on memories
    /// </summary>
    public List<Guid>? ReferencedMemoryIds { get; init; }

    /// <summary>
    /// Reasoning steps taken to generate the response
    /// </summary>
    public List<string>? ReasoningSteps { get; init; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Type of chatbot response
/// </summary>
public enum ResponseType
{
    /// <summary>
    /// Response based on memory search results
    /// </summary>
    MemoryBased,

    /// <summary>
    /// General LLM response when no relevant memories found
    /// </summary>
    General,

    /// <summary>
    /// Error response
    /// </summary>
    Error
}

/// <summary>
/// Request to search for memories related to a query
/// </summary>
public sealed record SearchMemoryRequest : IChatBotMessage
{
    /// <summary>
    /// The original user query
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Session identifier for tracking
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; init; } = 5;

    /// <summary>
    /// Minimum similarity threshold
    /// </summary>
    public double MinSimilarity { get; init; } = 0.3;
}

/// <summary>
/// Response from memory search
/// </summary>
public sealed record SearchMemoryResponse : IChatBotMessage
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Original query
    /// </summary>
    public required string OriginalQuery { get; init; }

    /// <summary>
    /// Transformed query used for search
    /// </summary>
    public string? TransformedQuery { get; init; }

    /// <summary>
    /// Found memories
    /// </summary>
    public required List<Models.Memory> Memories { get; init; }

    /// <summary>
    /// Whether search was required for this query
    /// </summary>
    public bool SearchPerformed { get; init; }

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryAttempts { get; init; }

    /// <summary>
    /// Keywords extracted for retry attempts
    /// </summary>
    public List<string>? ExtractedKeywords { get; init; }
}

/// <summary>
/// Request to evaluate relevance of search results
/// </summary>
public sealed record EvaluateRelevanceRequest : IChatBotMessage
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Original user query
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Memories to evaluate
    /// </summary>
    public required List<Models.Memory> Memories { get; init; }
}

/// <summary>
/// Response from relevance evaluation
/// </summary>
public sealed record EvaluateRelevanceResponse : IChatBotMessage
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Whether relevant memories were found
    /// </summary>
    public required bool HasRelevantMemories { get; init; }

    /// <summary>
    /// Relevant memories (if any)
    /// </summary>
    public List<Models.Memory>? RelevantMemories { get; init; }

    /// <summary>
    /// Reasoning for the decision
    /// </summary>
    public string? Reasoning { get; init; }
}

/// <summary>
/// Internal message for session timeout
/// </summary>
public sealed record SessionTimeout : IChatBotMessage
{
    /// <summary>
    /// Session that timed out
    /// </summary>
    public required string SessionId { get; init; }
}

/// <summary>
/// Message to reset session timer
/// </summary>
public sealed record ResetSessionTimer : IChatBotMessage
{
    /// <summary>
    /// Session to reset timer for
    /// </summary>
    public required string SessionId { get; init; }
}

/// <summary>
/// Streaming update message for SSE
/// </summary>
public sealed record StreamingUpdate : IChatBotMessage
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Type of update
    /// </summary>
    public required StreamUpdateType UpdateType { get; init; }

    /// <summary>
    /// Update content
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Timestamp of the update
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Type of streaming update
/// </summary>
public enum StreamUpdateType
{
    /// <summary>
    /// Reasoning step update
    /// </summary>
    Reasoning,

    /// <summary>
    /// Search progress update
    /// </summary>
    SearchProgress,

    /// <summary>
    /// Partial response update
    /// </summary>
    PartialResponse,

    /// <summary>
    /// Final response
    /// </summary>
    FinalResponse,

    /// <summary>
    /// Error update
    /// </summary>
    Error
}

/// <summary>
/// Actor registry key for SearchMemoryActor
/// </summary>
public sealed class SearchMemoryActorKey;

/// <summary>
/// Actor registry key for DecisionActor
/// </summary>
public sealed class DecisionActorKey;

/// <summary>
/// Actor registry key for ChatBotActor parent/supervisor
/// </summary>
public sealed class ChatBotSupervisorActorKey;

/// <summary>
/// Represents a single conversation entry (user message + bot response)
/// </summary>
public sealed record ConversationEntry
{
    /// <summary>
    /// User's message
    /// </summary>
    public required string UserMessage { get; init; }

    /// <summary>
    /// Bot's response
    /// </summary>
    public required string BotResponse { get; init; }

    /// <summary>
    /// Timestamp of the exchange
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this exchange involved memory search
    /// </summary>
    public bool UsedMemorySearch { get; init; }

    /// <summary>
    /// List of referenced memory IDs used in this response
    /// </summary>
    public List<Guid>? ReferencedMemoryIds { get; init; }

    /// <summary>
    /// Image data from user's message (if multi-modal request)
    /// </summary>
    public byte[]? ImageData { get; init; }

    /// <summary>
    /// Image format (e.g., "jpeg", "png") when ImageData is provided
    /// </summary>
    public string? ImageFormat { get; init; }
}

/// <summary>
/// Message to extract important context from conversation history
/// </summary>
public sealed record ExtractContextRequest : IChatBotMessage
{
    /// <summary>
    /// Conversation entries to extract context from
    /// </summary>
    public required List<ConversationEntry> ConversationHistory { get; init; }

    /// <summary>
    /// Current short-term memory to merge with
    /// </summary>
    public string? CurrentShortTermMemory { get; init; }
}

/// <summary>
/// Response with extracted context
/// </summary>
public sealed record ExtractContextResponse : IChatBotMessage
{
    /// <summary>
    /// Updated short-term memory (max 500 chars)
    /// </summary>
    public required string ShortTermMemory { get; init; }
}

/// <summary>
/// Request to get conversation history from actor
/// </summary>
public sealed record GetConversationHistoryRequest : IChatBotMessage
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public required string SessionId { get; init; }
}

/// <summary>
/// Response with conversation history
/// </summary>
public sealed record GetConversationHistoryResponse : IChatBotMessage
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// List of conversation entries
    /// </summary>
    public required List<ConversationEntry> ConversationEntries { get; init; }
}