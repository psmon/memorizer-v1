using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Memorizer.Settings;
using Microsoft.Playwright;

namespace Memorizer.Services;

/// <summary>
/// External web search service supporting API, direct fetch scraping, and optional headless mode.
/// </summary>
public sealed class WebSearchService : IWebSearchService
{
    private static readonly Regex ScriptStyleRegex = new("<script[\\s\\S]*?</script>|<style[\\s\\S]*?</style>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

    private static readonly Regex GoogleResultRegex = new(
        "<a[^>]*href=\"/url\\?q=([^\"&]+)[^\"]*\"[^>]*>[\\s\\S]*?<h3[^>]*>([\\s\\S]*?)</h3>[\\s\\S]*?</a>[\\s\\S]*?<div[^>]*>([\\s\\S]*?)</div>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BingResultRegex = new(
        """<li[^>]*class="[^"]*b_algo[^"]*"[^>]*>[\s\S]*?<h2[^>]*><a[^>]*href="([^"]+)"[^>]*>([\s\S]*?)</a></h2>[\s\S]*?<p[^>]*>([\s\S]*?)</p>""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NaverResultRegex = new(
        """<a[^>]*class="[^"]*(?:title_link|link_tit|total_tit|api_txt_lines[^"]*total_tit|fds-comps-right-image-text-title)[^"]*"[^>]*href="([^"]+)"[^>]*>([\s\S]*?)</a>(?:[\s\S]*?<(?:div|a)[^>]*class="[^"]*(?:dsc_area|total_dsc_wrap|api_txt_lines[^"]*dsc_txt|fds-comps-right-image-text-description)[^"]*"[^>]*>([\s\S]*?)</(?:div|a)>)?""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly WebSearchSettings _settings;
    private readonly ILogger<WebSearchService> _logger;

    public WebSearchService(
        HttpClient httpClient,
        WebSearchSettings settings,
        ILogger<WebSearchService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;

        _httpClient.Timeout = settings.Timeout;
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Memorizer", "1.0"));
        }

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        WebSearchBrowserBootstrapService.ApplyBrowserPathEnvironment(_settings.Headless.BrowserInstallPath, _logger);
    }

    public async Task<WebSearchResponse> SearchAsync(
        WebSearchProvider provider,
        string query,
        int maxResults = 5,
        WebSearchAccessMode? accessMode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query must not be empty.", nameof(query));

        if (maxResults <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxResults), "maxResults must be greater than 0.");

        var mode = accessMode ?? _settings.AccessMode;

        return mode switch
        {
            WebSearchAccessMode.Api => await SearchWithApiAsync(provider, query, maxResults, cancellationToken),
            WebSearchAccessMode.Fetch => await SearchWithFetchAsync(provider, query, maxResults, cancellationToken),
            WebSearchAccessMode.Headless => await SearchWithHeadlessAsync(provider, query, maxResults, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(accessMode), mode, "Unsupported access mode")
        };
    }

    public async Task<PageReadResponse> ReadPageAsync(
        string url,
        WebSearchAccessMode? accessMode = null,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("Only absolute http/https URLs are allowed.", nameof(url));

        var mode = accessMode ?? _settings.AccessMode;
        var html = mode == WebSearchAccessMode.Headless
            ? await FetchHtmlWithHeadlessAsync(uri.ToString(), cancellationToken)
            : await FetchHtmlAsync(uri.ToString(), cancellationToken);

        var title = ExtractTitle(html);
        var text = ExtractText(html);
        if (text.Length > _settings.MaxPageContentChars)
            text = text[.._settings.MaxPageContentChars];

        return new PageReadResponse(url, title, text, 200, DateTime.UtcNow);
    }

    public async Task<WebSearchPreviewResponse> SearchAndReadTopResultAsync(
        WebSearchProvider provider,
        string query,
        int maxResults = 5,
        WebSearchAccessMode? accessMode = null,
        CancellationToken cancellationToken = default)
    {
        var mode = accessMode ?? _settings.AccessMode;
        var search = await SearchAsync(provider, query, maxResults, mode, cancellationToken);
        var top = search.Items.FirstOrDefault();
        if (top == null)
            return new WebSearchPreviewResponse(provider, query, null, null);

        try
        {
            var page = await ReadPageAsync(top.Url, mode, cancellationToken);
            return new WebSearchPreviewResponse(provider, query, top, page);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read top search result page: {Url}", top.Url);
            return new WebSearchPreviewResponse(provider, query, top, null);
        }
    }

    private async Task<WebSearchResponse> SearchWithApiAsync(WebSearchProvider provider, string query, int maxResults, CancellationToken cancellationToken)
    {
        var encodedQuery = UrlEncoder.Default.Encode(query);

        return provider switch
        {
            WebSearchProvider.Google => await SearchGoogleApiAsync(query, encodedQuery, maxResults, cancellationToken),
            WebSearchProvider.Bing => await SearchBingApiAsync(query, encodedQuery, maxResults, cancellationToken),
            WebSearchProvider.Naver => await SearchNaverApiAsync(query, encodedQuery, maxResults, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported provider")
        };
    }

    private async Task<WebSearchResponse> SearchWithFetchAsync(WebSearchProvider provider, string query, int maxResults, CancellationToken cancellationToken)
    {
        var encodedQuery = UrlEncoder.Default.Encode(query);
        var searchUrl = provider switch
        {
            WebSearchProvider.Google => $"https://www.google.com/search?q={encodedQuery}&hl=ko",
            WebSearchProvider.Bing => $"https://www.bing.com/search?q={encodedQuery}&setlang=ko",
            WebSearchProvider.Naver => $"https://search.naver.com/search.naver?where=nexearch&query={encodedQuery}",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported provider")
        };

        var html = await FetchHtmlAsync(searchUrl, cancellationToken);
        var items = ParseSearchResults(provider, html, maxResults);
        return new WebSearchResponse(provider, query, items);
    }

    private async Task<WebSearchResponse> SearchWithHeadlessAsync(WebSearchProvider provider, string query, int maxResults, CancellationToken cancellationToken)
    {
        if (!_settings.Headless.Enabled)
            throw new InvalidOperationException("Headless mode is disabled. Set WebSearch:Headless:Enabled=true.");

        var encodedQuery = UrlEncoder.Default.Encode(query);
        var searchUrl = provider switch
        {
            WebSearchProvider.Google => $"https://www.google.com/search?q={encodedQuery}&hl=ko",
            WebSearchProvider.Bing => $"https://www.bing.com/search?q={encodedQuery}&setlang=ko",
            WebSearchProvider.Naver => $"https://search.naver.com/search.naver?where=nexearch&query={encodedQuery}",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported provider")
        };

        _logger.LogInformation("Headless search URL: {SearchUrl}", searchUrl);

        WebSearchBrowserBootstrapService.ApplyBrowserPathEnvironment(_settings.Headless.BrowserInstallPath, _logger);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_settings.Headless.Timeout);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            ChromiumSandbox = _settings.Headless.ChromiumSandbox
        });

        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
        });

        await page.GotoAsync(searchUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = (float)_settings.Headless.Timeout.TotalMilliseconds
        });

        timeoutCts.Token.ThrowIfCancellationRequested();

        IReadOnlyList<WebSearchItem> items;
        if (provider == WebSearchProvider.Naver)
        {
            items = await ParseNaverWithPlaywrightAsync(page, maxResults);
        }
        else
        {
            var html = await page.ContentAsync();
            _logger.LogInformation("Headless search returned HTML length: {HtmlLength} chars for provider {Provider}", html.Length, provider);
            items = ParseSearchResults(provider, html, maxResults);
        }

        _logger.LogInformation("Headless search parsed {ItemCount} results from {Provider}", items.Count, provider);
        return new WebSearchResponse(provider, query, items);
    }

    private async Task<IReadOnlyList<WebSearchItem>> ParseNaverWithPlaywrightAsync(IPage page, int maxResults)
    {
        var items = new List<WebSearchItem>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Naver uses data-heatmap-target=".link" for search result links
        var linkElements = await page.QuerySelectorAllAsync("a[data-heatmap-target=\".link\"]");
        _logger.LogInformation("Naver DOM query found {Count} link elements", linkElements.Count);

        foreach (var element in linkElements)
        {
            if (items.Count >= maxResults)
                break;

            var href = await element.GetAttributeAsync("href");
            if (string.IsNullOrWhiteSpace(href))
                continue;

            // Filter out internal Naver links
            if (href.Contains("naver.com") || href.Contains("pstatic.net"))
                continue;

            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
                continue;

            // Deduplicate
            if (!seenUrls.Add(uri.AbsoluteUri))
                continue;

            var title = (await element.InnerTextAsync())?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
                title = uri.Host;

            items.Add(new WebSearchItem(title, uri.AbsoluteUri, string.Empty, WebSearchProvider.Naver));
        }

        return items;
    }

    private async Task<string> FetchHtmlAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await ReadContentAsStringSafeAsync(response.Content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Fetch failed with status code {(int)response.StatusCode} ({response.StatusCode}) for {url}.");

        return body;
    }

    private async Task<string> FetchHtmlWithHeadlessAsync(string url, CancellationToken cancellationToken)
    {
        if (!_settings.Headless.Enabled)
            throw new InvalidOperationException("Headless mode is disabled. Set WebSearch:Headless:Enabled=true.");

        WebSearchBrowserBootstrapService.ApplyBrowserPathEnvironment(_settings.Headless.BrowserInstallPath, _logger);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_settings.Headless.Timeout);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            ChromiumSandbox = _settings.Headless.ChromiumSandbox
        });

        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
        });

        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = (float)_settings.Headless.Timeout.TotalMilliseconds
        });

        timeoutCts.Token.ThrowIfCancellationRequested();
        return await page.ContentAsync();
    }

    private IReadOnlyList<WebSearchItem> ParseSearchResults(WebSearchProvider provider, string html, int maxResults)
    {
        var list = new List<WebSearchItem>();
        var matches = provider switch
        {
            WebSearchProvider.Google => GoogleResultRegex.Matches(html),
            WebSearchProvider.Bing => BingResultRegex.Matches(html),
            WebSearchProvider.Naver => NaverResultRegex.Matches(html),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported provider")
        };

        foreach (Match match in matches)
        {
            if (list.Count >= maxResults)
                break;

            var url = CleanUrl(match.Groups[1].Value, provider);
            if (string.IsNullOrWhiteSpace(url))
                continue;

            var title = CleanInlineHtml(match.Groups[2].Value);
            var snippet = CleanInlineHtml(match.Groups[3].Value);

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                continue;

            list.Add(new WebSearchItem(title, url, snippet, provider));
        }

        return list;
    }

    private async Task<WebSearchResponse> SearchGoogleApiAsync(string query, string encodedQuery, int maxResults, CancellationToken cancellationToken)
    {
        EnsureGoogleConfigured();

        var requestUri = new Uri($"{_settings.Google.ApiUrl}?q={encodedQuery}&num={Math.Min(maxResults, 10)}&key={UrlEncoder.Default.Encode(_settings.Google.ApiKey)}&cx={UrlEncoder.Default.Encode(_settings.Google.CustomSearchEngineId)}");

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, content, WebSearchProvider.Google);

        using var doc = JsonDocument.Parse(content);
        var items = new List<WebSearchItem>();

        if (doc.RootElement.TryGetProperty("items", out var googleItems) && googleItems.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in googleItems.EnumerateArray().Take(maxResults))
            {
                var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                var url = item.TryGetProperty("link", out var l) ? l.GetString() ?? string.Empty : string.Empty;
                var snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() ?? string.Empty : string.Empty;

                if (!string.IsNullOrWhiteSpace(url))
                {
                    items.Add(new WebSearchItem(title, url, snippet, WebSearchProvider.Google));
                }
            }
        }

        return new WebSearchResponse(WebSearchProvider.Google, query, items);
    }

    private async Task<WebSearchResponse> SearchBingApiAsync(string query, string encodedQuery, int maxResults, CancellationToken cancellationToken)
    {
        EnsureBingConfigured();

        var requestUri = new Uri($"{_settings.Bing.ApiUrl}?q={encodedQuery}&count={Math.Min(maxResults, 50)}&mkt=ko-KR");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _settings.Bing.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, content, WebSearchProvider.Bing);

        using var doc = JsonDocument.Parse(content);
        var items = new List<WebSearchItem>();

        if (doc.RootElement.TryGetProperty("webPages", out var webPages) &&
            webPages.TryGetProperty("value", out var values) &&
            values.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in values.EnumerateArray().Take(maxResults))
            {
                var title = item.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                var url = item.TryGetProperty("url", out var u) ? u.GetString() ?? string.Empty : string.Empty;
                var snippet = item.TryGetProperty("snippet", out var s) ? s.GetString() ?? string.Empty : string.Empty;

                if (!string.IsNullOrWhiteSpace(url))
                {
                    items.Add(new WebSearchItem(title, url, snippet, WebSearchProvider.Bing));
                }
            }
        }

        return new WebSearchResponse(WebSearchProvider.Bing, query, items);
    }

    private async Task<WebSearchResponse> SearchNaverApiAsync(string query, string encodedQuery, int maxResults, CancellationToken cancellationToken)
    {
        EnsureNaverConfigured();

        var display = Math.Clamp(maxResults, 1, 100);
        var requestUri = new Uri($"{_settings.Naver.ApiUrl}?query={encodedQuery}&display={display}&start=1&sort=sim");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("X-Naver-Client-Id", _settings.Naver.ClientId);
        request.Headers.Add("X-Naver-Client-Secret", _settings.Naver.ClientSecret);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, content, WebSearchProvider.Naver);

        using var doc = JsonDocument.Parse(content);
        var items = new List<WebSearchItem>();

        if (doc.RootElement.TryGetProperty("items", out var naverItems) && naverItems.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in naverItems.EnumerateArray().Take(maxResults))
            {
                var title = item.TryGetProperty("title", out var t) ? CleanInlineHtml(t.GetString() ?? string.Empty) : string.Empty;
                var url = item.TryGetProperty("link", out var l) ? l.GetString() ?? string.Empty : string.Empty;
                var snippet = item.TryGetProperty("description", out var d) ? CleanInlineHtml(d.GetString() ?? string.Empty) : string.Empty;

                if (!string.IsNullOrWhiteSpace(url))
                {
                    items.Add(new WebSearchItem(title, url, snippet, WebSearchProvider.Naver));
                }
            }
        }

        return new WebSearchResponse(WebSearchProvider.Naver, query, items);
    }

    private void EnsureGoogleConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.Google.ApiKey) || string.IsNullOrWhiteSpace(_settings.Google.CustomSearchEngineId))
            throw new InvalidOperationException("Google search API is not configured. Set WebSearch:Google:ApiKey and WebSearch:Google:CustomSearchEngineId.");
    }

    private void EnsureBingConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.Bing.ApiKey))
            throw new InvalidOperationException("Bing search API is not configured. Set WebSearch:Bing:ApiKey.");
    }

    private void EnsureNaverConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.Naver.ClientId) || string.IsNullOrWhiteSpace(_settings.Naver.ClientSecret))
            throw new InvalidOperationException("Naver search API is not configured. Set WebSearch:Naver:ClientId and WebSearch:Naver:ClientSecret.");
    }

    private static string CleanUrl(string rawUrl, WebSearchProvider provider)
    {
        var decoded = WebUtility.HtmlDecode(rawUrl);

        if (provider == WebSearchProvider.Google && decoded.StartsWith("/url?q=", StringComparison.OrdinalIgnoreCase))
        {
            var qs = decoded[7..];
            var amp = qs.IndexOf('&');
            return amp > 0 ? qs[..amp] : qs;
        }

        return decoded;
    }

    private static void EnsureSuccess(HttpStatusCode statusCode, string content, WebSearchProvider provider)
    {
        if ((int)statusCode < 200 || (int)statusCode >= 300)
        {
            var snippet = content.Length > 200 ? content[..200] : content;
            throw new HttpRequestException($"{provider} search API failed with status {(int)statusCode}. Body: {snippet}");
        }
    }

    private static string ExtractTitle(string html)
    {
        var match = Regex.Match(html, "<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
            return string.Empty;

        return WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
    }

    private static string ExtractText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var withoutScripts = ScriptStyleRegex.Replace(html, " ");
        var noTags = TagRegex.Replace(withoutScripts, " ");
        var decoded = WebUtility.HtmlDecode(noTags);
        return WhitespaceRegex.Replace(decoded, " ").Trim();
    }

    private static string CleanInlineHtml(string value)
    {
        var noTags = TagRegex.Replace(value, " ");
        return WhitespaceRegex.Replace(WebUtility.HtmlDecode(noTags), " ").Trim();
    }

    private static async Task<string> ReadContentAsStringSafeAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var bytes = await content.ReadAsByteArrayAsync(cancellationToken);
        var charset = content.Headers.ContentType?.CharSet;

        if (!string.IsNullOrWhiteSpace(charset))
        {
            try
            {
                var encoding = Encoding.GetEncoding(charset.Trim('"'));
                return encoding.GetString(bytes);
            }
            catch (ArgumentException)
            {
                // Fall through to UTF-8 and EUC-KR fallback below.
            }
        }

        // Most modern pages are UTF-8.
        var utf8 = Encoding.UTF8.GetString(bytes);
        if (utf8.Contains('\uFFFD'))
        {
            try
            {
                return Encoding.GetEncoding("EUC-KR").GetString(bytes);
            }
            catch (ArgumentException)
            {
                return utf8;
            }
        }

        return utf8;
    }
}
