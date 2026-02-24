namespace Memorizer.Actors;

/// <summary>
/// User message to the SkillMaker actor
/// </summary>
public sealed record SkillMakerUserMessage : IChatBotMessage
{
    public required string SessionId { get; init; }
    public required string Message { get; init; }
    public required SkillMakerMessageType MessageType { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Type of user message in the skill creation flow
/// </summary>
public enum SkillMakerMessageType
{
    Category,
    SubSkill,
    Answer,
    CustomInput
}

/// <summary>
/// Response from the SkillMaker actor
/// </summary>
public sealed record SkillMakerResponse : IChatBotMessage
{
    public required string SessionId { get; init; }
    public required SkillMakerResponseType ResponseType { get; init; }
    public required string Content { get; init; }
    public List<string>? Options { get; init; }
    public bool IsComplete { get; init; }
    public string? SkillContent { get; init; }
    public string? ShortCode { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Type of response from the SkillMaker actor
/// </summary>
public enum SkillMakerResponseType
{
    CategorySelection,
    SubSkillSuggestion,
    FollowUpQuestion,
    Generating,
    SkillComplete,
    Error
}

/// <summary>
/// Streaming update for SkillMaker SSE
/// </summary>
public sealed record SkillMakerStreamingUpdate : IChatBotMessage
{
    public required string SessionId { get; init; }
    public required SkillMakerUpdateType UpdateType { get; init; }
    public required string Content { get; init; }
    public List<string>? Options { get; init; }
    public bool IsComplete { get; init; }
    public string? SkillContent { get; init; }
    public string? UsageGuide { get; init; }
    public string? Insight { get; init; }   // 질문의 인사이트 (왜 이 질문을?)
    public string? Example { get; init; }   // 답변 예시
    public string? NextMessageType { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Type of streaming update
/// </summary>
public enum SkillMakerUpdateType
{
    Phase,
    Options,
    Question,
    Chunk,
    Complete,
    Error
}

/// <summary>
/// Session timeout for SkillMaker
/// </summary>
public sealed record SkillMakerSessionTimeout : IChatBotMessage
{
    public required string SessionId { get; init; }
}

/// <summary>
/// Conversation entry for skill creation process
/// </summary>
public sealed record SkillConversationEntry
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public SkillMakerMessageType? MessageType { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
