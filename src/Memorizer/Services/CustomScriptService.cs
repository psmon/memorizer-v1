using Memorizer.Models;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace Memorizer.Services;

public class CustomScriptService : ICustomScriptService
{
    private readonly string _connectionString;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CustomScriptService> _logger;
    private const string CacheKey = "active_custom_scripts";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public CustomScriptService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<CustomScriptService> logger)
    {
        _connectionString = configuration.GetConnectionString("Storage")
            ?? throw new InvalidOperationException("Storage connection string not configured");
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetActiveScriptsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out string? cachedScripts) && cachedScripts != null)
        {
            _logger.LogDebug("Returning cached active scripts");
            return cachedScripts;
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = """
            SELECT script_content
            FROM custom_scripts
            WHERE is_active = true
            ORDER BY created_at ASC
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        var scripts = new List<string>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            scripts.Add(reader.GetString(0));
        }

        var combinedScripts = string.Join("\n", scripts);

        _cache.Set(CacheKey, combinedScripts, CacheDuration);
        _logger.LogInformation("Loaded {Count} active scripts and cached for {Duration}", scripts.Count, CacheDuration);

        return combinedScripts;
    }

    public async Task<IEnumerable<CustomScript>> GetAllScriptsAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = """
            SELECT id, name, script_content, is_active, created_at, updated_at
            FROM custom_scripts
            ORDER BY created_at DESC
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        var scripts = new List<CustomScript>();

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            scripts.Add(new CustomScript
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ScriptContent = reader.GetString(2),
                IsActive = reader.GetBoolean(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5)
            });
        }

        return scripts;
    }

    public async Task<CustomScript?> GetScriptByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = """
            SELECT id, name, script_content, is_active, created_at, updated_at
            FROM custom_scripts
            WHERE name = @name
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@name", name);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new CustomScript
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ScriptContent = reader.GetString(2),
                IsActive = reader.GetBoolean(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5)
            };
        }

        return null;
    }

    public async Task<CustomScript> CreateOrUpdateScriptAsync(
        string name,
        string scriptContent,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = """
            INSERT INTO custom_scripts (name, script_content, is_active, updated_at)
            VALUES (@name, @scriptContent, @isActive, now())
            ON CONFLICT (name)
            DO UPDATE SET
                script_content = EXCLUDED.script_content,
                is_active = EXCLUDED.is_active,
                updated_at = now()
            RETURNING id, name, script_content, is_active, created_at, updated_at
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@scriptContent", scriptContent);
        cmd.Parameters.AddWithValue("@isActive", isActive);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        var script = new CustomScript
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            ScriptContent = reader.GetString(2),
            IsActive = reader.GetBoolean(3),
            CreatedAt = reader.GetDateTime(4),
            UpdatedAt = reader.GetDateTime(5)
        };

        _cache.Remove(CacheKey);
        _logger.LogInformation("Script '{Name}' created/updated. Cache invalidated.", name);

        return script;
    }

    public async Task<bool> DeleteScriptAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = "DELETE FROM custom_scripts WHERE name = @name";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@name", name);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

        if (rowsAffected > 0)
        {
            _cache.Remove(CacheKey);
            _logger.LogInformation("Script '{Name}' deleted. Cache invalidated.", name);
            return true;
        }

        return false;
    }
}