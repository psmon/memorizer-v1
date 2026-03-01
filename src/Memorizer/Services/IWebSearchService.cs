using Memorizer.Settings;

namespace Memorizer.Services;

public enum WebSearchProvider
{
    Google,
    Bing,
    Naver
}

public sealed record WebSearchItem(
    string Title,
    string Url,
    string Snippet,
    WebSearchProvider Provider);

public sealed record WebSearchResponse(
    WebSearchProvider Provider,
    string Query,
    IReadOnlyList<WebSearchItem> Items);

public sealed record PageReadResponse(
    string Url,
    string? Title,
    string ContentPreview,
    int StatusCode,
    DateTime RetrievedAtUtc);

public sealed record WebSearchPreviewResponse(
    WebSearchProvider Provider,
    string Query,
    WebSearchItem? TopResult,
    PageReadResponse? TopPage);

public interface IWebSearchService
{
    Task<WebSearchResponse> SearchAsync(
        WebSearchProvider provider,
        string query,
        int maxResults = 5,
        WebSearchAccessMode? accessMode = null,
        CancellationToken cancellationToken = default);

    Task<PageReadResponse> ReadPageAsync(
        string url,
        WebSearchAccessMode? accessMode = null,
        CancellationToken cancellationToken = default);

    Task<WebSearchPreviewResponse> SearchAndReadTopResultAsync(
        WebSearchProvider provider,
        string query,
        int maxResults = 5,
        WebSearchAccessMode? accessMode = null,
        CancellationToken cancellationToken = default);
}
