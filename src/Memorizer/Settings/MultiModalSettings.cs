namespace Memorizer.Settings;

/// <summary>
/// Settings for Multi-modal services (image + text processing)
/// </summary>
public sealed class MultiModalSettings
{
    /// <summary>
    /// API URL for the Multi-modal service
    /// </summary>
    public Uri ApiUrl { get; set; } = new("http://localhost:1234");

    /// <summary>
    /// Model name to use for multi-modal operations
    /// Default: qwen2/qwen3-vl-8b
    /// </summary>
    public string Model { get; set; } = "qwen2/qwen3-vl-8b";

    /// <summary>
    /// Timeout for multi-modal requests
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Temperature for model responses (0.0 - 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>
    /// Maximum tokens for response
    /// </summary>
    public int MaxTokens { get; set; } = 1000;
}
