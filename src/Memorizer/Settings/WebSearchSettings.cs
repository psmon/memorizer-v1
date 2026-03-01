namespace Memorizer.Settings;

/// <summary>
/// Configuration for external web search providers.
/// </summary>
public sealed class WebSearchSettings
{
    public WebSearchAccessMode AccessMode { get; set; } = WebSearchAccessMode.Fetch;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
    public int MaxPageContentChars { get; set; } = 6000;
    public HeadlessWebSearchSettings Headless { get; set; } = new();

    public GoogleSearchSettings Google { get; set; } = new();
    public BingSearchSettings Bing { get; set; } = new();
    public NaverSearchSettings Naver { get; set; } = new();
}

public enum WebSearchAccessMode
{
    Api,
    Fetch,
    Headless
}

public sealed class HeadlessWebSearchSettings
{
    public bool Enabled { get; set; } = false;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(20);
    public bool AutoInstallBrowserOnStartup { get; set; } = true;
    public bool AutoInstallLinuxDependenciesOnStartup { get; set; } = false;
    public bool ChromiumSandbox { get; set; } = true;
    public string? BrowserInstallPath { get; set; }
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
