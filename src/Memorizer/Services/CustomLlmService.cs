using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Memorizer.Models;
using Memorizer.Prompts;
using Memorizer.Settings;

namespace Memorizer.Services;

/// <summary>
/// Service for LLM-based text analysis using Custom internal network API
/// </summary>
public sealed class CustomLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<CustomLlmService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Custom API specific settings
    private const string DefaultModel = "openai/gpt-oss-20b";
    private const int DefaultMaxTokens = 5000;

    public CustomLlmService(
        HttpClient httpClient,
        LlmSettings settings,
        ILogger<CustomLlmService> logger)
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

    public async Task<string> GenerateTitle(
        string content,
        string contentType,
        string[]? existingTags = null,
        int maxTitleLength = 80,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating title for content: length={Length}, type={Type}", 
                content.Length, contentType);

            var prompt = PromptTemplates.CreateTitleGenerationPrompt(content, contentType, existingTags, maxTitleLength);
            
            _logger.LogDebug("Sending title generation request to Custom API model {Model}", 
                _settings.Model ?? DefaultModel);
            
            var response = await SendChatRequest(
                PromptTemplates.TitleGenerationSystemMessage, 
                prompt, 
                cancellationToken);

            var title = ParseTitleResponse(response, maxTitleLength);
            
            _logger.LogDebug("Title generation complete: '{Title}'", title);

            return title;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during title generation: {ErrorMessage}", ex.Message);
            
            // Fallback: create a simple title
            var fallbackTitle = $"{contentType} - {DateTime.UtcNow:yyyy-MM-dd}";
            if (fallbackTitle.Length > maxTitleLength)
            {
                fallbackTitle = fallbackTitle[..(maxTitleLength - 3)] + "...";
            }
            
            return fallbackTitle;
        }
    }

    public async Task<List<string>> ExtractKeywordsAsync(
        string content,
        string contentType,
        int maxKeywords = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Extracting keywords from content: length={Length}, type={Type}", 
                content.Length, contentType);

            var prompt = PromptTemplates.CreateKeywordExtractionPrompt(content, contentType, maxKeywords);
            
            _logger.LogDebug("Sending keyword extraction request to Custom API model {Model}", 
                _settings.Model ?? DefaultModel);

            var response = await SendChatRequest(
                PromptTemplates.KeywordExtractionSystemMessage,
                prompt,
                cancellationToken);

            var keywords = ParseKeywordsResponse(response);
            
            _logger.LogDebug("Keyword extraction complete: {Count} keywords extracted", keywords.Count);

            return keywords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during keyword extraction: {ErrorMessage}", ex.Message);
            return new List<string>();
        }
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending completion request to Custom API");

            var response = await SendChatRequest(null, prompt, cancellationToken);
            
            _logger.LogDebug("Completion request successful");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during completion: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async Task<LlmHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Checking Custom API health for model {Model}", _settings.Model ?? DefaultModel);

            // Try to list available models
            var response = await _httpClient.GetAsync("/v1/models", cancellationToken);
            
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(content, _jsonOptions);
                
                var modelExists = modelsResponse?.Data?.Any(m => 
                    m.Id == (_settings.Model ?? DefaultModel)) ?? false;

                _logger.LogDebug("Custom API health check successful in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return new LlmHealthResult
                {
                    IsHealthy = modelExists,
                    Message = modelExists 
                        ? "Custom API is healthy and model is available" 
                        : $"Model {_settings.Model ?? DefaultModel} not found",
                    ModelName = _settings.Model ?? DefaultModel,
                    ResponseTime = stopwatch.Elapsed
                };
            }

            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = $"Custom API returned status code: {response.StatusCode}",
                ErrorDetails = $"Status: {response.StatusCode}",
                ResponseTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Custom API health check failed: {ErrorMessage}", ex.Message);

            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "Failed to connect to Custom API",
                ErrorDetails = ex.Message,
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    private async Task<string> SendChatRequest(
        string? systemMessage,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var messages = new List<object>();
        
        if (!string.IsNullOrEmpty(systemMessage))
        {
            messages.Add(new { role = "system", content = systemMessage });
        }
        
        messages.Add(new { role = "user", content = userMessage });

        var request = new
        {
            model = _settings.Model ?? DefaultModel,
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
            _logger.LogError("Custom API request failed: {StatusCode} - {Error}", 
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Custom API request failed: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions);

        if (chatResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
        {
            throw new InvalidOperationException("Invalid response from Custom API");
        }

        return chatResponse.Choices.First().Message!.Content!;
    }

    private static string ParseTitleResponse(string response, int maxTitleLength)
    {
        try
        {
            // Try to parse as JSON first
            var jsonDoc = JsonDocument.Parse(response);
            if (jsonDoc.RootElement.TryGetProperty("title", out var titleElement))
            {
                var title = titleElement.GetString() ?? "Untitled";
                
                // Ensure title doesn't exceed max length
                if (title.Length > maxTitleLength)
                {
                    title = title[..(maxTitleLength - 3)] + "...";
                }
                
                return title;
            }
        }
        catch (JsonException)
        {
            // If not valid JSON, treat the response as plain text
        }

        // Fallback: use first line of response
        var firstLine = response.Split('\n')[0].Trim();
        if (firstLine.Length > maxTitleLength)
        {
            firstLine = firstLine[..(maxTitleLength - 3)] + "...";
        }
        
        return string.IsNullOrWhiteSpace(firstLine) ? "Untitled" : firstLine;
    }

    private static List<string> ParseKeywordsResponse(string response)
    {
        try
        {
            // Try to parse as JSON first
            var jsonDoc = JsonDocument.Parse(response);
            if (jsonDoc.RootElement.TryGetProperty("keywords", out var keywordsElement) && 
                keywordsElement.ValueKind == JsonValueKind.Array)
            {
                var keywords = new List<string>();
                foreach (var element in keywordsElement.EnumerateArray())
                {
                    var keyword = element.GetString();
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        keywords.Add(keyword.ToLowerInvariant());
                    }
                }
                return keywords;
            }
        }
        catch (JsonException)
        {
            // If not valid JSON, try to extract keywords from plain text
        }

        // Fallback: split by common delimiters
        var delimiters = new[] { ',', ';', '\n' };
        var parts = response.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        
        return parts
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.ToLowerInvariant())
            .Take(10)
            .ToList();
    }

    public void Dispose()
    {
        // HttpClient is managed by the factory, don't dispose it
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
        public string? Reasoning { get; set; }
    }

    private class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}