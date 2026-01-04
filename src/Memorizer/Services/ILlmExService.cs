namespace Memorizer.Services;

/// <summary>
/// Service for LLM-EX (Extended Large Language Model) for advanced deep analysis features
/// Uses higher capacity model for complex tasks like PRD analysis and Event Storming
/// </summary>
public interface ILlmExService : IDisposable
{
    /// <summary>
    /// Tests connectivity to the LLM-EX service
    /// </summary>
    Task<LlmHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a completion request to the LLM-EX with streaming response
    /// </summary>
    IAsyncEnumerable<string> CompleteStreamingAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a non-streaming completion request to the LLM-EX
    /// </summary>
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
