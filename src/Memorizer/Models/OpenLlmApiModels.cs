using System.Text.Json.Serialization;

namespace Memorizer.Models.OpenLlm;

/// <summary>
/// Chat message for OpenLLM API
/// </summary>
public class ApiChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Chat completion request following OpenAI API specification
/// </summary>
public class ApiChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "openai/gpt-oss-20b";

    [JsonPropertyName("messages")]
    public List<ApiChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 5000;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}

/// <summary>
/// Chat completion response following OpenAI API specification
/// </summary>
public class ApiChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion";

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<ApiChatChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public ApiUsageInfo? Usage { get; set; }
}

/// <summary>
/// Chat completion choice
/// </summary>
public class ApiChatChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public ApiChatMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("delta")]
    public ApiChatMessage? Delta { get; set; }
}

/// <summary>
/// Token usage information
/// </summary>
public class ApiUsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// Text completion request following OpenAI API specification
/// </summary>
public class ApiCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "openai/gpt-oss-20b";

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 5000;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;
}

/// <summary>
/// Text completion response following OpenAI API specification
/// </summary>
public class ApiCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = "text_completion";

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<ApiCompletionChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public ApiUsageInfo? Usage { get; set; }
}

/// <summary>
/// Completion choice
/// </summary>
public class ApiCompletionChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// Streaming chunk for SSE
/// </summary>
public class ApiStreamingChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion.chunk";

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<ApiChatChoice> Choices { get; set; } = new();
}
