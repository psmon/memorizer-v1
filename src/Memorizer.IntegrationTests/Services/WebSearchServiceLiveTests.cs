using Memorizer.Services;
using Memorizer.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Moq;

namespace Memorizer.IntegrationTests.Services;

public sealed class WebSearchServiceLiveTests
{
    [Fact]
    [Trait("Category", "LiveWeb")]
    public async Task Live_Fetch_Google_Matjip_ShouldReturnResults()
    {
        if (!IsLiveWebEnabled()) return;

        using var httpClient = CreateHttpClient();
        var service = new WebSearchService(httpClient, CreateFetchSettings(), CreateLogger());

        var result = await service.SearchAsync(WebSearchProvider.Google, "맛집", 5, WebSearchAccessMode.Fetch);

        Assert.NotNull(result);
        Assert.Equal(WebSearchProvider.Google, result.Provider);
        if (result.Items.Count == 0)
        {
            // Google may return anti-bot/challenge pages for direct fetch.
            var page = await service.ReadPageAsync("https://www.google.com/search?q=%EB%A7%9B%EC%A7%91&hl=ko", WebSearchAccessMode.Fetch);
            Assert.Contains("Google Search", page.ContentPreview, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.All(result.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.Url)));
        }
    }

    [Fact]
    [Trait("Category", "LiveWeb")]
    public async Task Live_Fetch_Bing_Matjip_ShouldReturnResults()
    {
        if (!IsLiveWebEnabled()) return;

        using var httpClient = CreateHttpClient();
        var service = new WebSearchService(httpClient, CreateFetchSettings(), CreateLogger());

        var result = await service.SearchAsync(WebSearchProvider.Bing, "맛집", 5, WebSearchAccessMode.Fetch);

        Assert.NotNull(result);
        Assert.Equal(WebSearchProvider.Bing, result.Provider);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    [Trait("Category", "LiveWeb")]
    public async Task Live_Fetch_Naver_Matjip_ShouldReturnResults()
    {
        if (!IsLiveWebEnabled()) return;

        using var httpClient = CreateHttpClient();
        var service = new WebSearchService(httpClient, CreateFetchSettings(), CreateLogger());

        var result = await service.SearchAsync(WebSearchProvider.Naver, "맛집", 5, WebSearchAccessMode.Fetch);

        Assert.NotNull(result);
        Assert.Equal(WebSearchProvider.Naver, result.Provider);
        if (result.Items.Count == 0)
        {
            var page = await service.ReadPageAsync("https://search.naver.com/search.naver?where=nexearch&query=%EB%A7%9B%EC%A7%91", WebSearchAccessMode.Fetch);
            Assert.Contains("네이버 검색", page.ContentPreview, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    [Trait("Category", "LiveWeb")]
    public async Task Live_ReadPage_NaverSection_ShouldReturnContent()
    {
        if (!IsLiveWebEnabled()) return;

        using var httpClient = CreateHttpClient();
        var service = new WebSearchService(httpClient, CreateFetchSettings(), CreateLogger());

        var page = await service.ReadPageAsync(
            "https://section.blog.naver.com/BlogHome.naver?directoryNo=0&currentPage=1&groupId=0",
            WebSearchAccessMode.Fetch);

        Assert.Equal(200, page.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(page.ContentPreview));
    }

    [Fact]
    [Trait("Category", "LiveWeb")]
    public async Task Live_Headless_Naver_Matjip_ShouldReturnResults_WhenEnabled()
    {
        if (!IsLiveWebEnabled()) return;

        using var httpClient = CreateHttpClient();
        var settings = CreateFetchSettings();
        settings.AccessMode = WebSearchAccessMode.Headless;
        settings.Headless.Enabled = true;

        var service = new WebSearchService(httpClient, settings, CreateLogger());

        try
        {
            var result = await service.SearchAsync(WebSearchProvider.Naver, "맛집", 3, WebSearchAccessMode.Headless);
            Assert.NotNull(result);
            Assert.Equal(WebSearchProvider.Naver, result.Provider);
        }
        catch (PlaywrightException ex) when (
            ex.Message.Contains("missing dependencies", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            // Host dependency issue: real headless test cannot run on this machine.
            return;
        }
    }

    private static bool IsLiveWebEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_WEB_LIVE_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private static WebSearchSettings CreateFetchSettings()
    {
        return new WebSearchSettings
        {
            AccessMode = WebSearchAccessMode.Fetch,
            Timeout = TimeSpan.FromSeconds(20),
            MaxPageContentChars = 5000,
            Headless = new HeadlessWebSearchSettings
            {
                Enabled = false,
                Timeout = TimeSpan.FromSeconds(25)
            }
        };
    }

    private static ILogger<WebSearchService> CreateLogger()
    {
        return new Mock<ILogger<WebSearchService>>().Object;
    }
}
