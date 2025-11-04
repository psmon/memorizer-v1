using Memorizer.Models;

namespace Memorizer.Services;

/// <summary>
/// Service for interacting with Multi-modal models that can process both images and text
/// </summary>
public interface IMultiModalService : IDisposable
{
    /// <summary>
    /// Analyzes an image with a text prompt
    /// </summary>
    /// <param name="imageData">The image data as byte array</param>
    /// <param name="prompt">The text prompt/question about the image</param>
    /// <param name="imageFormat">Image format (e.g., "jpeg", "png")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The model's response</returns>
    Task<string> AnalyzeImageAsync(
        byte[] imageData,
        string prompt,
        string imageFormat = "jpeg",
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Tests connectivity to the Multi-modal service
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if the service is available and configured</returns>
    Task<MultiModalHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of Multi-modal service health check
/// </summary>
public class MultiModalHealthResult
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public string? ErrorDetails { get; set; }
}
