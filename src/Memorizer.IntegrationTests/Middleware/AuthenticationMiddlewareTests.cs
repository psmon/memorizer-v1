using System.Diagnostics.CodeAnalysis;
using System.Text;
using Memorizer.Middleware;
using Memorizer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Moq;

namespace Memorizer.IntegrationTests.Middleware;

/// <summary>
/// Admin operations under /ui/tools (embedding sync, title generation, graph sync) and
/// POST /api/graph/sync require login; viewing /ui pages and public generators stay open.
/// </summary>
public sealed class AuthenticationMiddlewareTests
{
    [Theory]
    [InlineData("POST", "/ui/tools/start-metadata-embedding-all")]
    [InlineData("POST", "/ui/tools/start-metadata-embedding")]
    [InlineData("POST", "/ui/tools/start-title-generation")]
    [InlineData("POST", "/ui/tools/graph-sync/start")]
    [InlineData("POST", "/ui/tools/graph-sync/initialize-schema")]
    [InlineData("POST", "/api/graph/sync")]
    public async Task AdminOperation_WithoutLogin_Returns401(string method, string path)
    {
        var (status, nextCalled) = await InvokeAsync(method, path, authenticated: false);

        Assert.Equal(StatusCodes.Status401Unauthorized, status);
        Assert.False(nextCalled);
    }

    [Theory]
    [InlineData("POST", "/ui/tools/start-metadata-embedding-all")]
    [InlineData("POST", "/api/graph/sync")]
    public async Task AdminOperation_WithLogin_IsAllowed(string method, string path)
    {
        var (_, nextCalled) = await InvokeAsync(method, path, authenticated: true);

        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData("GET", "/ui/tools")]
    [InlineData("GET", "/ui/tools/metadata-embedding-status")]
    [InlineData("POST", "/api/askbot/message")]
    [InlineData("POST", "/ui/prd/generate-title")]
    [InlineData("POST", "/api/graph/search")]
    public async Task PublicPaths_WithoutLogin_StayOpen(string method, string path)
    {
        var (_, nextCalled) = await InvokeAsync(method, path, authenticated: false);

        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData("POST", "/api/memory")]
    [InlineData("DELETE", "/api/memory/00000000-0000-0000-0000-000000000000")]
    public async Task MemoryWrites_WithoutLogin_Return401(string method, string path)
    {
        var (status, nextCalled) = await InvokeAsync(method, path, authenticated: false);

        Assert.Equal(StatusCodes.Status401Unauthorized, status);
        Assert.False(nextCalled);
    }

    private static async Task<(int Status, bool NextCalled)> InvokeAsync(string method, string path, bool authenticated)
    {
        var nextCalled = false;
        var middleware = new AuthenticationMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            Mock.Of<ILogger<AuthenticationMiddleware>>());

        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.Features.Set<ISessionFeature>(new TestSessionFeature(authenticated));

        await middleware.InvokeAsync(context,
            Mock.Of<Memorizer.Services.IAuthenticationService>(),
            Mock.Of<IOAuthTokenService>());

        return (context.Response.StatusCode, nextCalled);
    }

    private sealed class TestSessionFeature(bool authenticated) : ISessionFeature
    {
        public ISession Session { get; set; } = new TestSession(authenticated);
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public TestSession(bool authenticated)
        {
            if (authenticated)
            {
                _values["IsAuthenticated"] = Encoding.UTF8.GetBytes("true");
                _values["Username"] = Encoding.UTF8.GetBytes("admin");
            }
        }

        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => _values.Keys;
        public void Clear() => _values.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _values.Remove(key);
        public void Set(string key, byte[] value) => _values[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _values.TryGetValue(key, out value);
    }
}
