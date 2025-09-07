using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Memorizer.Settings;

namespace Memorizer.Services;

/// <summary>
/// Service for generating embeddings using Custom internal network API
/// </summary>
public class CustomEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingSettings _settings;
    private readonly ILogger<CustomEmbeddingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Custom API specific settings
    private const string DefaultModel = "text-embedding-all-minilm-l12-v2";
    static private int EmbeddingDimensions = 384;

    public CustomEmbeddingService(
        HttpClient httpClient,
        EmbeddingSettings settings,
        ILogger<CustomEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        // Set base address if not already set
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = _settings.ApiUrl;
        }

        _httpClient.Timeout = _settings.Timeout;
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<float[]> Generate(
        string text,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating embedding for text of length {TextLength} using Custom API", text.Length);

            var embedding = await GenerateEmbeddingInternal(text, cancellationToken);

            _logger.LogDebug("Successfully generated embedding with {Dimensions} dimensions using Custom API", embedding.Length);
            
            EmbeddingDimensions = embedding.Length;

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding with Custom API: {ErrorMessage}", ex.Message);

            // Fallback to a random embedding in case of error
            _logger.LogWarning("Falling back to random embedding generation");
            return GenerateRandomEmbedding();
        }
    }

    public async Task<float[]> Generate(
        JsonDocument document,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert JsonDocument to string for embedding
            var text = document.RootElement.ToString();
            
            _logger.LogDebug("Generating embedding for JSON document using Custom API");

            return await Generate(text, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding from JSON with Custom API: {ErrorMessage}", ex.Message);
            
            // Fallback to a random embedding in case of error
            _logger.LogWarning("Falling back to random embedding generation");
            return GenerateRandomEmbedding();
        }
    }

    private async Task<float[]> GenerateEmbeddingInternal(
        string text,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            model = _settings.Model ?? DefaultModel,
            input = text
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/embeddings", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Custom API embedding request failed: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Custom API embedding request failed: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent, _jsonOptions);

        if (embeddingResponse?.Data?.FirstOrDefault()?.Embedding == null)
        {
            throw new InvalidOperationException("Invalid embedding response from Custom API");
        }

        return embeddingResponse.Data.First().Embedding!.ToArray();
    }

    /// <summary>
    /// Generates embeddings for multiple texts in a single batch request
    /// </summary>
    public async Task<List<float[]>> GenerateBatch(
        List<string> texts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating batch embeddings for {Count} texts using Custom API", texts.Count);

            var request = new
            {
                model = _settings.Model ?? DefaultModel,
                input = texts
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/embeddings", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Custom API batch embedding request failed: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Custom API batch embedding request failed: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent, _jsonOptions);

            if (embeddingResponse?.Data == null || !embeddingResponse.Data.Any())
            {
                throw new InvalidOperationException("Invalid batch embedding response from Custom API");
            }

            // Sort by index to ensure correct order
            var sortedData = embeddingResponse.Data.OrderBy(d => d.Index).ToList();
            var embeddings = sortedData.Select(d => d.Embedding?.ToArray() ?? GenerateRandomEmbedding()).ToList();

            _logger.LogDebug("Successfully generated {Count} embeddings using Custom API", embeddings.Count);

            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating batch embeddings with Custom API: {ErrorMessage}", ex.Message);
            
            // Fallback: generate random embeddings for all texts
            _logger.LogWarning("Falling back to random embedding generation for batch");
            return texts.Select(_ => GenerateRandomEmbedding()).ToList();
        }
    }

    /// <summary>
    /// Checks if the Custom API embedding service is healthy
    /// </summary>
    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Custom API embedding service health");

            // Try to list available models
            var response = await _httpClient.GetAsync("/v1/models", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(content, _jsonOptions);
                
                var modelExists = modelsResponse?.Data?.Any(m => 
                    m.Id == (_settings.Model ?? DefaultModel)) ?? false;

                if (modelExists)
                {
                    _logger.LogDebug("Custom API embedding service is healthy and model is available");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Embedding model {Model} not found in Custom API", 
                        _settings.Model ?? DefaultModel);
                    return false;
                }
            }

            _logger.LogWarning("Custom API embedding service health check failed with status: {StatusCode}", 
                response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Custom API embedding service health check failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    private static float[] GenerateRandomEmbedding()
    {
        Random random = new();
        float[] embedding = new float[EmbeddingDimensions];
        
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
    
    public int GetEmbeddingDimensions()
    {
        return EmbeddingDimensions;
    }

    // Response models for Custom API
    private class ModelsResponse
    {
        public List<ModelData>? Data { get; set; }
        public string? Object { get; set; }
    }

    private class ModelData
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public string? OwnedBy { get; set; }
    }

    private class EmbeddingResponse
    {
        public string? Object { get; set; }
        public string? Model { get; set; }
        public List<EmbeddingData>? Data { get; set; }
        public Usage? Usage { get; set; }
    }

    private class EmbeddingData
    {
        public string? Object { get; set; }
        public List<float>? Embedding { get; set; }
        public int Index { get; set; }
    }

    private class Usage
    {
        public int PromptTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}