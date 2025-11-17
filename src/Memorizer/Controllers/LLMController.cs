using Memorizer.Models.OpenLlm;
using Memorizer.Settings;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Memorizer.Controllers;

/// <summary>
/// OpenAI-compatible LLM API Controller
/// Provides direct access to LLM functionality via OpenAI-compatible endpoints
/// </summary>
[ApiController]
[Route("api/llm")]
public class LLMController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<LLMController> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string DefaultModel = "openai/gpt-oss-20b";
    private const int DefaultMaxTokens = 5000;

    public LLMController(
        IHttpClientFactory httpClientFactory,
        LlmSettings settings,
        ILogger<LLMController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("LLMClient");
        _settings = settings;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Configure HTTP client
        if (_httpClient.BaseAddress == null && _settings.ApiUrl != null)
        {
            _httpClient.BaseAddress = _settings.ApiUrl;
        }

        _httpClient.Timeout = _settings.Timeout;
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Chat completion endpoint - supports both streaming and non-streaming modes
    /// </summary>
    /// <param name="request">Chat completion request following OpenAI API format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat completion response or SSE stream</returns>
    [HttpPost("chat/completions")]
    [ProducesResponseType(typeof(ApiChatCompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChatCompletions(
        [FromBody] ApiChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (request.Messages == null || request.Messages.Count == 0)
            {
                return BadRequest(new { error = "Messages array cannot be empty" });
            }

            _logger.LogInformation("Processing chat completion request: model={Model}, stream={Stream}, messages={Count}",
                request.Model, request.Stream, request.Messages.Count);

            // Ensure default values
            if (string.IsNullOrEmpty(request.Model))
            {
                request.Model = DefaultModel;
            }

            if (request.MaxTokens <= 0)
            {
                request.MaxTokens = DefaultMaxTokens;
            }

            // Handle streaming vs non-streaming
            if (request.Stream)
            {
                return await HandleStreamingChatCompletion(request, cancellationToken);
            }
            else
            {
                return await HandleNonStreamingChatCompletion(request, cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during chat completion: {Message}", ex.Message);
            return StatusCode(503, new { error = "LLM service unavailable", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat completion: {Message}", ex.Message);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Text completion endpoint
    /// </summary>
    /// <param name="request">Text completion request following OpenAI API format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Text completion response</returns>
    [HttpPost("completions")]
    [ProducesResponseType(typeof(ApiCompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Completions(
        [FromBody] ApiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (string.IsNullOrEmpty(request.Prompt))
            {
                return BadRequest(new { error = "Prompt cannot be empty" });
            }

            _logger.LogInformation("Processing text completion request: model={Model}, prompt_length={Length}",
                request.Model, request.Prompt.Length);

            // Ensure default values
            if (string.IsNullOrEmpty(request.Model))
            {
                request.Model = DefaultModel;
            }

            if (request.MaxTokens <= 0)
            {
                request.MaxTokens = DefaultMaxTokens;
            }

            // Prepare request payload
            var payload = new
            {
                model = request.Model,
                prompt = request.Prompt,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature
            };

            var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Send request to LLM service
            var response = await _httpClient.PostAsync("/v1/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var completionResponse = JsonSerializer.Deserialize<ApiCompletionResponse>(responseBody, _jsonOptions);

            _logger.LogInformation("Text completion successful: id={Id}", completionResponse?.Id);

            return Ok(completionResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during text completion: {Message}", ex.Message);
            return StatusCode(503, new { error = "LLM service unavailable", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing text completion: {Message}", ex.Message);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint for LLM service
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Health()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Performing LLM health check");

            // Simple health check with minimal token request
            var testMessages = new[]
            {
                new { role = "user", content = "Hi" }
            };

            var payload = new
            {
                model = DefaultModel,
                messages = testMessages,
                max_tokens = 10,
                temperature = 0.7
            };

            var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            stopwatch.Stop();

            _logger.LogInformation("LLM health check passed: response_time={ResponseTime}ms", stopwatch.ElapsedMilliseconds);

            return Ok(new
            {
                healthy = true,
                model = DefaultModel,
                responseTimeMs = stopwatch.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "LLM health check failed: {Message}", ex.Message);

            return StatusCode(503, new
            {
                healthy = false,
                error = ex.Message,
                responseTimeMs = stopwatch.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow
            });
        }
    }

    #region Private Helper Methods

    private async Task<IActionResult> HandleNonStreamingChatCompletion(
        ApiChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        // Prepare request payload
        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            stream = false
        };

        var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending non-streaming request to LLM service");

        // Send request to LLM service
        var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<ApiChatCompletionResponse>(responseBody, _jsonOptions);

        _logger.LogInformation("Chat completion successful: id={Id}, finish_reason={FinishReason}",
            chatResponse?.Id, chatResponse?.Choices?.FirstOrDefault()?.FinishReason);

        return Ok(chatResponse);
    }

    private async Task<IActionResult> HandleStreamingChatCompletion(
        ApiChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        // Prepare request payload
        var payload = new
        {
            model = request.Model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            stream = true
        };

        var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending streaming request to LLM service");

        // Create HttpRequestMessage for streaming
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = content
        };

        // Send request to LLM service with ResponseHeadersRead to enable streaming
        var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Set SSE headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        _logger.LogInformation("Starting SSE stream for chat completion");

        // Stream the response
        await StreamResponseToClient(response, cancellationToken);

        return new EmptyResult();
    }

    private async Task StreamResponseToClient(HttpResponseMessage upstreamResponse, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = await upstreamResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // Forward SSE data lines to client
                if (line.StartsWith("data: "))
                {
                    await Response.WriteAsync(line + "\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);

                    _logger.LogTrace("Forwarded SSE chunk: {Line}", line.Length > 100 ? line[..100] + "..." : line);

                    // Check for [DONE] marker
                    if (line.Contains("[DONE]"))
                    {
                        _logger.LogInformation("SSE stream completed with [DONE] marker");
                        break;
                    }
                }
            }

            _logger.LogInformation("SSE streaming finished");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE stream cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SSE streaming: {Message}", ex.Message);
            throw;
        }
    }

    #endregion
}
