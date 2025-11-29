namespace Memorizer.Settings;

public class OAuthSettings
{
    public bool Enabled { get; init; } = false;
    public string Issuer { get; init; } = "memorizer";
    public string Audience { get; init; } = "memorizer-api";
    public string SecretKey { get; init; } = string.Empty;
    public int TokenExpirationHours { get; init; } = 24;
    public List<OAuthClient> Clients { get; init; } = new();
}

public class OAuthClient
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public List<string> AllowedScopes { get; init; } = new();
    public List<string> AllowedRedirectUris { get; init; } = new();
}
