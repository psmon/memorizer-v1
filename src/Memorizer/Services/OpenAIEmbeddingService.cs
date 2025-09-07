using System.Text.Json;
using Memorizer.Settings;
using OpenAI;
using OpenAI.Embeddings;

namespace Memorizer.Services;

/// <summary>
/// Service for generating embeddings using OpenAI API
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly OpenAIClient _openAIClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly EmbeddingSettings _settings;
    private readonly ILogger<OpenAIEmbeddingService> _logger;
    private int _embeddingDimensions = 1536; // Default for OpenAI, will be updated dynamically

    public OpenAIEmbeddingService(
        EmbeddingSettings settings,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _settings = settings;
        _logger = logger;
        
        // Create OpenAI client with API key
        _openAIClient = new OpenAIClient(_settings.ApiKey);
        
        // Get the embedding client for the specified model
        // Default to text-embedding-3-small if not specified
        var modelName = string.IsNullOrEmpty(_settings.Model) ? "text-embedding-3-small" : _settings.Model;
        _embeddingClient = _openAIClient.GetEmbeddingClient(modelName);
    }

    public async Task<float[]> Generate(
        string text,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating embedding for text of length {TextLength} using OpenAI", text.Length);

            // Generate embedding using OpenAI API
            var response = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
            
            if (response?.Value == null)
            {
                throw new Exception("Failed to generate embedding: Empty response from OpenAI API");
            }

            // Convert ReadOnlyMemory<float> to float[]
            var embedding = response.Value.ToFloats().ToArray();

            _logger.LogDebug("Successfully generated embedding with {Dimensions} dimensions using OpenAI", embedding.Length);
            
            // Update dimensions dynamically
            _embeddingDimensions = embedding.Length;

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding with OpenAI: {ErrorMessage}", ex.Message);

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
        CancellationToken cancellationToken = default)
    {
        string jsonString = document.RootElement.ToString();
        return await Generate(jsonString, cancellationToken);
    }
    
    public int GetEmbeddingDimensions()
    {
        return _embeddingDimensions;
    }
}