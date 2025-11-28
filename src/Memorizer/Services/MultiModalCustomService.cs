using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Memorizer.Settings;

namespace Memorizer.Services;

/// <summary>
/// Service for Multi-modal LLM that can process both images and text
/// Compatible with OpenAI API format (LM Studio, etc.)
/// Uses custom HTTP-based implementation without API key
/// </summary>
public sealed class MultiModalCustomService : IMultiModalService
{
    private readonly HttpClient _httpClient;
    private readonly MultiModalSettings _settings;
    private readonly ILogger<MultiModalCustomService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultiModalCustomService(
        HttpClient httpClient,
        MultiModalSettings settings,
        ILogger<MultiModalCustomService> logger)
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

    public async Task<string> AnalyzeImageAsync(
        byte[] imageData,
        string prompt,
        string imageFormat = "jpeg",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Analyzing image with multi-modal model {Model}", _settings.Model);

            // Convert image to base64 data URI
            var base64Image = Convert.ToBase64String(imageData);
            var dataUri = $"data:image/{imageFormat};base64,{base64Image}";

            // Build messages with multi-modal content
            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = new { url = dataUri } }
                    }
                }
            };

            var request = new
            {
                model = _settings.Model,
                messages = messages,
                temperature = _settings.Temperature,
                max_tokens = _settings.MaxTokens
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Multi-modal API request failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Multi-modal API request failed: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions);

            if (chatResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
            {
                throw new InvalidOperationException("Invalid response from Multi-modal API");
            }

            var result = chatResponse.Choices.First().Message!.Content!;
            _logger.LogDebug("Multi-modal analysis complete. Response length: {Length}", result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during multi-modal image analysis: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async Task<MultiModalHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Checking Multi-modal API health for model {Model}", _settings.Model);

            // Try to list available models
            var response = await _httpClient.GetAsync("/v1/models", cancellationToken);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(content, _jsonOptions);

                var modelExists = modelsResponse?.Data?.Any(m =>
                    m.Id == _settings.Model) ?? false;

                _logger.LogDebug("Multi-modal API health check successful in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return new MultiModalHealthResult
                {
                    IsHealthy = modelExists,
                    Message = modelExists
                        ? "Multi-modal API is healthy and model is available"
                        : $"Model {_settings.Model} not found",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed
                };
            }

            return new MultiModalHealthResult
            {
                IsHealthy = false,
                Message = $"Multi-modal API returned status code: {response.StatusCode}",
                ErrorDetails = $"Status: {response.StatusCode}",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Multi-modal API health check failed: {ErrorMessage}", ex.Message);

            return new MultiModalHealthResult
            {
                IsHealthy = false,
                Message = "Failed to connect to Multi-modal API",
                ErrorDetails = ex.Message,
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    public void Dispose()
    {
        // HttpClient is managed by the factory, don't dispose it
    }

    // Response models for OpenAI-compatible API
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

    private class ChatCompletionResponse
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public long? Created { get; set; }
        public string? Model { get; set; }
        public List<Choice>? Choices { get; set; }
        public Usage? Usage { get; set; }
    }

    private class Choice
    {
        public int Index { get; set; }
        public Message? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    private class Message
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
