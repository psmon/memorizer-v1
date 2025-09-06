namespace Memorizer.Settings;

public class EmbeddingSettings
{   
    public string Type { get; init; } = "Ollama";
    public required Uri ApiUrl { get; init; }
    public required string Model { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
    public string ApiKey { get; init; } = string.Empty;
}