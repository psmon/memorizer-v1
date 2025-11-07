namespace Memorizer.Settings;

/// <summary>
/// Settings for AskBot feature
/// </summary>
public sealed class AskBotSettings
{
    /// <summary>
    /// Directory path for storing uploaded images
    /// Default: AskBotImages
    /// </summary>
    public string ImageStoragePath { get; set; } = "AskBotImages";
}
