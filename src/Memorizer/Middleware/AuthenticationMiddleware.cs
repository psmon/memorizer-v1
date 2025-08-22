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

    public async Task InvokeAsync(HttpContext context, IAuthenticationService authService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method;

        _logger.LogDebug("Authentication check for path: {Path}, Method: {Method}", path, method);

        // Check if this is an SSE endpoint requiring API key
        if (path == "/sse" || path.StartsWith("/mcp"))
        {
            var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault() ??
                        context.Request.Query["apikey"].FirstOrDefault();

            if (string.IsNullOrEmpty(apiKey) || !authService.ValidateApiKey(apiKey))
            {
                _logger.LogWarning("Invalid API key for SSE endpoint access");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid API Key");
                return;
            }
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
                // For API endpoints, only GET is public
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