using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Memorizer.Settings;

namespace Memorizer.Services;

/// <summary>
/// Service for LLM-EX (Extended) using Custom internal network API
/// Uses higher capacity model (120B) for complex deep analysis tasks
/// </summary>
public sealed class CustomLlmExService : ILlmExService
{
    private readonly HttpClient _httpClient;
    private readonly LlmExSettings _settings;
    private readonly ILogger<CustomLlmExService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private const int DefaultMaxTokens = 8000;

    public CustomLlmExService(
        HttpClient httpClient,
        LlmExSettings settings,
        ILogger<CustomLlmExService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = _settings.ApiUrl;
        }

        _httpClient.Timeout = _settings.Timeout;
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<LlmHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Checking LLM-EX API health for model {Model}", _settings.Model);

            var response = await _httpClient.GetAsync("/v1/models", cancellationToken);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(content, _jsonOptions);

                var modelExists = modelsResponse?.Data?.Any(m =>
                    m.Id == _settings.Model) ?? false;

                _logger.LogDebug("LLM-EX API health check successful in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return new LlmHealthResult
                {
                    IsHealthy = modelExists,
                    Message = modelExists
                        ? "LLM-EX API is healthy and model is available"
                        : $"Model {_settings.Model} not found",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed
                };
            }

            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = $"LLM-EX API returned status code: {response.StatusCode}",
                ErrorDetails = $"Status: {response.StatusCode}",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LLM-EX API health check failed: {ErrorMessage}", ex.Message);

            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "Failed to connect to LLM-EX API",
                ErrorDetails = ex.Message,
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending completion request to LLM-EX API");

            var messages = new List<object>
            {
                new { role = "user", content = prompt }
            };

            var request = new
            {
                model = _settings.Model,
                messages = messages,
                max_tokens = DefaultMaxTokens,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("LLM-EX API request failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"LLM-EX API request failed: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions);

            if (chatResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
            {
                throw new InvalidOperationException("Invalid response from LLM-EX API");
            }

            return chatResponse.Choices.First().Message!.Content!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM-EX completion: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending streaming completion request to LLM-EX API");

        var messages = new List<object>
        {
            new { role = "user", content = prompt }
        };

        var request = new
        {
            model = _settings.Model,
            messages = messages,
            max_tokens = DefaultMaxTokens,
            temperature = 0.7,
            stream = true
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("LLM-EX API streaming request failed: {StatusCode} - {Error}",
                response.StatusCode, errorContent);
            throw new HttpRequestException($"LLM-EX API request failed: {response.StatusCode}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line.Substring(6);
            if (data == "[DONE]") break;

            string? delta = null;
            try
            {
                var chunk = JsonSerializer.Deserialize<StreamingChunk>(data, _jsonOptions);
                delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            }
            catch (JsonException)
            {
                // Ignore parsing errors for individual chunks
            }

            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta;
            }
        }
    }

    public void Dispose()
    {
        // HttpClient is managed by the factory, don't dispose it
    }

    // Response models
    private class ModelsResponse
    {
        public List<ModelData>? Data { get; set; }
    }

    private class ModelData
    {
        public string? Id { get; set; }
    }

    private class ChatCompletionResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }

    private class StreamingChunk
    {
        public List<StreamingChoice>? Choices { get; set; }
    }

    private class StreamingChoice
    {
        public StreamingDelta? Delta { get; set; }
    }

    private class StreamingDelta
    {
        public string? Content { get; set; }
    }
}
