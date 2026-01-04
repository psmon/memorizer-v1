namespace Memorizer.Settings;

/// <summary>
/// Settings for LLM-EX (Extended Large Language Model) service
/// Used for advanced deep analysis features requiring higher capacity models
/// </summary>
public sealed class LlmExSettings
{
    public string Type { get; set; } = "Custom";

    /// <summary>
    /// API URL for the LLM-EX service
    /// </summary>
    public Uri ApiUrl { get; set; } = new("http://localhost:1234");

    /// <summary>
    /// Model name to use for LLM-EX operations (e.g., openai/gpt-oss-120b)
    /// </summary>
    public string Model { get; set; } = "openai/gpt-oss-120b";

    /// <summary>
    /// Timeout for LLM-EX requests (default 5 minutes for complex analysis)
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
}
