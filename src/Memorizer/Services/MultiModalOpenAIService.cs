using Memorizer.Settings;
using OpenAI;
using OpenAI.Chat;

namespace Memorizer.Services;

/// <summary>
/// Service for Multi-modal LLM that can process both images and text
/// Uses OpenAI SDK with API key authentication
/// </summary>
public sealed class MultiModalOpenAIService : IMultiModalService
{
    private readonly OpenAIClient _openAIClient;
    private readonly ChatClient _chatClient;
    private readonly MultiModalSettings _settings;
    private readonly ILogger<MultiModalOpenAIService> _logger;

    public MultiModalOpenAIService(
        MultiModalSettings settings,
        ILogger<MultiModalOpenAIService> logger)
    {
        _settings = settings;
        _logger = logger;

        // Create OpenAI client with API key
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new ArgumentException("API key is required for OpenAI MultiModal service");
        }

        _openAIClient = new OpenAIClient(_settings.ApiKey);
        _chatClient = _openAIClient.GetChatClient(_settings.Model);
    }

    public async Task<string> AnalyzeImageAsync(
        byte[] imageData,
        string prompt,
        string imageFormat = "jpeg",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Analyzing image with OpenAI multi-modal model {Model}", _settings.Model);

            // Convert image to base64 data URI
            var base64Image = Convert.ToBase64String(imageData);
            var mimeType = GetMimeType(imageFormat);
            var imageUri = new BinaryData(imageData);

            // Build messages with multi-modal content
            var contentParts = new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart(prompt),
                ChatMessageContentPart.CreateImagePart(imageUri, mimeType)
            };

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateUserMessage(contentParts)
            };

            var chatRequest = new ChatCompletionOptions
            {
                Temperature = (float)_settings.Temperature,
                MaxOutputTokenCount = _settings.MaxTokens
            };

            var response = await _chatClient.CompleteChatAsync(messages, chatRequest, cancellationToken);

            if (response?.Value?.Content == null || response.Value.Content.Count == 0)
            {
                throw new InvalidOperationException("Empty response from OpenAI multi-modal service");
            }

            var responseText = response.Value.Content[0].Text;

            if (string.IsNullOrEmpty(responseText))
            {
                throw new InvalidOperationException("Empty response text from OpenAI multi-modal service");
            }

            _logger.LogDebug("OpenAI multi-modal analysis complete. Response length: {Length}", responseText.Length);

            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OpenAI multi-modal image analysis: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async Task<MultiModalHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Checking OpenAI multi-modal health for model {Model}", _settings.Model);

            // Try a simple test request to verify connectivity and model availability
            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateUserMessage("Test")
            };

            var chatRequest = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 1
            };

            var response = await _chatClient.CompleteChatAsync(messages, chatRequest, cancellationToken);

            stopwatch.Stop();

            if (response?.Value != null)
            {
                _logger.LogDebug("OpenAI multi-modal health check successful in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return new MultiModalHealthResult
                {
                    IsHealthy = true,
                    Message = "OpenAI multi-modal service is available and responding",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed
                };
            }

            return new MultiModalHealthResult
            {
                IsHealthy = false,
                Message = "OpenAI multi-modal service returned empty response",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = "Null response from OpenAI service"
            };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "OpenAI multi-modal health check failed - connection issue: {Error}", ex.Message);
            return new MultiModalHealthResult
            {
                IsHealthy = false,
                Message = "Cannot connect to OpenAI multi-modal service",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = ex.Message
            };
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "OpenAI multi-modal health check timed out after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return new MultiModalHealthResult
            {
                IsHealthy = false,
                Message = "OpenAI multi-modal service request timed out",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = $"Request timed out after {stopwatch.Elapsed.TotalSeconds:F1} seconds"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "OpenAI multi-modal health check failed with unexpected error: {Error}", ex.Message);
            return new MultiModalHealthResult
            {
                IsHealthy = false,
                Message = "OpenAI multi-modal service health check failed",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = ex.Message
            };
        }
    }

    public void Dispose()
    {
        // OpenAI client doesn't need explicit disposal
    }

    private static string GetMimeType(string imageFormat)
    {
        return imageFormat.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "webp" => "image/webp",
            _ => $"image/{imageFormat}"
        };
    }
}
