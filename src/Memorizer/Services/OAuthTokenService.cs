using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Memorizer.Settings;
using Microsoft.IdentityModel.Tokens;

namespace Memorizer.Services;

public interface IOAuthTokenService
{
    bool IsEnabled { get; }
    OAuthTokenResponse? GenerateToken(string clientId, string clientSecret, string? scope = null);
    OAuthTokenResponse? GenerateTokenByCode(string code, string clientId, string? codeVerifier = null);
    ClaimsPrincipal? ValidateToken(string token);
    OAuthClient? ValidateClient(string clientId, string clientSecret);
    string GenerateAuthorizationCode(string clientId, string? codeChallenge = null, string? codeChallengeMethod = null, string? scope = null);
}

public class OAuthTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
    public string? Scope { get; init; }
}

public class AuthorizationCodeInfo
{
    public string ClientId { get; init; } = string.Empty;
    public string? CodeChallenge { get; init; }
    public string? CodeChallengeMethod { get; init; }
    public string? Scope { get; init; }
    public DateTime ExpiresAt { get; init; }
}

public class OAuthTokenService : IOAuthTokenService
{
    private readonly OAuthSettings _settings;
    private readonly ILogger<OAuthTokenService> _logger;
    private readonly Dictionary<string, AuthorizationCodeInfo> _authorizationCodes = new();
    private readonly object _lock = new();

    public OAuthTokenService(OAuthSettings settings, ILogger<OAuthTokenService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public bool IsEnabled => _settings.Enabled;

    public OAuthClient? ValidateClient(string clientId, string clientSecret)
    {
        var client = _settings.Clients.FirstOrDefault(c =>
            c.ClientId == clientId && c.ClientSecret == clientSecret);

        if (client == null)
        {
            _logger.LogWarning("Invalid client credentials for clientId: {ClientId}", clientId);
        }

        return client;
    }

    public OAuthTokenResponse? GenerateToken(string clientId, string clientSecret, string? scope = null)
    {
        var client = ValidateClient(clientId, clientSecret);
        if (client == null)
        {
            return null;
        }

        return CreateTokenResponse(clientId, client.AllowedScopes, scope);
    }

    public string GenerateAuthorizationCode(string clientId, string? codeChallenge = null, string? codeChallengeMethod = null, string? scope = null)
    {
        var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var codeInfo = new AuthorizationCodeInfo
        {
            ClientId = clientId,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            Scope = scope,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        lock (_lock)
        {
            CleanupExpiredCodes();
            _authorizationCodes[code] = codeInfo;
        }

        _logger.LogInformation("Generated authorization code for clientId: {ClientId}", clientId);
        return code;
    }

    public OAuthTokenResponse? GenerateTokenByCode(string code, string clientId, string? codeVerifier = null)
    {
        AuthorizationCodeInfo? codeInfo;

        lock (_lock)
        {
            if (!_authorizationCodes.TryGetValue(code, out codeInfo))
            {
                _logger.LogWarning("Invalid or expired authorization code");
                return null;
            }

            _authorizationCodes.Remove(code);
        }

        if (codeInfo.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Authorization code expired");
            return null;
        }

        if (codeInfo.ClientId != clientId)
        {
            _logger.LogWarning("Client ID mismatch for authorization code");
            return null;
        }

        // Verify PKCE code challenge if present
        if (!string.IsNullOrEmpty(codeInfo.CodeChallenge))
        {
            if (string.IsNullOrEmpty(codeVerifier))
            {
                _logger.LogWarning("Code verifier required but not provided");
                return null;
            }

            var isValid = VerifyCodeChallenge(codeVerifier, codeInfo.CodeChallenge, codeInfo.CodeChallengeMethod);
            if (!isValid)
            {
                _logger.LogWarning("Code verifier validation failed");
                return null;
            }
        }

        var client = _settings.Clients.FirstOrDefault(c => c.ClientId == clientId);
        if (client == null)
        {
            _logger.LogWarning("Client not found for authorization code exchange");
            return null;
        }

        return CreateTokenResponse(clientId, client.AllowedScopes, codeInfo.Scope);
    }

    private OAuthTokenResponse CreateTokenResponse(string clientId, List<string> allowedScopes, string? requestedScope)
    {
        var scopes = ParseAndValidateScopes(requestedScope, allowedScopes);
        var expirationSeconds = _settings.TokenExpirationHours * 3600;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, clientId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("client_id", clientId)
        };

        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(expirationSeconds),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation("Generated access token for clientId: {ClientId}, scopes: {Scopes}",
            clientId, string.Join(" ", scopes));

        return new OAuthTokenResponse
        {
            AccessToken = tokenString,
            TokenType = "Bearer",
            ExpiresIn = expirationSeconds,
            Scope = string.Join(" ", scopes)
        };
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_settings.SecretKey);

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken)
            {
                _logger.LogDebug("Token validated successfully for client: {ClientId}",
                    jwtToken.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value);
            }

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token has expired");
            return null;
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    private List<string> ParseAndValidateScopes(string? requestedScope, List<string> allowedScopes)
    {
        if (string.IsNullOrWhiteSpace(requestedScope))
        {
            return allowedScopes;
        }

        var requested = requestedScope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return requested.Where(s => allowedScopes.Contains(s)).ToList();
    }

    private bool VerifyCodeChallenge(string codeVerifier, string codeChallenge, string? method)
    {
        if (method == "S256")
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
            var computed = Convert.ToBase64String(hash)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
            return computed == codeChallenge;
        }
        else if (method == "plain" || string.IsNullOrEmpty(method))
        {
            return codeVerifier == codeChallenge;
        }

        return false;
    }

    private void CleanupExpiredCodes()
    {
        var expiredCodes = _authorizationCodes
            .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var code in expiredCodes)
        {
            _authorizationCodes.Remove(code);
        }
    }
}
