namespace Memorizer.Settings;

/// <summary>
/// Configuration for external web search providers.
/// </summary>
public sealed class WebSearchSettings
{
    public WebSearchAccessMode AccessMode { get; set; } = WebSearchAccessMode.Fetch;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
    public int MaxPageContentChars { get; set; } = 6000;
    /// <summary>
    /// Ordered, comma-separated providers used by automatic web search (AskBot), each as
    /// "Provider" or "Provider:AccessMode". The first provider returning results wins.
    /// Examples: "SearXng", "SearXng,Naver:Api". SearXng ignores the access mode.
    /// </summary>
    public string ProviderChain { get; set; } = "SearXng";

    public SearXngSearchSettings SearXng { get; set; } = new();

    public GoogleSearchSettings Google { get; set; } = new();
    public BingSearchSettings Bing { get; set; } = new();
    public NaverSearchSettings Naver { get; set; } = new();
}

public enum WebSearchAccessMode
{
    Api,
    Fetch,
    /// <summary>Deprecated: the Playwright headless browser was removed. Treated as <see cref="Fetch"/>.</summary>
    Headless
}

public sealed class SearXngSearchSettings
{
    /// <summary>Base URL of the SearXNG instance (the "json" format must be enabled in its settings.yml).</summary>
    public Uri ApiUrl { get; set; } = new("http://localhost:8888");

    /// <summary>Search language for non-Korean queries ("auto" lets SearXNG decide). Korean queries use "ko-KR".</summary>
    public string Language { get; set; } = "auto";

    public string Categories { get; set; } = "general";
}

public sealed class GoogleSearchSettings
{
    public Uri ApiUrl { get; set; } = new("https://www.googleapis.com/customsearch/v1");
    public string ApiKey { get; set; } = string.Empty;
    public string CustomSearchEngineId { get; set; } = string.Empty;
}

public sealed class BingSearchSettings
{
    public Uri ApiUrl { get; set; } = new("https://api.bing.microsoft.com/v7.0/search");
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class NaverSearchSettings
{
    public Uri ApiUrl { get; set; } = new("https://openapi.naver.com/v1/search/webkr.json");
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
