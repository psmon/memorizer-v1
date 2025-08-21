using Neo4j.Driver;
using Memorizer.Settings;
using Microsoft.Extensions.Options;

namespace Memorizer.Services;

public interface INeo4jDriverFactory : IDisposable
{
    IDriver GetDriver();
}

public sealed class Neo4jDriverFactory : INeo4jDriverFactory
{
    private readonly IDriver _driver;
    private readonly ILogger<Neo4jDriverFactory> _logger;
    
    public Neo4jDriverFactory(IOptions<Neo4jSettings> settings, ILogger<Neo4jDriverFactory> logger)
    {
        _logger = logger;
        var neo4jSettings = settings.Value;
        
        _driver = GraphDatabase.Driver(
            neo4jSettings.Uri, 
            AuthTokens.Basic(neo4jSettings.User, neo4jSettings.Password),
            config => config
                .WithMaxConnectionPoolSize(neo4jSettings.MaxConnectionPoolSize)
                .WithConnectionAcquisitionTimeout(neo4jSettings.ConnectionAcquisitionTimeout));
        
        _logger.LogInformation("Neo4j driver initialized for {Uri} with database {Database}", 
            neo4jSettings.Uri, neo4jSettings.Database);
    }
    
    public IDriver GetDriver() => _driver;
    
    public void Dispose()
    {
        _driver?.Dispose();
        _logger.LogInformation("Neo4j driver disposed");
    }
}