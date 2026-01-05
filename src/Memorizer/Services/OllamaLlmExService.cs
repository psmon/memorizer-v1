using System.Text;
using Memorizer.Settings;
using OllamaSharp;

namespace Memorizer.Services;

/// <summary>
/// Service for LLM-EX (Extended) using Ollama
/// Provides deep analysis capabilities using Ollama models
/// </summary>
public sealed class OllamaLlmExService : ILlmExService
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly LlmExSettings _settings;
    private readonly ILogger<OllamaLlmExService> _logger;

    public OllamaLlmExService(
        HttpClient httpClient,
        LlmExSettings settings,
        ILogger<OllamaLlmExService> logger)
    {
        _settings = settings;
        _logger = logger;

        // Create OllamaSharp client with the configured HttpClient
        _ollamaClient = new OllamaApiClient(httpClient)
        {
            SelectedModel = _settings.Model
        };
    }

    public async Task<LlmHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Checking LLM-EX (Ollama) health for model {Model} at {ApiUrl}",
                _settings.Model, _settings.ApiUrl);

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
                _logger.LogDebug("LLM-EX (Ollama) health check successful in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                return new LlmHealthResult
                {
                    IsHealthy = true,
                    Message = "LLM-EX (Ollama) service is available and responding",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed
                };
            }
            else
            {
                _logger.LogWarning("LLM-EX (Ollama) health check returned null response");
                return new LlmHealthResult
                {
                    IsHealthy = false,
                    Message = "LLM-EX (Ollama) service returned empty response",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed,
                    ErrorDetails = "Null response from LLM-EX service"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "LLM-EX (Ollama) health check failed - connection issue: {Error}", ex.Message);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = $"Cannot connect to LLM-EX (Ollama) service at {_settings.ApiUrl}",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = ex.Message
            };
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "LLM-EX (Ollama) health check timed out after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "LLM-EX (Ollama) service request timed out",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = $"Request timed out after {stopwatch.Elapsed.TotalSeconds:F1} seconds"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LLM-EX (Ollama) health check failed with unexpected error: {Error}", ex.Message);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "LLM-EX (Ollama) service health check failed",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending completion request to LLM-EX (Ollama) model {Model}", _settings.Model);

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
                throw new InvalidOperationException("Empty response from LLM-EX (Ollama) service");
            }

            _logger.LogDebug("Received completion response with length {Length}", response.Length);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating completion from LLM-EX (Ollama)");
            throw;
        }
    }

    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending streaming completion request to LLM-EX (Ollama) model {Model}", _settings.Model);

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

    public void Dispose()
    {
        _ollamaClient?.Dispose();
    }
}
