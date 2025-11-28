namespace Memorizer.Settings;

/// <summary>
/// Settings for Multi-modal services (image + text processing)
/// </summary>
public sealed class MultiModalSettings
{
    /// <summary>
    /// Type of multi-modal service to use: "OpenAI", "Custom"
    /// Default: Custom
    /// </summary>
    public string Type { get; set; } = "Custom";

    /// <summary>
    /// API URL for the Multi-modal service (used by Custom type)
    /// </summary>
    public Uri ApiUrl { get; set; } = new("http://localhost:1234");

    /// <summary>
    /// API Key for the Multi-modal service (required for OpenAI type)
    /// </summary>
    public string? ApiKey { get; set; }

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
