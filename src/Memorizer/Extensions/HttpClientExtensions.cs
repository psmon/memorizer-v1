using System.Net.Http.Headers;

namespace Memorizer.Extensions;

public static class HttpClientExtensions
{
    /// <summary>
    /// Applies OpenAI-compatible API key authentication (Authorization: Bearer {apiKey}).
    /// LM Studio and other OpenAI-compatible servers use the same scheme.
    /// No header is added when the key is empty, so servers without authentication keep working.
    /// </summary>
    public static HttpClient UseBearerApiKey(this HttpClient httpClient, string? apiKey)
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        }

        return httpClient;
    }
}
