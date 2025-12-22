using System.Text;
using System.Text.Json;
using Memorizer.Models;
using Memorizer.Prompts;
using Memorizer.Settings;
using OllamaSharp;

namespace Memorizer.Services;

/// <summary>
/// Service for LLM-based text analysis using OllamaSharp
/// </summary>
public sealed class OllamaLlmService : ILlmService
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<OllamaLlmService> _logger;

    public OllamaLlmService(
        HttpClient httpClient,
        LlmSettings settings,
        ILogger<OllamaLlmService> logger)
    {
        _settings = settings;
        _logger = logger;
        
        // Create OllamaSharp client with the configured HttpClient
        _ollamaClient = new OllamaApiClient(httpClient)
        {
            SelectedModel = _settings.Model
        };
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
            
            _logger.LogDebug("Sending title generation request to LLM model {Model}", _settings.Model);
            
            var response = await SendLlmRequest(prompt, cancellationToken);
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

    public async Task<LlmHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Checking LLM health for model {Model} at {ApiUrl}", _settings.Model, _settings.ApiUrl);

            // Try a simple test request to verify connectivity and model availability
            var testRequest = new OllamaSharp.Models.GenerateRequest
            {
                Model = _settings.Model,
                Prompt = "Test",
                Stream = false,
                Options = new OllamaSharp.Models.RequestOptions
                {
                    NumPredict = 1 // Only generate 1 token for quick test
                }
            };

            // For health check, we just need to know if the service responds
            var responseStream = _ollamaClient.GenerateAsync(testRequest, cancellationToken);
            OllamaSharp.Models.GenerateResponseStream? firstResponse = null;
            
            await foreach (var chunk in responseStream)
            {
                firstResponse = chunk;
                break; // Just need the first response to confirm connectivity
            }
            
            stopwatch.Stop();

            if (firstResponse != null)
            {
                _logger.LogDebug("LLM health check successful in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return new LlmHealthResult
                {
                    IsHealthy = true,
                    Message = $"LLM service is available and responding",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed
                };
            }
            else
            {
                _logger.LogWarning("LLM health check returned null response");
                return new LlmHealthResult
                {
                    IsHealthy = false,
                    Message = "LLM service returned empty response",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed,
                    ErrorDetails = "Null response from LLM service"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "LLM health check failed - connection issue: {Error}", ex.Message);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = $"Cannot connect to LLM service at {_settings.ApiUrl}",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = ex.Message
            };
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "LLM health check timed out after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "LLM service request timed out",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = $"Request timed out after {stopwatch.Elapsed.TotalSeconds:F1} seconds"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LLM health check failed with unexpected error: {Error}", ex.Message);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "LLM service health check failed",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = ex.Message
            };
        }
    }

    private async Task<string> SendLlmRequest(string prompt, CancellationToken cancellationToken = default)
    {
        // Use OllamaSharp simple API to generate completion
        var request = new OllamaSharp.Models.GenerateRequest
        {
            Model = _settings.Model,
            Prompt = prompt,
            Stream = true,
            Format = "json"
        };
        
        var responseStream = _ollamaClient.GenerateAsync(request, cancellationToken);
        
        var responseBuilder = new StringBuilder();
        await foreach (var responseChunk in responseStream)
        {
            responseBuilder.Append(responseChunk?.Response);
        }
        
        var response = responseBuilder.ToString();

        if (string.IsNullOrEmpty(response))
        {
            throw new InvalidOperationException("Empty response from LLM service");
        }

        return response;
    }


    private static string ParseTitleResponse(string response, int maxTitleLength)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(response);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("title", out var titleProp))
            {
                var title = titleProp.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    // Ensure title doesn't exceed max length
                    if (title.Length > maxTitleLength)
                    {
                        title = title[..(maxTitleLength - 3)] + "...";
                    }
                    return title;
                }
            }

            throw new InvalidOperationException("No valid title found in LLM response");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse LLM title response: {ex.Message}", ex);
        }
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending completion request to LLM model {Model}", _settings.Model);

            // Use OllamaSharp API to generate completion without JSON format requirement
            var request = new OllamaSharp.Models.GenerateRequest
            {
                Model = _settings.Model,
                Prompt = prompt,
                Stream = true
            };

            var responseStream = _ollamaClient.GenerateAsync(request, cancellationToken);

            var responseBuilder = new StringBuilder();
            await foreach (var responseChunk in responseStream)
            {
                responseBuilder.Append(responseChunk?.Response);
            }

            var response = responseBuilder.ToString();

            if (string.IsNullOrEmpty(response))
            {
                throw new InvalidOperationException("Empty response from LLM service");
            }

            _logger.LogDebug("Received completion response with length {Length}", response.Length);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating completion from LLM");
            throw;
        }
    }

    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending streaming completion request to LLM model {Model}", _settings.Model);

        var request = new OllamaSharp.Models.GenerateRequest
        {
            Model = _settings.Model,
            Prompt = prompt,
            Stream = true
        };

        var responseStream = _ollamaClient.GenerateAsync(request, cancellationToken);

        await foreach (var responseChunk in responseStream.WithCancellation(cancellationToken))
        {
            if (!string.IsNullOrEmpty(responseChunk?.Response))
            {
                yield return responseChunk.Response;
            }
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
            
            _logger.LogDebug("Sending keyword extraction request to LLM model {Model}", _settings.Model);
            
            var response = await SendLlmRequest(prompt, cancellationToken);
            var keywords = ParseKeywordsResponse(response);
            
            _logger.LogDebug("Keyword extraction complete: {Count} keywords", keywords.Count);

            return keywords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during keyword extraction: {ErrorMessage}", ex.Message);
            
            // Fallback: return empty list
            return new List<string>();
        }
    }


    private static List<string> ParseKeywordsResponse(string response)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(response);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("keywords", out var keywordsProp) && keywordsProp.ValueKind == JsonValueKind.Array)
            {
                var keywords = new List<string>();
                foreach (var keyword in keywordsProp.EnumerateArray())
                {
                    var keywordStr = keyword.GetString()?.Trim().ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(keywordStr))
                    {
                        keywords.Add(keywordStr);
                    }
                }
                return keywords;
            }

            throw new InvalidOperationException("No valid keywords found in LLM response");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse LLM keywords response: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _ollamaClient?.Dispose();
    }
} 