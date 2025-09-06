using System.Text.Json;
using Memorizer.Services;
using Memorizer.Models;
using Memorizer.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Npgsql;
using Pgvector;

namespace Memorizer.IntegrationTests;

[Collection(nameof(IntegrationTestCollection))]
public class GraphSyncTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private IGraphRepository _graphRepository = null!;
    private IGraphSyncService _graphSyncService = null!;
    private IStorage _storage = null!;
    private IDriver _neo4jDriver = null!;
    private string _postgresConnectionString = null!;

    public GraphSyncTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Setup Neo4j connection
        _neo4jDriver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "password"));
        
        var neo4jDriverFactory = new TestNeo4jDriverFactory(_neo4jDriver);
        var graphLogger = new TestLogger<GraphRepository>();
        _graphRepository = new GraphRepository(neo4jDriverFactory, graphLogger);

        // Setup services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Storage"] = _fixture.PostgresConnectionString,
                ["Ollama:BaseUrl"] = _fixture.OllamaApiUrl,
                ["Ollama:EmbeddingModel"] = "all-minilm"
            })
            .Build();

        _postgresConnectionString = _fixture.PostgresConnectionString;
        
        // Create EmbeddingSettings for the service
        var embeddingSettings = new EmbeddingSettings
        {
            ApiUrl = new Uri(_fixture.OllamaApiUrl),
            Model = "all-minilm"
        };
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.OllamaApiUrl) };
        var embeddingService = new OllamaEmbeddingService(httpClient, embeddingSettings, new TestLogger<OllamaEmbeddingService>());
        var llmService = new TestOllamaLlmService();
        var graphSyncLogger = new TestLogger<GraphSyncService>();
        
        _graphSyncService = new GraphSyncService(
            _graphRepository,
            configuration,
            llmService,
            graphSyncLogger);

        var dataSource = NpgsqlDataSource.Create(_postgresConnectionString);
        var storageLogger = new TestLogger<Storage>();
        var serviceProvider = new TestServiceProvider(_graphSyncService);
        
        _storage = new Storage(dataSource, embeddingService, serviceProvider, storageLogger);
        
        // Initialize Neo4j schema
        await _graphSyncService.InitializeGraphSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        // Clear test data
        await _graphRepository.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync("MATCH (n) DETACH DELETE n");
        });
        
        _neo4jDriver?.Dispose();
    }

    [Fact]
    public async Task SyncMemoriesToGraph_WithTags_CreatesWordNodes()
    {
        // Arrange
        var tags = new[] { "test", "integration", "graph" };
        var memory = await _storage.StoreMemory(
            type: "test",
            content: "Test memory content for graph sync",
            source: "unit-test",
            tags: tags,
            confidence: 1.0,
            title: "Test Graph Sync"
        );

        // Act
        var syncedCount = await _graphSyncService.SyncMemoriesToGraphAsync(fullSync: true);

        // Assert
        Assert.Equal(1, syncedCount);

        // Verify Memory node was created
        var memoryNodes = await _graphRepository.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(
                "MATCH (m:Memory {id: $id}) RETURN m",
                new { id = memory.Id.ToString() });
            return await cursor.ToListAsync();
        });
        
        Assert.Single(memoryNodes);

        // Verify Word nodes were created from tags
        var wordNodes = await _graphRepository.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("MATCH (w:Word) RETURN w.name AS name ORDER BY name");
            return await cursor.ToListAsync();
        });
        
        Assert.Equal(3, wordNodes.Count);
        var wordNames = wordNodes.Select(r => r["name"].As<string>()).ToList();
        Assert.Contains("test", wordNames);
        Assert.Contains("integration", wordNames);
        Assert.Contains("graph", wordNames);

        // Verify relationships between Memory and Word nodes
        var relationships = await _graphRepository.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(
                "MATCH (m:Memory {id: $id})-[r:HAS_KEYWORD]->(w:Word) RETURN w.name AS word",
                new { id = memory.Id.ToString() });
            return await cursor.ToListAsync();
        });
        
        Assert.Equal(3, relationships.Count);
    }

    [Fact]
    public async Task FullSync_ClearsDatabase_AndResyncsAll()
    {
        // Arrange - Create initial data
        var memory1 = await _storage.StoreMemory(
            type: "test",
            content: "First memory",
            source: "test",
            tags: new[] { "first", "test" },
            confidence: 1.0,
            title: "Memory 1"
        );
        
        await _graphSyncService.SyncMemoriesToGraphAsync(fullSync: false);
        
        // Create a manual node that should be deleted
        await _graphRepository.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync("CREATE (n:TestNode {name: 'should-be-deleted'})");
        });

        // Add second memory
        var memory2 = await _storage.StoreMemory(
            type: "test",
            content: "Second memory",
            source: "test",
            tags: new[] { "second", "test" },
            confidence: 1.0,
            title: "Memory 2"
        );

        // Act - Full sync
        var syncedCount = await _graphSyncService.SyncMemoriesToGraphAsync(fullSync: true);

        // Assert
        Assert.Equal(2, syncedCount); // Both memories should be synced

        // Verify test node was deleted
        var testNodes = await _graphRepository.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("MATCH (n:TestNode) RETURN n");
            return await cursor.ToListAsync();
        });
        Assert.Empty(testNodes);

        // Verify all memories are present
        var memoryNodes = await _graphRepository.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("MATCH (m:Memory) RETURN m.id AS id ORDER BY m.createdAt");
            return await cursor.ToListAsync();
        });
        Assert.Equal(2, memoryNodes.Count);

        // Verify Word nodes were created correctly
        var wordNodes = await _graphRepository.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync("MATCH (w:Word) RETURN w.name AS name ORDER BY name");
            return await cursor.ToListAsync();
        });
        
        var wordNames = wordNodes.Select(r => r["name"].As<string>()).ToList();
        Assert.Contains("first", wordNames);
        Assert.Contains("second", wordNames);
        Assert.Contains("test", wordNames); // Common tag
    }

    [Fact]
    public async Task WordNode_FrequencyIncreases_WithMultipleMemories()
    {
        // Arrange
        var commonTag = "shared-tag";
        
        // Create first memory with common tag
        var memory1 = await _storage.StoreMemory(
            type: "test",
            content: "First memory",
            source: "test",
            tags: new[] { commonTag, "unique1" },
            confidence: 1.0,
            title: "Memory 1"
        );

        // Create second memory with common tag
        var memory2 = await _storage.StoreMemory(
            type: "test",
            content: "Second memory",
            source: "test",
            tags: new[] { commonTag, "unique2" },
            confidence: 1.0,
            title: "Memory 2"
        );

        // Act
        await _graphSyncService.SyncMemoriesToGraphAsync(fullSync: true);

        // Assert - Check Word node frequency
        var wordNode = await _graphRepository.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(
                "MATCH (w:Word {name: $name}) RETURN w.frequency AS frequency",
                new { name = commonTag });
            return await cursor.SingleAsync();
        });
        
        Assert.Equal(2, wordNode["frequency"].As<int>());

        // Verify both memories are connected to the shared Word
        var connectedMemories = await _graphRepository.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(
                "MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word {name: $name}) RETURN m.id AS id ORDER BY m.createdAt",
                new { name = commonTag });
            return await cursor.ToListAsync();
        });
        
        Assert.Equal(2, connectedMemories.Count);
    }

    [Fact]
    public async Task GraphVisualization_IncludesWordNodes()
    {
        // Arrange
        var memory = await _storage.StoreMemory(
            type: "test",
            content: "Test memory for visualization",
            source: "test",
            tags: new[] { "visualization", "test" },
            confidence: 1.0,
            title: "Viz Test"
        );

        await _graphSyncService.SyncMemoriesToGraphAsync(fullSync: true);

        // Act
        var visualization = await _graphSyncService.GetGraphVisualizationAsync(limit: 100);

        // Assert
        Assert.NotNull(visualization);
        
        // Check for Memory nodes
        var memoryNodes = visualization.Nodes.Where(n => n.Type == "Memory").ToList();
        Assert.Single(memoryNodes);
        Assert.Equal(memory.Id.ToString(), memoryNodes[0].Id);

        // Check for Word nodes
        var wordNodes = visualization.Nodes.Where(n => n.Type == "Word").ToList();
        Assert.Equal(2, wordNodes.Count);
        
        var wordLabels = wordNodes.Select(n => n.Label).OrderBy(l => l).ToList();
        Assert.Contains("test", wordLabels);
        Assert.Contains("visualization", wordLabels);

        // Check edges
        var edges = visualization.Edges.Where(e => e.Label == "HAS_KEYWORD").ToList();
        Assert.Equal(2, edges.Count);
    }
}

// Test helpers
public class TestNeo4jDriverFactory : INeo4jDriverFactory, IDisposable
{
    private readonly IDriver _driver;
    
    public TestNeo4jDriverFactory(IDriver driver)
    {
        _driver = driver;
    }
    
    public IDriver GetDriver() => _driver;
    
    public void Dispose()
    {
        // Driver is disposed elsewhere
    }
}

public class TestOllamaLlmService : ILlmService, IDisposable
{
    public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Return mock LLM response for relationship suggestions
        return Task.FromResult("[{\"targetIndex\": 1, \"type\": \"related-to\", \"confidence\": 0.8}]");
    }
    
    public Task<List<string>> ExtractKeywordsAsync(string text, string type, int maxKeywords = 10, CancellationToken cancellationToken = default)
    {
        // This should not be called as we're using tags instead
        throw new NotImplementedException("Keywords should be extracted from tags, not LLM");
    }
    
    public Task<string> GenerateTitle(string text, string type, string[]? tags, int maxLength = 100, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Generated title for {type}");
    }
    
    public Task<LlmHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LlmHealthResult
        {
            IsHealthy = true,
            Message = "Test LLM service is healthy",
            ModelName = "test-model"
        });
    }
    
    public void Dispose()
    {
        // Nothing to dispose
    }
}

public class TestServiceProvider : IServiceProvider
{
    private readonly IGraphSyncService? _graphSyncService;
    
    public TestServiceProvider(IGraphSyncService? graphSyncService)
    {
        _graphSyncService = graphSyncService;
    }
    
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IGraphSyncService))
            return _graphSyncService;
        return null;
    }
}

public class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Could write to test output if needed
    }
}