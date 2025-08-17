using System.ClientModel;
using System.Text;
using System.Text.Json;
using Memorizer.Models;
using Memorizer.Settings;
using OpenAI;
using OpenAI.Chat;

namespace Memorizer.Services;

/// <summary>
/// Service for LLM-based text analysis using OpenAI API
/// </summary>
public sealed class OpenAILlmService : ILlmService
{
    private readonly OpenAIClient _openAIClient;
    private readonly ChatClient _chatClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<OpenAILlmService> _logger;

    public OpenAILlmService(
        LlmSettings settings,
        ILogger<OpenAILlmService> logger)
    {
        _settings = settings;
        _logger = logger;
        
        // Create OpenAI client with API key
        _openAIClient = new OpenAIClient(_settings.ApiKey);
        
        // Get the chat client for the specified model
        _chatClient = _openAIClient.GetChatClient(_settings.Model);
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

            var prompt = CreateTitleGenerationPrompt(content, contentType, existingTags, maxTitleLength);
            
            _logger.LogDebug("Sending title generation request to OpenAI model {Model}", _settings.Model);
            
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
            _logger.LogDebug("Checking OpenAI health for model {Model}", _settings.Model);

            // Try a simple test request to verify connectivity and model availability
            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage("You are a helpful assistant."),
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
                _logger.LogDebug("OpenAI health check successful in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return new LlmHealthResult
                {
                    IsHealthy = true,
                    Message = "OpenAI service is available and responding",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed
                };
            }
            else
            {
                _logger.LogWarning("OpenAI health check returned null response");
                return new LlmHealthResult
                {
                    IsHealthy = false,
                    Message = "OpenAI service returned empty response",
                    ModelName = _settings.Model,
                    ResponseTime = stopwatch.Elapsed,
                    ErrorDetails = "Null response from OpenAI service"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "OpenAI health check failed - connection issue: {Error}", ex.Message);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = $"Cannot connect to OpenAI service",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = ex.Message
            };
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "OpenAI health check timed out after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "OpenAI service request timed out",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = $"Request timed out after {stopwatch.Elapsed.TotalSeconds:F1} seconds"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "OpenAI health check failed with unexpected error: {Error}", ex.Message);
            return new LlmHealthResult
            {
                IsHealthy = false,
                Message = "OpenAI service health check failed",
                ModelName = _settings.Model,
                ResponseTime = stopwatch.Elapsed,
                ErrorDetails = ex.Message
            };
        }
    }

    private async Task<string> SendLlmRequest(string prompt, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage("You are an expert at creating concise, descriptive titles for various types of content."),
            ChatMessage.CreateUserMessage(prompt)
        };

        var chatRequest = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            Temperature = 0.7f,
            MaxOutputTokenCount = 150
        };

        var response = await _chatClient.CompleteChatAsync(messages, chatRequest, cancellationToken);

        if (response?.Value?.Content == null || response.Value.Content.Count == 0)
        {
            throw new InvalidOperationException("Empty response from OpenAI service");
        }

        var responseText = response.Value.Content[0].Text;
        
        if (string.IsNullOrEmpty(responseText))
        {
            throw new InvalidOperationException("Empty response text from OpenAI service");
        }

        return responseText;
    }

    private static string CreateTitleGenerationPrompt(
        string content,
        string contentType,
        string[]? existingTags,
        int maxTitleLength)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("TASK: Generate a clear, descriptive title for the provided content that captures its main topic and purpose.");
        prompt.AppendLine();
        prompt.AppendLine("GUIDELINES:");
        prompt.AppendLine($"- Maximum title length: {maxTitleLength} characters");
        prompt.AppendLine("- Make it descriptive and searchable");
        prompt.AppendLine("- Capture the main topic or purpose");
        prompt.AppendLine("- Use natural language, avoid generic phrases");
        prompt.AppendLine("- Consider the content type and existing tags for context");
        prompt.AppendLine();
        prompt.AppendLine("CONTENT DETAILS:");
        prompt.AppendLine($"- Type: {contentType}");
        if (existingTags?.Length > 0)
        {
            prompt.AppendLine($"- Tags: {string.Join(", ", existingTags)}");
        }
        prompt.AppendLine($"- Length: {content.Length} characters");
        prompt.AppendLine();
        prompt.AppendLine("CONTENT TO ANALYZE:");
        prompt.AppendLine("```");
        // Truncate content if it's very long to avoid token limits
        var truncatedContent = content.Length > 2000 ? content[..2000] + "..." : content;
        prompt.AppendLine(truncatedContent);
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("RESPOND WITH VALID JSON in this exact format:");
        prompt.AppendLine("""
        {
          "title": "Generated title here",
          "reasoning": "Brief explanation of why this title was chosen"
        }
        """);

        return prompt.ToString();
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

            throw new InvalidOperationException("No valid title found in OpenAI response");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse OpenAI title response: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        // OpenAI client doesn't need explicit disposal
    }
}