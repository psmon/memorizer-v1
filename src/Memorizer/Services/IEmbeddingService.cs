using System.Text.Json;

namespace Memorizer.Services;

public interface IEmbeddingService
{
    Task<float[]> Generate(
        string text,
        CancellationToken cancellationToken = default
    );

    Task<float[]> Generate(
        JsonDocument document,
        CancellationToken cancellationToken = default
    );
    
    int GetEmbeddingDimensions();
}