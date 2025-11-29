using Configuration.Extensions.EnvironmentFile;
using Memorizer.Extensions;
using Memorizer.Middleware;
using Memorizer.Services;
using Memorizer.Telemetry;
using PostgMem.Tools;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Logging.Console;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .Configuration
    .AddEnvironmentFile() 
    .AddEnvironmentVariables("MEMORIZER_");

var resourceBuilder = ResourceBuilder.CreateDefault();
resourceBuilder
    .AddEnvironmentVariableDetector()
    .AddTelemetrySdk()
    .AddServiceVersionDetector();

// Enhanced logging configuration for detailed debugging
builder.Logging.ClearProviders();
builder.Logging.AddConsole().AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
});

// Set specific log levels for detailed SSE and HTTP debugging
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Debug);
builder.Logging.AddFilter("System.Net.Http", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore.Cors", LogLevel.Debug);

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("Storage") ?? 
    throw new InvalidOperationException("Missing Storage connection string");

// Enhanced debug logging
var logger = LoggerFactory.Create(b =>
{
    b.AddConsole().AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
    });
    b.SetMinimumLevel(LogLevel.Trace);
}).CreateLogger("Startup-Debug");

logger.LogInformation("=== APPLICATION STARTUP DEBUG ===");
logger.LogInformation("OTEL_EXPORTER_OTLP_ENDPOINT: {Endpoint}", Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));
logger.LogInformation("OTEL_SERVICE_NAME: {ServiceName}", Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME"));
logger.LogInformation("OTEL_RESOURCE_ATTRIBUTES: {ResourceAttributes}", Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES"));
logger.LogInformation("ASPNETCORE_ENVIRONMENT: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
logger.LogInformation("ASPNETCORE_URLS: {Urls}", Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add authentication services
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

// Add custom script service
builder.Services.AddScoped<ICustomScriptService, CustomScriptService>();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".Memorizer.Session";
});

// Add services
builder.Services.AddMemorizer();
builder.Services.AddMemorizerOtel();
builder.Services.AddMcpServer().WithHttpTransport().WithTools<MemoryTools>();

// Add MVC support for web UI
builder.Services.AddControllersWithViews();

// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Memorizer LLM API",
        Version = "v1",
        Description = "OpenAI-compatible LLM API endpoints"
    });

    // Only include LLMController endpoints in Swagger
    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
        return controllerName == "LLM";
    });
});

// Configure routing options for lowercase URLs
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name: "postgres",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "postgres", "required"]);

WebApplication app = builder.Build();

// Create application logger for runtime debugging
var appLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Application");

// Initialize ImageStoragePath and create access.txt file
var imageStoragePath = app.Configuration["AskBot:ImageStoragePath"];
if (!string.IsNullOrEmpty(imageStoragePath))
{
    try
    {
        // Create directory if it doesn't exist
        if (!Directory.Exists(imageStoragePath))
        {
            Directory.CreateDirectory(imageStoragePath);
            appLogger.LogInformation("Created ImageStoragePath directory: {Path}", imageStoragePath);
        }

        // Create access.txt file
        var accessFilePath = Path.Combine(imageStoragePath, "access.txt");
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var accessMessage = $"Application started at {timestamp} UTC\n";

        File.WriteAllText(accessFilePath, accessMessage);
        appLogger.LogInformation("Created access.txt file at: {Path}", accessFilePath);
    }
    catch (Exception ex)
    {
        appLogger.LogError(ex, "Failed to initialize ImageStoragePath or create access.txt: {Error}", ex.Message);
    }
}
else
{
    appLogger.LogWarning("AskBot:ImageStoragePath not configured in appsettings");
}

// Enhanced request logging middleware
app.Use(async (context, next) =>
{
    var startTime = DateTime.UtcNow;
    appLogger.LogInformation("=== REQUEST START ===");
    appLogger.LogInformation("Method: {Method}, Path: {Path}, QueryString: {QueryString}",
        context.Request.Method, context.Request.Path, context.Request.QueryString);
    appLogger.LogInformation("Headers: {Headers}",
        string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}")));
    appLogger.LogInformation("ContentType: {ContentType}, ContentLength: {ContentLength}",
        context.Request.ContentType, context.Request.ContentLength);

    try
    {
        await next();

        var duration = DateTime.UtcNow - startTime;
        appLogger.LogInformation("=== REQUEST END ===");
        appLogger.LogInformation("Status: {StatusCode}, Duration: {Duration}ms",
            context.Response.StatusCode, duration.TotalMilliseconds);
        appLogger.LogInformation("Response Headers: {Headers}",
            string.Join(", ", context.Response.Headers.Select(h => $"{h.Key}={h.Value}")));
    }
    catch (Exception ex)
    {
        var duration = DateTime.UtcNow - startTime;
        appLogger.LogError(ex, "=== REQUEST FAILED ===");
        appLogger.LogError("Method: {Method}, Path: {Path}, Duration: {Duration}ms, Error: {Error}",
            context.Request.Method, context.Request.Path, duration.TotalMilliseconds, ex.Message);
        throw;
    }
});

// CORS middleware with logging
app.Use(async (context, next) =>
{
    if (context.Request.Headers.ContainsKey("Origin"))
    {
        var origin = context.Request.Headers["Origin"].ToString();
        appLogger.LogDebug("CORS request detected. Origin: {Origin}", origin);
    }

    await next();

    if (context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
    {
        var allowOrigin = context.Response.Headers["Access-Control-Allow-Origin"].ToString();
        appLogger.LogDebug("CORS headers added. Access-Control-Allow-Origin: {Origin}", allowOrigin);
    }
});

app.UseCors();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Memorizer API v1");
    options.RoutePrefix = "swagger"; // Access at /swagger
});

app.UseStaticFiles();

// Add session middleware
app.UseSession();

// Add root redirect middleware before routing
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && context.Request.Method == "GET")
    {
        context.Response.Redirect("/ui/blog");
        return;
    }
    await next();
});

// Add authentication middleware
app.UseMiddleware<AuthenticationMiddleware>();

// Add logging before and after MCP mapping
appLogger.LogInformation("Mapping MCP endpoints...");
try
{
    // Map MCP endpoints:
    // - Streamable HTTP at / (POST) for ChatGPT, newer clients
    // - Legacy SSE at /sse (GET) for Claude Desktop, existing MCP clients
    // - Legacy message at /message (POST) for SSE message handling
    app.MapMcp();
    appLogger.LogInformation("MCP endpoints mapped successfully (Streamable HTTP at POST /, Legacy SSE at /sse, /message)");
}
catch (Exception ex)
{
    appLogger.LogError(ex, "Failed to map MCP endpoints: {Error}", ex.Message);
    throw;
}

// Configure health check endpoints
app.MapHealthChecks("/healthz");

// Add OTEL test endpoint
app.MapGet("/otel-test", () =>
{
    using var activity = TelemetryConfig.ActivitySource.StartActivity("test-activity");
    activity?.SetTag("test.tag", "test-value");
    activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
    
    var testLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("OtelTest");
    testLogger.LogInformation("OTEL test endpoint called - this should appear in collector logs");
    
    return Results.Ok(new { message = "OTEL test completed", activityId = activity?.Id });
});

// Add SSE connection test endpoint for debugging
app.MapGet("/sse-test", async (HttpContext context) =>
{
    var sseLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SSE-Test");
    sseLogger.LogInformation("SSE test endpoint called");

    context.Response.Headers["Cache-Control"] = "no-cache";
    context.Response.Headers["Connection"] = "keep-alive";
    context.Response.ContentType = "text/event-stream";

    sseLogger.LogDebug("SSE headers set");

    try
    {
        for (int i = 0; i < 5; i++)
        {
            var data = $"data: Test message {i + 1} at {DateTime.UtcNow:HH:mm:ss.fff}\n\n";
            await context.Response.WriteAsync(data);
            await context.Response.Body.FlushAsync();
            sseLogger.LogDebug("SSE message {MessageNumber} sent", i + 1);
            await Task.Delay(1000);
        }

        sseLogger.LogInformation("SSE test completed successfully");
    }
    catch (Exception ex)
    {
        sseLogger.LogError(ex, "SSE test failed: {Error}", ex.Message);
        throw;
    }
});

// Configure default MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();