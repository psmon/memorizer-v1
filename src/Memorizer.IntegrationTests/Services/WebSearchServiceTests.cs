using System.Net;
using System.Text;
using Memorizer.Services;
using Memorizer.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace Memorizer.IntegrationTests.Services;

public sealed class WebSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Google_WithMatjip_ShouldParseResults()
    {
        var handler = new StubHttpMessageHandler((req, ct) =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            var query = req.RequestUri!.Query;
            Assert.Contains("q=", query);
            Assert.Contains("key=google-key", query);
            Assert.Contains("cx=google-cx", query);

            const string body = """
                {
                  "items": [
                    {
                      "title": "서울 맛집 추천",
                      "link": "https://example.com/google/matjip",
                      "snippet": "맛집 모음"
                    }
                  ]
                }
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        });

        using var httpClient = new HttpClient(handler);
        var service = new WebSearchService(httpClient, CreateSettings(), CreateLogger());

        var result = await service.SearchAsync(WebSearchProvider.Google, "맛집", 3, WebSearchAccessMode.Api);

        Assert.Single(result.Items);
        Assert.Equal(WebSearchProvider.Google, result.Provider);
        Assert.Equal("서울 맛집 추천", result.Items[0].Title);
        Assert.Equal("https://example.com/google/matjip", result.Items[0].Url);
    }

    [Fact]
    public async Task SearchAsync_Bing_WithMatjip_ShouldParseResults()
    {
        var handler = new StubHttpMessageHandler((req, ct) =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("bing-key", req.Headers.GetValues("Ocp-Apim-Subscription-Key").Single());
            Assert.Contains("q=", req.RequestUri!.Query);

            const string body = """
                {
                  "webPages": {
                    "value": [
                      {
                        "name": "Bing 맛집 TOP",
                        "url": "https://example.com/bing/matjip",
                        "snippet": "빙 검색 결과"
                      }
                    ]
                  }
                }
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        });

        using var httpClient = new HttpClient(handler);
        var service = new WebSearchService(httpClient, CreateSettings(), CreateLogger());

        var result = await service.SearchAsync(WebSearchProvider.Bing, "맛집", 3, WebSearchAccessMode.Api);

        Assert.Single(result.Items);
        Assert.Equal(WebSearchProvider.Bing, result.Provider);
        Assert.Equal("Bing 맛집 TOP", result.Items[0].Title);
        Assert.Equal("https://example.com/bing/matjip", result.Items[0].Url);
    }

    [Fact]
    public async Task SearchAsync_Naver_WithMatjip_ShouldParseAndCleanHtmlResults()
    {
        var handler = new StubHttpMessageHandler((req, ct) =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("naver-id", req.Headers.GetValues("X-Naver-Client-Id").Single());
            Assert.Equal("naver-secret", req.Headers.GetValues("X-Naver-Client-Secret").Single());
            Assert.Contains("query=", req.RequestUri!.Query);

            const string body = """
                {
                  "items": [
                    {
                      "title": "<b>맛집</b> 베스트",
                      "link": "https://example.com/naver/matjip",
                      "description": "서울 <b>맛집</b> 총정리"
                    }
                  ]
                }
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        });

        using var httpClient = new HttpClient(handler);
        var service = new WebSearchService(httpClient, CreateSettings(), CreateLogger());

        var result = await service.SearchAsync(WebSearchProvider.Naver, "맛집", 3, WebSearchAccessMode.Api);

        Assert.Single(result.Items);
        Assert.Equal(WebSearchProvider.Naver, result.Provider);
        Assert.Equal("맛집 베스트", result.Items[0].Title);
        Assert.Equal("서울 맛집 총정리", result.Items[0].Snippet);
    }

    [Fact]
    public async Task ReadPageAsync_NaverBlogHomeUrl_ShouldExtractTitleAndContentPreview()
    {
        var html = """
            <html>
              <head><title>네이버 블로그 홈</title></head>
              <body>
                <h1>맛집 블로그 모음</h1>
                <script>console.log('x')</script>
                <p>서울 맛집, 부산 맛집 정보를 확인하세요.</p>
              </body>
            </html>
            """;

        var handler = new StubHttpMessageHandler((req, ct) =>
        {
            Assert.Equal("https://section.blog.naver.com/BlogHome.naver?directoryNo=0&currentPage=1&groupId=0", req.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            });
        });

        using var httpClient = new HttpClient(handler);
        var service = new WebSearchService(httpClient, CreateSettings(), CreateLogger());

        var page = await service.ReadPageAsync("https://section.blog.naver.com/BlogHome.naver?directoryNo=0&currentPage=1&groupId=0");

        Assert.Equal(200, page.StatusCode);
        Assert.Equal("네이버 블로그 홈", page.Title);
        Assert.Contains("맛집 블로그 모음", page.ContentPreview);
        Assert.DoesNotContain("console.log", page.ContentPreview);
    }

    [Fact]
    public async Task SearchAndReadTopResultAsync_Google_WithMatjip_ShouldReturnTopPage()
    {
        var call = 0;
        var handler = new StubHttpMessageHandler((req, ct) =>
        {
            call++;
            if (call == 1)
            {
                const string searchBody = """
                    {
                      "items": [
                        {
                          "title": "맛집 랭킹",
                          "link": "https://example.com/top-matjip",
                          "snippet": "전국 맛집"
                        }
                      ]
                    }
                    """;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(searchBody, Encoding.UTF8, "application/json")
                });
            }

            const string htmlBody = """
                <html><head><title>맛집 랭킹 페이지</title></head><body><p>맛집 상세 내용</p></body></html>
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(htmlBody, Encoding.UTF8, "text/html")
            });
        });

        using var httpClient = new HttpClient(handler);
        var service = new WebSearchService(httpClient, CreateSettings(), CreateLogger());

        var preview = await service.SearchAndReadTopResultAsync(WebSearchProvider.Google, "맛집", 3, WebSearchAccessMode.Api);

        Assert.NotNull(preview.TopResult);
        Assert.NotNull(preview.TopPage);
        Assert.Equal("맛집 랭킹", preview.TopResult!.Title);
        Assert.Equal("맛집 랭킹 페이지", preview.TopPage!.Title);
        Assert.Contains("맛집 상세 내용", preview.TopPage.ContentPreview);
    }

    [Fact]
    public async Task SearchAsync_Google_WithFetchMode_ShouldParseHtmlResults()
    {
        var html = """
            <html><body>
            <a href="/url?q=https://example.com/google/matjip&sa=U"><h3>구글 맛집</h3></a>
            <div>구글 스니펫 내용</div>
            </body></html>
            """;

        var handler = new StubHttpMessageHandler((req, ct) =>
        {
            Assert.Contains("google.com/search", req.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            });
        });

        using var httpClient = new HttpClient(handler);
        var service = new WebSearchService(httpClient, CreateFetchSettings(), CreateLogger());

        var result = await service.SearchAsync(WebSearchProvider.Google, "맛집", 3, WebSearchAccessMode.Fetch);

        Assert.Single(result.Items);
        Assert.Equal("https://example.com/google/matjip", result.Items[0].Url);
        Assert.Equal("구글 맛집", result.Items[0].Title);
    }

    [Fact]
    public async Task SearchAsync_Bing_WithFetchMode_ShouldParseHtmlResults()
    {
        var html = """
            <html><body>
            <li class="b_algo">
              <h2><a href="https://example.com/bing/matjip">빙 맛집</a></h2>
              <p>빙 스니펫 내용</p>
            </li>
            </body></html>
            """;

        var handler = new StubHttpMessageHandler((req, ct) =>
        {
            Assert.Contains("bing.com/search", req.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            });
        });

        using var httpClient = new HttpClient(handler);
        var service = new WebSearchService(httpClient, CreateFetchSettings(), CreateLogger());

        var result = await service.SearchAsync(WebSearchProvider.Bing, "맛집", 3, WebSearchAccessMode.Fetch);

        Assert.Single(result.Items);
        Assert.Equal("빙 맛집", result.Items[0].Title);
        Assert.Equal("https://example.com/bing/matjip", result.Items[0].Url);
    }

    [Fact]
    public async Task SearchAsync_Naver_WithFetchMode_ShouldParseHtmlResults()
    {
        var html = """
            <html><body>
            <a class="title_link" href="https://example.com/naver/matjip">네이버 맛집</a>
            <div class="dsc_area">네이버 스니펫 내용</div>
            </body></html>
            """;

        var handler = new StubHttpMessageHandler((req, ct) =>
        {
            Assert.Contains("search.naver.com/search.naver", req.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            });
        });

        using var httpClient = new HttpClient(handler);
        var service = new WebSearchService(httpClient, CreateFetchSettings(), CreateLogger());

        var result = await service.SearchAsync(WebSearchProvider.Naver, "맛집", 3, WebSearchAccessMode.Fetch);

        Assert.Single(result.Items);
        Assert.Equal("네이버 맛집", result.Items[0].Title);
        Assert.Equal("https://example.com/naver/matjip", result.Items[0].Url);
    }

    [Fact]
    public async Task SearchAsync_Headless_WhenDisabled_ShouldThrow()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

        var settings = CreateFetchSettings();
        settings.Headless.Enabled = false;
        var service = new WebSearchService(httpClient, settings, CreateLogger());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SearchAsync(WebSearchProvider.Google, "맛집", 3, WebSearchAccessMode.Headless));
    }

    private static WebSearchSettings CreateSettings()
    {
        return new WebSearchSettings
        {
            AccessMode = WebSearchAccessMode.Api,
            Timeout = TimeSpan.FromSeconds(5),
            MaxPageContentChars = 2000,
            Google = new GoogleSearchSettings
            {
                ApiKey = "google-key",
                CustomSearchEngineId = "google-cx"
            },
            Bing = new BingSearchSettings
            {
                ApiKey = "bing-key"
            },
            Naver = new NaverSearchSettings
            {
                ClientId = "naver-id",
                ClientSecret = "naver-secret"
            }
        };
    }

    private static WebSearchSettings CreateFetchSettings()
    {
        return new WebSearchSettings
        {
            AccessMode = WebSearchAccessMode.Fetch,
            Timeout = TimeSpan.FromSeconds(5),
            MaxPageContentChars = 2000,
            Headless = new HeadlessWebSearchSettings
            {
                Enabled = false,
                Timeout = TimeSpan.FromSeconds(10)
            }
        };
    }

    private static ILogger<WebSearchService> CreateLogger()
    {
        return new Mock<ILogger<WebSearchService>>().Object;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
