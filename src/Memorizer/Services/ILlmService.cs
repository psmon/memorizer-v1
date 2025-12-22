using Memorizer.Models;

namespace Memorizer.Services;

/// <summary>
/// Service for interacting with Large Language Models for text analysis
/// </summary>
public interface ILlmService : IDisposable
{
    /// <summary>
    /// Tests connectivity to the LLM service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if the service is available and configured</returns>
    Task<LlmHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a descriptive title for content that doesn't have one
    /// </summary>
    /// <param name="content">The text content to generate a title for</param>
    /// <param name="contentType">Type of content (e.g., "reference", "how-to")</param>
    /// <param name="existingTags">Existing tags for context</param>
    /// <param name="maxTitleLength">Maximum length for the generated title</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated title</returns>
    Task<string> GenerateTitle(
        string content,
        string contentType,
        string[]? existingTags = null,
        int maxTitleLength = 80,
        CancellationToken cancellationToken = default
    );
    
    /// <summary>
    /// Sends a generic completion request to the LLM
    /// </summary>
    /// <param name="prompt">The prompt to send to the LLM</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The LLM's response</returns>
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a completion request to the LLM with streaming response
    /// </summary>
    /// <param name="prompt">The prompt to send to the LLM</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of response chunks</returns>
    IAsyncEnumerable<string> CompleteStreamingAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts keywords from content for creating NodeWord relationships
    /// </summary>
    /// <param name="content">The text content to extract keywords from</param>
    /// <param name="contentType">Type of content for context</param>
    /// <param name="maxKeywords">Maximum number of keywords to extract</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of extracted keywords</returns>
    Task<List<string>> ExtractKeywordsAsync(
        string content,
        string contentType,
        int maxKeywords = 10,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Result of LLM health check
/// </summary>
public class LlmHealthResult
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public string? ErrorDetails { get; set; }
} 