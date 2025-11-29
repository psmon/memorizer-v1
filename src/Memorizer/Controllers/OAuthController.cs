using Memorizer.Services;
using Memorizer.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Memorizer.Controllers;

[ApiController]
public class OAuthController : ControllerBase
{
    private readonly IOAuthTokenService _tokenService;
    private readonly OAuthSettings _settings;
    private readonly ServerSettings _serverSettings;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IOAuthTokenService tokenService,
        OAuthSettings settings,
        ServerSettings serverSettings,
        ILogger<OAuthController> logger)
    {
        _tokenService = tokenService;
        _settings = settings;
        _serverSettings = serverSettings;
        _logger = logger;
    }

    /// <summary>
    /// OAuth 2.0 Token endpoint - supports client_credentials and authorization_code grant types
    /// </summary>
    [HttpPost("/oauth/token")]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult Token([FromForm] OAuthTokenRequest request)
    {
        if (!_tokenService.IsEnabled)
        {
            return BadRequest(new OAuthErrorResponse
            {
                Error = "server_error",
                ErrorDescription = "OAuth is not enabled on this server"
            });
        }

        _logger.LogInformation("OAuth token request received. GrantType: {GrantType}, ClientId: {ClientId}",
            request.GrantType, request.ClientId);

        return request.GrantType?.ToLower() switch
        {
            "client_credentials" => HandleClientCredentials(request),
            "authorization_code" => HandleAuthorizationCode(request),
            _ => BadRequest(new OAuthErrorResponse
            {
                Error = "unsupported_grant_type",
                ErrorDescription = $"Grant type '{request.GrantType}' is not supported"
            })
        };
    }

    private IActionResult HandleClientCredentials(OAuthTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
        {
            return BadRequest(new OAuthErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = "client_id and client_secret are required"
            });
        }

        var tokenResponse = _tokenService.GenerateToken(request.ClientId, request.ClientSecret, request.Scope);
        if (tokenResponse == null)
        {
            return Unauthorized(new OAuthErrorResponse
            {
                Error = "invalid_client",
                ErrorDescription = "Invalid client credentials"
            });
        }

        return Ok(new
        {
            access_token = tokenResponse.AccessToken,
            token_type = tokenResponse.TokenType,
            expires_in = tokenResponse.ExpiresIn,
            scope = tokenResponse.Scope
        });
    }

    private IActionResult HandleAuthorizationCode(OAuthTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.Code))
        {
            return BadRequest(new OAuthErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = "Authorization code is required"
            });
        }

        if (string.IsNullOrEmpty(request.ClientId))
        {
            return BadRequest(new OAuthErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = "client_id is required"
            });
        }

        var tokenResponse = _tokenService.GenerateTokenByCode(request.Code, request.ClientId, request.CodeVerifier);
        if (tokenResponse == null)
        {
            return Unauthorized(new OAuthErrorResponse
            {
                Error = "invalid_grant",
                ErrorDescription = "Invalid authorization code or code verifier"
            });
        }

        return Ok(new
        {
            access_token = tokenResponse.AccessToken,
            token_type = tokenResponse.TokenType,
            expires_in = tokenResponse.ExpiresIn,
            scope = tokenResponse.Scope
        });
    }

    /// <summary>
    /// OAuth 2.0 Authorization endpoint - for authorization code flow
    /// </summary>
    [HttpGet("/oauth/authorize")]
    public IActionResult Authorize(
        [FromQuery(Name = "response_type")] string responseType,
        [FromQuery(Name = "client_id")] string clientId,
        [FromQuery(Name = "redirect_uri")] string? redirectUri,
        [FromQuery(Name = "scope")] string? scope,
        [FromQuery(Name = "state")] string? state,
        [FromQuery(Name = "code_challenge")] string? codeChallenge,
        [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod)
    {
        if (!_tokenService.IsEnabled)
        {
            return BadRequest(new OAuthErrorResponse
            {
                Error = "server_error",
                ErrorDescription = "OAuth is not enabled on this server"
            });
        }

        if (responseType != "code")
        {
            return BadRequest(new OAuthErrorResponse
            {
                Error = "unsupported_response_type",
                ErrorDescription = "Only 'code' response type is supported"
            });
        }

        // Validate client exists
        var client = _settings.Clients.FirstOrDefault(c => c.ClientId == clientId);
        if (client == null)
        {
            return BadRequest(new OAuthErrorResponse
            {
                Error = "invalid_client",
                ErrorDescription = "Unknown client_id"
            });
        }

        // For PKCE, validate code_challenge_method
        if (!string.IsNullOrEmpty(codeChallenge) && codeChallengeMethod != "S256" && codeChallengeMethod != "plain")
        {
            return BadRequest(new OAuthErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = "Invalid code_challenge_method. Use 'S256' or 'plain'"
            });
        }

        // Validate redirect_uri if provided and client has allowed URIs configured
        if (!string.IsNullOrEmpty(redirectUri) && client.AllowedRedirectUris.Count > 0)
        {
            var isAllowed = client.AllowedRedirectUris.Any(allowed =>
                redirectUri.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                _logger.LogWarning("Invalid redirect_uri: {RedirectUri} for client: {ClientId}", redirectUri, clientId);
                return BadRequest(new OAuthErrorResponse
                {
                    Error = "invalid_request",
                    ErrorDescription = "redirect_uri is not allowed for this client"
                });
            }
        }

        // Generate authorization code
        var code = _tokenService.GenerateAuthorizationCode(clientId, codeChallenge, codeChallengeMethod, scope);

        // If redirect_uri is provided, redirect with code
        if (!string.IsNullOrEmpty(redirectUri))
        {
            var uri = new UriBuilder(redirectUri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            query["code"] = code;
            if (!string.IsNullOrEmpty(state))
            {
                query["state"] = state;
            }
            uri.Query = query.ToString();

            _logger.LogInformation("Redirecting authorization code to: {RedirectUri}", uri.Uri);
            return Redirect(uri.Uri.ToString());
        }

        // If no redirect_uri, return code in response (for testing)
        return Ok(new
        {
            code,
            state
        });
    }

    /// <summary>
    /// OAuth 2.0 Authorization Server Metadata (RFC 8414)
    /// Required for ChatGPT MCP integration
    /// </summary>
    [HttpGet("/.well-known/oauth-authorization-server")]
    public IActionResult AuthorizationServerMetadata()
    {
        var baseUrl = GetBaseUrl();

        // Collect all allowed redirect URIs from all clients
        var allRedirectUris = _settings.Clients
            .SelectMany(c => c.AllowedRedirectUris)
            .Distinct()
            .ToArray();

        return Ok(new
        {
            issuer = _settings.Issuer,
            authorization_endpoint = $"{baseUrl}/oauth/authorize",
            token_endpoint = $"{baseUrl}/oauth/token",
            token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" },
            grant_types_supported = new[] { "authorization_code", "client_credentials" },
            response_types_supported = new[] { "code" },
            scopes_supported = new[] { "mcp:read", "mcp:write" },
            code_challenge_methods_supported = new[] { "S256", "plain" },
            redirect_uris_supported = allRedirectUris.Length > 0 ? allRedirectUris : null,
            service_documentation = $"{baseUrl}/swagger"
        });
    }

    /// <summary>
    /// OAuth Protected Resource Metadata (RFC 9449)
    /// Required for ChatGPT MCP integration
    /// </summary>
    [HttpGet("/.well-known/oauth-protected-resource")]
    public IActionResult ProtectedResourceMetadata()
    {
        var baseUrl = GetBaseUrl();

        return Ok(new
        {
            resource = baseUrl,
            authorization_servers = new[] { baseUrl },
            bearer_methods_supported = new[] { "header" },
            scopes_supported = new[] { "mcp:read", "mcp:write" }
        });
    }

    /// <summary>
    /// OpenID Configuration (for compatibility)
    /// </summary>
    [HttpGet("/.well-known/openid-configuration")]
    public IActionResult OpenIdConfiguration()
    {
        var baseUrl = GetBaseUrl();

        return Ok(new
        {
            issuer = _settings.Issuer,
            authorization_endpoint = $"{baseUrl}/oauth/authorize",
            token_endpoint = $"{baseUrl}/oauth/token",
            jwks_uri = $"{baseUrl}/.well-known/jwks.json",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "client_credentials" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "HS256" },
            scopes_supported = new[] { "mcp:read", "mcp:write" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" },
            code_challenge_methods_supported = new[] { "S256", "plain" }
        });
    }

    private string GetBaseUrl()
    {
        if (!string.IsNullOrEmpty(_serverSettings.CanonicalUrl))
        {
            return _serverSettings.CanonicalUrl.TrimEnd('/');
        }

        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}";
    }
}

public class OAuthTokenRequest
{
    [FromForm(Name = "grant_type")]
    public string? GrantType { get; set; }

    [FromForm(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromForm(Name = "client_secret")]
    public string? ClientSecret { get; set; }

    [FromForm(Name = "code")]
    public string? Code { get; set; }

    [FromForm(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [FromForm(Name = "code_verifier")]
    public string? CodeVerifier { get; set; }

    [FromForm(Name = "scope")]
    public string? Scope { get; set; }
}

public class OAuthErrorResponse
{
    public string Error { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
