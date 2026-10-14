using System.Net;
using System.Text;
using Memorizer.Services;
using Memorizer.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace Memorizer.IntegrationTests.Services;

/// <summary>
/// Custom (LM Studio / OpenAI-compatible) providers: API key is sent as a Bearer token,
/// and embedding failures surface as exceptions instead of random vectors.
/// </summary>
public sealed class CustomProviderAuthTests
{
    private const string ApiUrl = "http://lmstudio.test:1234";
    private const string ApiKey = "sk-lm-test-token";

    [Fact]
    public async Task CustomEmbedding_WithApiKey_SendsBearerToken()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(req =>
        {
            captured = req;
            return Json(HttpStatusCode.OK, EmbeddingBody(768));
        });

        var service = new CustomEmbeddingService(new HttpClient(handler), EmbeddingSettings(ApiKey),
            Mock.Of<ILogger<CustomEmbeddingService>>());

        var embedding = await service.Generate("hello");

        Assert.Equal(768, embedding.Length);
        Assert.NotNull(captured);
        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.Equal(ApiKey, captured.Headers.Authorization?.Parameter);
        Assert.Equal("/v1/embeddings", captured.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CustomEmbedding_WithoutApiKey_SendsNoAuthorizationHeader()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(req =>
        {
            captured = req;
            return Json(HttpStatusCode.OK, EmbeddingBody(384));
        });

        var service = new CustomEmbeddingService(new HttpClient(handler), EmbeddingSettings(string.Empty),
            Mock.Of<ILogger<CustomEmbeddingService>>());

        await service.Generate("hello");

        Assert.Null(captured!.Headers.Authorization);
    }

    [Fact]
    public async Task CustomEmbedding_WhenUnauthorized_ThrowsInsteadOfRandomVector()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Json(HttpStatusCode.Unauthorized, """{"error":{"code":"invalid_api_key"}}"""));

        var service = new CustomEmbeddingService(new HttpClient(handler), EmbeddingSettings(string.Empty),
            Mock.Of<ILogger<CustomEmbeddingService>>());

        await Assert.ThrowsAsync<HttpRequestException>(() => service.Generate("hello"));
    }

    [Fact]
    public async Task CustomEmbedding_BatchWhenUnauthorized_ThrowsInsteadOfRandomVectors()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Json(HttpStatusCode.Unauthorized, """{"error":{"code":"invalid_api_key"}}"""));

        var service = new CustomEmbeddingService(new HttpClient(handler), EmbeddingSettings(string.Empty),
            Mock.Of<ILogger<CustomEmbeddingService>>());

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GenerateBatch(["a", "b"]));
    }

    [Fact]
    public async Task CustomEmbedding_BatchWithMissingItems_Throws()
    {
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, EmbeddingBody(768)));

        var service = new CustomEmbeddingService(new HttpClient(handler), EmbeddingSettings(ApiKey),
            Mock.Of<ILogger<CustomEmbeddingService>>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateBatch(["a", "b"]));
    }

    [Fact]
    public async Task CustomLlm_WithApiKey_SendsBearerToken()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(req =>
        {
            captured = req;
            return Json(HttpStatusCode.OK, ChatBody("ok"));
        });

        var settings = new LlmSettings { Type = "Custom", ApiUrl = new Uri(ApiUrl), Model = "google/gemma-4-e4b", ApiKey = ApiKey };
        using var service = new CustomLlmService(new HttpClient(handler), settings, Mock.Of<ILogger<CustomLlmService>>());

        await service.CompleteAsync("hi");

        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.Equal(ApiKey, captured.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task CustomLlmEx_WithApiKey_SendsBearerToken()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(req =>
        {
            captured = req;
            return Json(HttpStatusCode.OK, ChatBody("ok"));
        });

        var settings = new LlmExSettings { Type = "Custom", ApiUrl = new Uri(ApiUrl), Model = "google/gemma-4-26b-a4b", ApiKey = ApiKey };
        var service = new CustomLlmExService(new HttpClient(handler), settings, Mock.Of<ILogger<CustomLlmExService>>());

        var result = await service.CompleteAsync("hi");

        Assert.Equal("ok", result);
        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.Equal(ApiKey, captured.Headers.Authorization?.Parameter);
    }

    private static EmbeddingSettings EmbeddingSettings(string apiKey) => new()
    {
        Type = "Custom",
        ApiUrl = new Uri(ApiUrl),
        Model = "text-embedding-nomic-embed-text-v1.5",
        ApiKey = apiKey
    };

    private static string EmbeddingBody(int dims) =>
        $$"""{"object":"list","data":[{"object":"embedding","index":0,"embedding":[{{string.Join(",", Enumerable.Repeat("0.01", dims))}}]}]}""";

    private static string ChatBody(string content) =>
        $$"""{"choices":[{"index":0,"message":{"role":"assistant","content":"{{content}}"},"finish_reason":"stop"}]}""";

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
