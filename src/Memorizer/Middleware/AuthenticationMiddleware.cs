using Memorizer.Services;

namespace Memorizer.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthenticationService authService, IOAuthTokenService oauthService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method;

        _logger.LogDebug("Authentication check for path: {Path}, Method: {Method}", path, method);

        // Skip authentication for OAuth endpoints (they handle their own auth)
        if (path.StartsWith("/oauth/") || path.StartsWith("/.well-known/"))
        {
            await _next(context);
            return;
        }

        // Check if this is an MCP endpoint requiring authentication
        // MCP endpoints:
        // - POST / : Streamable HTTP (for ChatGPT, newer clients)
        // - GET /sse : Legacy SSE connection (for Claude Desktop, existing MCP clients)
        // - POST /message : Legacy SSE message handling
        var isMcpEndpoint = path == "/sse" || path == "/message" ||
                           (path == "/" && method == "POST");

        if (isMcpEndpoint)
        {
            // Priority 1: Check for API Key (X-API-Key header or apikey query parameter)
            var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault() ??
                        context.Request.Query["apikey"].FirstOrDefault();

            if (!string.IsNullOrEmpty(apiKey))
            {
                if (authService.ValidateApiKey(apiKey))
                {
                    _logger.LogDebug("API Key validated successfully for MCP endpoint");
                    context.Items["AuthenticatedViaApiKey"] = true;
                    await _next(context);
                    return;
                }
                else
                {
                    _logger.LogWarning("Invalid API key for MCP endpoint: {ApiKey}", apiKey);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized: Invalid API Key");
                    return;
                }
            }

            // Priority 2: Fallback to Bearer token (OAuth 2.0) if no API Key provided
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();

                if (oauthService.IsEnabled)
                {
                    var principal = oauthService.ValidateToken(token);
                    if (principal != null)
                    {
                        _logger.LogDebug("OAuth Bearer token validated successfully for MCP endpoint");
                        context.Items["AuthenticatedViaOAuth"] = true;
                        context.Items["OAuthClientId"] = principal.FindFirst("client_id")?.Value;
                        await _next(context);
                        return;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid OAuth Bearer token for MCP endpoint");
                        context.Response.StatusCode = 401;
                        context.Response.Headers["WWW-Authenticate"] = "Bearer error=\"invalid_token\"";
                        await context.Response.WriteAsync("Unauthorized: Invalid Bearer token");
                        return;
                    }
                }
            }

            // No valid authentication provided
            _logger.LogWarning("No valid authentication for MCP endpoint");
            context.Response.StatusCode = 401;
            if (oauthService.IsEnabled)
            {
                context.Response.Headers["WWW-Authenticate"] = "Bearer";
            }
            await context.Response.WriteAsync("Unauthorized: X-API-Key or Bearer token required");
            return;
        }

        // Skip authentication requirement for /api/askbot, /ui/askbot, and /api/llm paths
        // But still set authentication status based on session
        if (path.StartsWith("/api/askbot") || path.StartsWith("/ui/askbot") || path.StartsWith("/api/llm"))
        {
            _logger.LogDebug("Public API path (authentication optional): {Path}", path);

            // Set authentication status from session (but don't require it)
            var isAuthenticated = context.Session.GetString("IsAuthenticated") == "true";
            context.Items["IsAuthenticated"] = isAuthenticated;
            context.Items["Username"] = isAuthenticated ? context.Session.GetString("Username") : null;

            await _next(context);
            return;
        }

        // Check if path requires authentication
        if (RequiresAuthentication(path, method))
        {
            var isAuthenticated = context.Session.GetString("IsAuthenticated") == "true";

            if (!isAuthenticated)
            {
                _logger.LogInformation("Unauthenticated access attempt to protected route: {Path}", path);
                
                // For API calls, return 401
                if (path.StartsWith("/api/"))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
                
                // For UI routes, redirect to login
                context.Response.Redirect("/");
                return;
            }
        }

        // Set authentication status in Items for use in views
        context.Items["IsAuthenticated"] = context.Session.GetString("IsAuthenticated") == "true";
        context.Items["Username"] = context.Session.GetString("Username");

        await _next(context);
    }

    private bool RequiresAuthentication(string path, string method)
    {
        // Public paths that don't require authentication
        var publicPaths = new[]
        {
            "/",
            "/auth/login",
            "/login",
            "/logout",
            "/ui",
            "/ui/",
            "/ui/graph",
            "/ui/view/",
            "/api/memory", // GET only
            "/api/memory/search",
            "/api/memory/types",
            "/api/graph/memories",
            "/api/graph/search",
            "/api/graph/search/cypher",
            "/api/graph/sync",
            "/api/askbot",  // AskBot endpoints are public
            "/ui/askbot",   // AskBot UI is public
            "/api/llm",     // LLM API endpoints are public
            "/healthz",
            "/sse-test",
            "/otel-test"
        };

        // Check if it's a public path
        foreach (var publicPath in publicPaths)
        {
            if (path == publicPath || 
                (publicPath.EndsWith("/") && path.StartsWith(publicPath)))
            {
                // For /api/graph/, POST is public for search and sync
                if ((path.StartsWith("/api/graph/search") || path == "/api/graph/sync") && method == "POST")
                {
                    return false;
                }

                // For /api/askbot/, all methods are public (SSE, POST for messages)
                if (path.StartsWith("/api/askbot"))
                {
                    return false;
                }

                // For /api/llm/, all methods are public (POST for completions, GET for health)
                if (path.StartsWith("/api/llm"))
                {
                    return false;
                }

                // For other API endpoints, only GET is public
                if (path.StartsWith("/api/") && method != "GET")
                {
                    return true;
                }
                return false;
            }
        }

        // Special case for /api/memory/{id} - GET is public
        if (path.StartsWith("/api/memory/") && method == "GET" && 
            !path.Contains("/edit") && !path.Contains("/delete"))
        {
            return false;
        }

        // All other paths require authentication
        return true;
    }
}