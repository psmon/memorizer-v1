using Microsoft.AspNetCore.Mvc;
using Memorizer.Services;
using Memorizer.Models;
using Neo4j.Driver;

namespace Memorizer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GraphController : ControllerBase
{
    private readonly IGraphSyncService _graphSyncService;
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<GraphController> _logger;
    
    public GraphController(
        IGraphSyncService graphSyncService,
        IGraphRepository graphRepository,
        ILogger<GraphController> logger)
    {
        _graphSyncService = graphSyncService;
        _graphRepository = graphRepository;
        _logger = logger;
    }
    
    [HttpPost("sync")]
    public async Task<IActionResult> SyncMemories([FromQuery] bool fullSync = false)
    {
        try
        {
            var syncedCount = await _graphSyncService.SyncMemoriesToGraphAsync(fullSync);
            return Ok(new { success = true, syncedCount, message = $"Successfully synced {syncedCount} memories to graph" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing memories to graph");
            return StatusCode(500, new { success = false, error = "Failed to sync memories to graph" });
        }
    }
    
    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeSchema()
    {
        try
        {
            var success = await _graphSyncService.InitializeGraphSchemaAsync();
            if (success)
            {
                return Ok(new { success = true, message = "Graph schema initialized successfully" });
            }
            
            return StatusCode(500, new { success = false, error = "Failed to initialize graph schema" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing graph schema");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
    
    [HttpPost("relationships")]
    public async Task<IActionResult> CreateRelationship([FromBody] CreateRelationshipRequest request)
    {
        try
        {
            var success = await _graphSyncService.CreateGraphRelationshipAsync(
                request.FromId,
                request.ToId,
                request.Type,
                request.Properties);
            
            if (success)
            {
                return Ok(new { success = true, message = "Relationship created successfully" });
            }
            
            return BadRequest(new { success = false, error = "Failed to create relationship" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating relationship");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
    
    [HttpPost("relationships/suggest/{memoryId}")]
    public async Task<IActionResult> SuggestRelationships(Guid memoryId, [FromQuery] bool autoCreate = true)
    {
        try
        {
            _logger.LogInformation("Suggesting relationships for memory {MemoryId}", memoryId);
            var suggestions = await _graphSyncService.SuggestRelationshipsAsync(memoryId);
            
            int createdCount = 0;
            if (autoCreate)
            {
                foreach (var suggestion in suggestions)
                {
                    var success = await _graphSyncService.CreateGraphRelationshipAsync(
                        suggestion.FromId,
                        suggestion.ToId,
                        suggestion.Type,
                        new Dictionary<string, object> { ["weight"] = suggestion.Weight });
                        
                    if (success)
                    {
                        createdCount++;
                        _logger.LogInformation("Created relationship from {FromId} to {ToId} with type {Type}", 
                            suggestion.FromId, suggestion.ToId, suggestion.Type);
                    }
                }
            }
            
            return Ok(new { 
                success = true, 
                suggestionsCount = suggestions.Count,
                createdCount,
                suggestions = suggestions.Select(s => new {
                    fromId = s.FromId,
                    toId = s.ToId,
                    type = s.Type,
                    weight = s.Weight
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting relationships for memory {MemoryId}", memoryId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
    
    [HttpPost("relationships/suggest-all")]
    public async Task<IActionResult> SuggestAllRelationships()
    {
        try
        {
            _logger.LogInformation("Starting LLM-enhanced relationship discovery for all memories");
            
            // Get all memory IDs from Neo4j
            var memoryIds = await _graphRepository.ExecuteReadAsync(async tx =>
            {
                var query = "MATCH (m:Memory) RETURN m.id AS id";
                var cursor = await tx.RunAsync(query);
                var results = await cursor.ToListAsync();
                return results.Select(r => Guid.Parse(r["id"].As<string>())).ToList();
            });
            
            _logger.LogInformation("Found {Count} memories in Neo4j for relationship discovery", memoryIds.Count);
            
            int totalSuggestions = 0;
            int totalCreated = 0;
            
            foreach (var memoryId in memoryIds)
            {
                try
                {
                    var suggestions = await _graphSyncService.SuggestRelationshipsAsync(memoryId);
                    totalSuggestions += suggestions.Count;
                    
                    foreach (var suggestion in suggestions)
                    {
                        var success = await _graphSyncService.CreateGraphRelationshipAsync(
                            suggestion.FromId,
                            suggestion.ToId,
                            suggestion.Type,
                            new Dictionary<string, object> { ["weight"] = suggestion.Weight });
                            
                        if (success)
                        {
                            totalCreated++;
                        }
                    }
                    
                    if (suggestions.Count > 0)
                    {
                        _logger.LogInformation("Memory {MemoryId}: {Count} relationships suggested", memoryId, suggestions.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to suggest relationships for memory {MemoryId}", memoryId);
                }
            }
            
            _logger.LogInformation("Relationship discovery completed: {TotalCreated} relationships created from {TotalSuggestions} suggestions", 
                totalCreated, totalSuggestions);
            
            return Ok(new { 
                success = true, 
                memoriesProcessed = memoryIds.Count,
                totalSuggestions,
                totalCreated,
                message = $"Created {totalCreated} relationships from {totalSuggestions} suggestions across {memoryIds.Count} memories"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk relationship suggestion");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
    
    [HttpGet("visualization")]
    public async Task<IActionResult> GetVisualization([FromQuery] int limit = 100)
    {
        try
        {
            var data = await _graphSyncService.GetGraphVisualizationAsync(limit);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting graph visualization");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
    
    [HttpGet("health")]
    public async Task<IActionResult> CheckHealth()
    {
        try
        {
            var isHealthy = await _graphRepository.TestConnectionAsync();
            if (isHealthy)
            {
                return Ok(new { status = "healthy", message = "Neo4j connection is healthy" });
            }
            
            return StatusCode(503, new { status = "unhealthy", message = "Neo4j connection failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Neo4j health check failed");
            return StatusCode(503, new { status = "unhealthy", error = ex.Message });
        }
    }
    
    [HttpPost("search")]
    public async Task<IActionResult> SearchWithNaturalLanguage([FromBody] NaturalLanguageSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { success = false, error = "Query cannot be empty" });
            }
            
            // Get the GraphSearchService if available
            var graphSearchService = HttpContext.RequestServices.GetService<IGraphSearchService>();
            if (graphSearchService == null)
            {
                return StatusCode(501, new { success = false, error = "Graph search service not available" });
            }
            
            var result = await graphSearchService.SearchGraphAsync(request.Query);
            
            // Also return the generated Cypher query for transparency
            var cypherQuery = await graphSearchService.GenerateCypherQueryAsync(request.Query);
            
            return Ok(new
            {
                nodes = result.Nodes,
                relationships = result.Relationships,
                cypherQuery = cypherQuery
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing natural language graph search");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
    
    [HttpPost("search/cypher")]
    public async Task<IActionResult> SearchWithCypher([FromBody] CypherSearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { success = false, error = "Query cannot be empty" });
            }
            
            _logger.LogInformation("Executing Cypher query: {Query}", request.Query);
            
            var records = await _graphRepository.RunQueryAsync(request.Query);
            var result = new GraphSearchResult();
            
            var nodeIds = new HashSet<string>();
            var relationships = new List<GraphRelationship>();
            
            foreach (var record in records)
            {
                foreach (var value in record.Values.Values)
                {
                    if (value is INode node)
                    {
                        // Handle Word nodes
                        if (node.Labels.Contains("Word"))
                        {
                            var word = node.Properties.ContainsKey("name") ? node["name"].As<string>() : 
                                        node.Properties.ContainsKey("word") ? node["word"].As<string>() : "unknown";
                            if (!nodeIds.Contains(word))
                            {
                                nodeIds.Add(word);
                                result.Nodes.Add(new GraphMemoryNode
                                {
                                    Id = Guid.NewGuid(),
                                    Title = word,
                                    Type = "Word",
                                    Source = "keyword",
                                    Confidence = node.Properties.ContainsKey("frequency") ? node["frequency"].As<double>() / 100.0 : 1.0,
                                    CreatedAt = node.Properties.ContainsKey("createdAt") 
                                        ? DateTime.Parse(node["createdAt"].As<string>()) 
                                        : DateTime.UtcNow,
                                    Tags = new List<string> { node.Properties.ContainsKey("language") ? node["language"].As<string>() : "en" }
                                });
                            }
                        }
                        // Handle Memory nodes
                        else if (node.Labels.Contains("Memory"))
                        {
                            var nodeId = node["id"].As<string>();
                            if (!nodeIds.Contains(nodeId))
                            {
                                nodeIds.Add(nodeId);
                                result.Nodes.Add(new GraphMemoryNode
                                {
                                    Id = Guid.Parse(nodeId),
                                    Title = node.Properties.ContainsKey("title") ? node["title"].As<string>() : "",
                                    Type = node.Properties.ContainsKey("type") ? node["type"].As<string>() : "",
                                    Source = node.Properties.ContainsKey("source") ? node["source"].As<string>() : "",
                                    Confidence = node.Properties.ContainsKey("confidence") ? node["confidence"].As<double>() : 1.0,
                                    CreatedAt = node.Properties.ContainsKey("createdAt") 
                                        ? DateTime.Parse(node["createdAt"].As<string>()) 
                                        : DateTime.UtcNow,
                                    Tags = node.Properties.ContainsKey("tags") 
                                        ? node["tags"].As<List<string>>() ?? new List<string>()
                                        : new List<string>()
                                });
                            }
                        }
                    }
                    else if (value is IRelationship relationship)
                    {
                        // For now, we'll include basic relationship info
                        // In a full implementation, we'd need to resolve the start and end nodes
                        relationships.Add(new GraphRelationship
                        {
                            FromId = Guid.NewGuid(), // Would need to resolve actual IDs
                            ToId = Guid.NewGuid(),
                            Type = relationship.Type,
                            Weight = relationship.Properties.ContainsKey("weight") 
                                ? relationship["weight"].As<double>() 
                                : 1.0,
                            CreatedAt = relationship.Properties.ContainsKey("createdAt")
                                ? DateTime.Parse(relationship["createdAt"].As<string>())
                                : DateTime.UtcNow
                        });
                    }
                }
            }
            
            result.Relationships = relationships;
            
            return Ok(new
            {
                nodes = result.Nodes,
                relationships = result.Relationships
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Cypher query");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

public class CreateRelationshipRequest
{
    public Guid FromId { get; set; }
    public Guid ToId { get; set; }
    public string Type { get; set; } = "related-to";
    public Dictionary<string, object>? Properties { get; set; }
}

public class NaturalLanguageSearchRequest
{
    public string Query { get; set; } = string.Empty;
}

public class CypherSearchRequest
{
    public string Query { get; set; } = string.Empty;
}