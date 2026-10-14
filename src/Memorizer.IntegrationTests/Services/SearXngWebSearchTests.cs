using System.Net;
using System.Text;
using Memorizer.Services;
using Memorizer.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace Memorizer.IntegrationTests.Services;

/// <summary>
/// SearXNG provider (JSON API) and the WebSearch:ProviderChain fallback used by AskBot.
/// </summary>
public sealed class SearXngWebSearchTests
{
    private const string SearXngUrl = "http://searxng.test:8080";

    [Fact]
    public async Task SearchAsync_SearXng_ParsesResults_AndDeduplicatesUrls()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(req =>
        {
            captured = req;
            return Json(HttpStatusCode.OK, """
                {
                  "results": [
                    { "url": "https://getakka.net/articles/concepts/actors.html", "title": "Actors | <b>Akka.NET</b>", "content": "Actor concepts", "engine": "google cse" },
                    { "url": "https://getakka.net/articles/concepts/actors.html", "title": "Actors (dup)", "content": "dup", "engine": "bing" },
                    { "url": "https://example.com/akka", "title": "Akka example", "content": "Example", "engine": "naver" }
                  ],
                  "unresponsive_engines": [["brave", "too many requests"]]
                }
                """);
        });

        var service = CreateService(handler, chain: "SearXng");

        var result = await service.SearchAsync(WebSearchProvider.SearXng, "Akka.NET actor", 5);

        Assert.Equal(WebSearchProvider.SearXng, result.Provider);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Actors | Akka.NET", result.Items[0].Title);
        Assert.Equal("https://example.com/akka", result.Items[1].Url);

        var uri = captured!.RequestUri!;
        Assert.Equal("/search", uri.AbsolutePath);
        Assert.Contains("format=json", uri.Query);
        Assert.Contains("language=auto", uri.Query);
        Assert.Contains("categories=general", uri.Query);
    }

    [Fact]
    public async Task SearchAsync_SearXng_KoreanQuery_UsesKoreanLanguage()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHttpMessageHandler(req =>
        {
            captured = req;
            return Json(HttpStatusCode.OK, """{ "results": [] }""");
        });

        var service = CreateService(handler, chain: "SearXng");

        await service.SearchAsync(WebSearchProvider.SearXng, "리액티브 스트림 동향", 5);

        Assert.Contains("language=ko-KR", captured!.RequestUri!.Query);
    }

    [Fact]
    public async Task SearchAsync_SearXng_RespectsMaxResults()
    {
        var results = string.Join(",", Enumerable.Range(1, 10)
            .Select(i => $$"""{ "url": "https://example.com/{{i}}", "title": "T{{i}}", "content": "C{{i}}" }"""));
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, $$"""{ "results": [{{results}}] }"""));

        var service = CreateService(handler, chain: "SearXng");

        var result = await service.SearchAsync(WebSearchProvider.SearXng, "query", 3);

        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task SearchWithFallbackAsync_SearXngFails_FallsBackToNextProvider()
    {
        var requestedHosts = new List<string>();
        var handler = new StubHttpMessageHandler(req =>
        {
            requestedHosts.Add(req.RequestUri!.Host);
            if (req.RequestUri.Host == "searxng.test")
                return Json(HttpStatusCode.InternalServerError, "boom");

            return Json(HttpStatusCode.OK, """
                { "items": [ { "title": "네이버 결과", "link": "https://naver.example/1", "description": "설명" } ] }
                """);
        });

        var service = CreateService(handler, chain: "SearXng, Naver:Api");

        var result = await service.SearchWithFallbackAsync("query", 5);

        Assert.Equal(WebSearchProvider.Naver, result.Provider);
        Assert.Single(result.Items);
        Assert.Equal(new[] { "searxng.test", "openapi.naver.com" }, requestedHosts);
    }

    [Fact]
    public async Task SearchWithFallbackAsync_FirstProviderHasResults_DoesNotCallNext()
    {
        var calls = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            calls++;
            return Json(HttpStatusCode.OK, """{ "results": [ { "url": "https://example.com", "title": "ok", "content": "" } ] }""");
        });

        var service = CreateService(handler, chain: "SearXng, Naver:Api");

        var result = await service.SearchWithFallbackAsync("query", 5);

        Assert.Equal(WebSearchProvider.SearXng, result.Provider);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task SearchWithFallbackAsync_AllProvidersFail_ReturnsEmpty()
    {
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.ServiceUnavailable, "down"));

        var service = CreateService(handler, chain: "SearXng");

        var result = await service.SearchWithFallbackAsync("query", 5);

        Assert.Empty(result.Items);
    }

    [Theory]
    [InlineData("SearXng", 1)]
    [InlineData("SearXng, Naver:Fetch", 2)]
    [InlineData("searxng,naver:api,google:fetch", 3)]
    [InlineData("SearXng, Unknown, Naver:BadMode", 1)]
    [InlineData("", 0)]
    public void ParseProviderChain_ParsesEntries(string chain, int expectedCount)
    {
        var parsed = WebSearchService.ParseProviderChain(chain);

        Assert.Equal(expectedCount, parsed.Count);
    }

    [Fact]
    public void ParseProviderChain_ReadsProviderAndMode()
    {
        var parsed = WebSearchService.ParseProviderChain("SearXng, Naver:Fetch");

        Assert.Equal((WebSearchProvider.SearXng, (WebSearchAccessMode?)null), parsed[0]);
        Assert.Equal((WebSearchProvider.Naver, (WebSearchAccessMode?)WebSearchAccessMode.Fetch), parsed[1]);
    }

    private static WebSearchService CreateService(HttpMessageHandler handler, string chain)
    {
        var settings = new WebSearchSettings
        {
            ProviderChain = chain,
            SearXng = new SearXngSearchSettings { ApiUrl = new Uri(SearXngUrl) },
            Naver = new NaverSearchSettings { ClientId = "naver-id", ClientSecret = "naver-secret" }
        };

        return new WebSearchService(new HttpClient(handler), settings, Mock.Of<ILogger<WebSearchService>>());
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
