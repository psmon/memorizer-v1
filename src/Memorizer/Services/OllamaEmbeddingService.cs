using System.Text.Json;
using Memorizer.Models;
using Memorizer.Settings;

namespace Memorizer.Services;



public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingSettings _settings ;
    private readonly ILogger<OllamaEmbeddingService> _logger;
    private int _embeddingDimensions = 384; // Default, will be updated dynamically

    public OllamaEmbeddingService(
        HttpClient httpClient,
        EmbeddingSettings settings,
        ILogger<OllamaEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        _httpClient.Timeout = _settings.Timeout;
    }

    public async Task<float[]> Generate(
        string text,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Generating embedding for text of length {TextLength}", text.Length);

            EmbeddingRequest request = new() { Model = _settings.Model, Prompt = text };

            _logger.LogDebug("Sending request to embedding API at {ApiUrl}", _settings.ApiUrl);
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                "api/embeddings",
                request,
                cancellationToken
            );
            response.EnsureSuccessStatusCode();

            EmbeddingResponse? result =
                await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
                    cancellationToken: cancellationToken
                );
            if (result?.Embedding == null || result.Embedding.Length == 0)
            {
                throw new Exception("Failed to generate embedding: Empty response from API");
            }

            _logger.LogDebug("Successfully generated embedding with {Dimensions} dimensions", result.Embedding.Length);
            
            // Update dimensions dynamically
            _embeddingDimensions = result.Embedding.Length;

            return result.Embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding: {ErrorMessage}", ex.Message);

            // Fallback to a random embedding in case of error
            _logger.LogWarning("Falling back to random embedding generation");
            Random random = new();
            float[] embedding = new float[_embeddingDimensions];
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)random.NextDouble();
            }

            // Normalize the embedding
            float sum = 0;
            for (int i = 0; i < embedding.Length; i++)
            {
                sum += embedding[i] * embedding[i];
            }

            float magnitude = (float)Math.Sqrt(sum);
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= magnitude;
            }

            return embedding;
        }
    }

    public async Task<float[]> Generate(
        JsonDocument document,
        CancellationToken cancellationToken = default
    )
    {
        string jsonString = document.RootElement.ToString();
        return await Generate(jsonString, cancellationToken);
    }
    
    public int GetEmbeddingDimensions()
    {
        return _embeddingDimensions;
    }
}
