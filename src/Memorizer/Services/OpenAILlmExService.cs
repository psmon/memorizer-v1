using System.ClientModel;
using Memorizer.Prompts;
using Memorizer.Settings;
using OpenAI;
using OpenAI.Chat;

namespace Memorizer.Services;

/// <summary>
/// Service for LLM-EX (Extended) using OpenAI API
/// Provides deep analysis capabilities using OpenAI models (e.g., gpt-4o, gpt-4-turbo)
/// </summary>
public sealed class OpenAILlmExService : ILlmExService
{
    private readonly OpenAIClient _openAIClient;
    private readonly ChatClient _chatClient;
    private readonly LlmExSettings _settings;
    private readonly ILogger<OpenAILlmExService> _logger;

    private const int DefaultMaxTokens = 8000;

    public OpenAILlmExService(
        LlmExSettings settings,
        ILogger<OpenAILlmExService> logger)
    {
        _settings = settings;
        _logger = logger;

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API Key is required for LLM-EX OpenAI service. " +
                "Set the MEMORIZER_LLM-EX__ApiKey environment variable.");
        }

        // Create OpenAI client with API key
        _openAIClient = new OpenAIClient(_settings.ApiKey);

        // Get the chat client for the specified model
        _chatClient = _openAIClient.GetChatClient(_settings.Model);
    }

    public async Task<LlmHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Checking LLM-EX (OpenAI) health for model {Model}", _settings.Model);

            // Try a simple test request to verify connectivity and model availability
            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(PromptTemplates.HealthCheckSystemMessage),
                ChatMessage.CreateUserMessage("Test")
            };

            var chatRequest = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 1,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await _chatClient.CompleteChatAsync(messages, chatRequest, cancellationToken);

            stopwatch.Stop();

            if (response?.Value != null)
            {
                _logger.LogDebug("LLM-EX (OpenAI) health check successful in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                return new LlmHealthResult
                {
                    IsHealthy = true,
                    Message = "LLM-EX (OpenAI) service is available and responding",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed
                };
            }
            else
            {
                _logger.LogWarning("LLM-EX (OpenAI) health check returned null response");
                return new LlmHealthResult
                {
                    IsHealthy = false,
                    Message = "LLM-EX (OpenAI) service returned empty response",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed,
                    ErrorDetails = "Null response from LLM-EX OpenAI service"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "LLM-EX (OpenAI) health check failed - connection issue: {Error}", ex.Message);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "Cannot connect to LLM-EX (OpenAI) service",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = ex.Message
            };
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "LLM-EX (OpenAI) health check timed out after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "LLM-EX (OpenAI) service request timed out",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = $"Request timed out after {stopwatch.Elapsed.TotalSeconds:F1} seconds"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LLM-EX (OpenAI) health check failed with unexpected error: {Error}", ex.Message);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "LLM-EX (OpenAI) service health check failed",
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
            _logger.LogDebug("Sending completion request to LLM-EX (OpenAI) model {Model}", _settings.Model);

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateUserMessage(prompt)
            };

            var chatRequest = new ChatCompletionOptions
            {
                // Note: Temperature is not set to support reasoning models (o1, o3) which only allow default value
                MaxOutputTokenCount = DefaultMaxTokens
            };

            var response = await _chatClient.CompleteChatAsync(messages, chatRequest, cancellationToken);

            if (response?.Value?.Content == null || response.Value.Content.Count == 0)
            {
                throw new InvalidOperationException("Empty response from LLM-EX (OpenAI) service");
            }

            var responseText = response.Value.Content[0].Text;

            if (string.IsNullOrEmpty(responseText))
            {
                throw new InvalidOperationException("Empty response text from LLM-EX (OpenAI) service");
            }

            _logger.LogDebug("Received completion response with length {Length}", responseText.Length);
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating completion from LLM-EX (OpenAI)");
            throw;
        }
    }

    public async IAsyncEnumerable<string> CompleteStreamingAsync(
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending streaming completion request to LLM-EX (OpenAI) model {Model}", _settings.Model);

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateUserMessage(prompt)
        };

        var chatRequest = new ChatCompletionOptions
        {
            // Note: Temperature is not set to support reasoning models (o1, o3) which only allow default value
            MaxOutputTokenCount = DefaultMaxTokens
        };

        AsyncCollectionResult<StreamingChatCompletionUpdate> streamingUpdates =
            _chatClient.CompleteChatStreamingAsync(messages, chatRequest, cancellationToken);

        await foreach (var update in streamingUpdates.WithCancellation(cancellationToken))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentPart.Text))
                {
                    yield return contentPart.Text;
                }
            }
        }
    }

    public void Dispose()
    {
        // OpenAI client doesn't need explicit disposal
    }
}
