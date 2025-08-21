using Neo4j.Driver;
using System.Collections.Generic;

namespace Memorizer.Services;

public interface IGraphRepository
{
    Task<T> ExecuteReadAsync<T>(Func<IAsyncQueryRunner, Task<T>> func, string? database = null);
    Task<T> ExecuteWriteAsync<T>(Func<IAsyncQueryRunner, Task<T>> func, string? database = null);
    Task ExecuteWriteAsync(Func<IAsyncQueryRunner, Task> func, string? database = null);
    Task<List<IRecord>> RunQueryAsync(string cypher, object? parameters = null, bool isWrite = false);
    Task<bool> TestConnectionAsync();
}

public sealed class GraphRepository : IGraphRepository
{
    private readonly INeo4jDriverFactory _driverFactory;
    private readonly ILogger<GraphRepository> _logger;
    
    public GraphRepository(INeo4jDriverFactory driverFactory, ILogger<GraphRepository> logger)
    {
        _driverFactory = driverFactory;
        _logger = logger;
    }
    
    public async Task<T> ExecuteReadAsync<T>(Func<IAsyncQueryRunner, Task<T>> func, string? database = null)
    {
        var driver = _driverFactory.GetDriver();
        Action<SessionConfigBuilder>? sessionConfig = database != null 
            ? o => o.WithDatabase(database)
            : null;
            
        await using var session = driver.AsyncSession(sessionConfig);
        
        try
        {
            return await session.ExecuteReadAsync(func);
        }
        catch (Neo4jException ex)
        {
            _logger.LogError(ex, "Error executing read transaction");
            throw;
        }
    }
    
    public async Task<T> ExecuteWriteAsync<T>(Func<IAsyncQueryRunner, Task<T>> func, string? database = null)
    {
        var driver = _driverFactory.GetDriver();
        Action<SessionConfigBuilder>? sessionConfig = database != null 
            ? o => o.WithDatabase(database)
            : null;
            
        await using var session = driver.AsyncSession(sessionConfig);
        
        try
        {
            return await session.ExecuteWriteAsync(func);
        }
        catch (Neo4jException ex)
        {
            _logger.LogError(ex, "Error executing write transaction");
            throw;
        }
    }
    
    public async Task ExecuteWriteAsync(Func<IAsyncQueryRunner, Task> func, string? database = null)
    {
        await ExecuteWriteAsync(async tx =>
        {
            await func(tx);
            return 0;
        }, database);
    }
    
    public async Task<List<IRecord>> RunQueryAsync(string cypher, object? parameters = null, bool isWrite = false)
    {
        if (isWrite)
        {
            return await ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher, parameters);
                return await cursor.ToListAsync();
            });
        }
        else
        {
            return await ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(cypher, parameters);
                return await cursor.ToListAsync();
            });
        }
    }
    
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("RETURN 1 AS test");
                var result = await cursor.SingleAsync();
                return result["test"].As<int>() == 1;
            });
            
            _logger.LogInformation("Neo4j connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Neo4j connection test failed");
            return false;
        }
    }
}