using Neo4j.Driver;
using Memorizer.Models;
using System.Text.Json;

namespace Memorizer.Services;

public interface IGraphSearchService
{
    Task<GraphSearchResult> SearchGraphAsync(string naturalLanguageQuery);
    Task<string> GenerateCypherQueryAsync(string naturalLanguageQuery);
}

public class GraphSearchService : IGraphSearchService
{
    private readonly IGraphRepository _graphRepository;
    private readonly ILlmService _llmService;
    private readonly ILogger<GraphSearchService> _logger;
    
    public GraphSearchService(
        IGraphRepository graphRepository,
        ILlmService llmService,
        ILogger<GraphSearchService> logger)
    {
        _graphRepository = graphRepository;
        _llmService = llmService;
        _logger = logger;
    }
    
    public async Task<GraphSearchResult> SearchGraphAsync(string naturalLanguageQuery)
    {
        try
        {
            var cypherQuery = await GenerateCypherQueryAsync(naturalLanguageQuery);
            
            _logger.LogInformation("Executing Cypher query: {Query}", cypherQuery);
            
            var result = new GraphSearchResult();
            
            var records = await _graphRepository.RunQueryAsync(cypherQuery);
            
            var nodeIds = new HashSet<string>();
            var relationships = new List<GraphRelationship>();
            
            foreach (var record in records)
            {
                foreach (var value in record.Values.Values)
                {
                    if (value is INode node)
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
                                    : new List<string>(),
                                Summary = node.Properties.ContainsKey("summary") ? node["summary"].As<string>() : null
                            });
                        }
                    }
                    else if (value is IRelationship relationship)
                    {
                        var startNode = await GetNodeById(relationship.StartNodeElementId);
                        var endNode = await GetNodeById(relationship.EndNodeElementId);
                        
                        if (startNode != null && endNode != null)
                        {
                            relationships.Add(new GraphRelationship
                            {
                                FromId = Guid.Parse(startNode["id"].As<string>()),
                                ToId = Guid.Parse(endNode["id"].As<string>()),
                                Type = relationship.Properties.ContainsKey("type") 
                                    ? relationship["type"].As<string>() 
                                    : relationship.Type,
                                Weight = relationship.Properties.ContainsKey("weight") 
                                    ? relationship["weight"].As<double>() 
                                    : 1.0,
                                CreatedAt = relationship.Properties.ContainsKey("createdAt")
                                    ? DateTime.Parse(relationship["createdAt"].As<string>())
                                    : DateTime.UtcNow
                            });
                        }
                    }
                    else if (value is IPath path)
                    {
                        foreach (var pathNode in path.Nodes)
                        {
                            var nodeId = pathNode["id"].As<string>();
                            if (!nodeIds.Contains(nodeId))
                            {
                                nodeIds.Add(nodeId);
                                result.Nodes.Add(new GraphMemoryNode
                                {
                                    Id = Guid.Parse(nodeId),
                                    Title = pathNode.Properties.ContainsKey("title") ? pathNode["title"].As<string>() : "",
                                    Type = pathNode.Properties.ContainsKey("type") ? pathNode["type"].As<string>() : "",
                                    Source = pathNode.Properties.ContainsKey("source") ? pathNode["source"].As<string>() : "",
                                    Confidence = pathNode.Properties.ContainsKey("confidence") ? pathNode["confidence"].As<double>() : 1.0,
                                    CreatedAt = pathNode.Properties.ContainsKey("createdAt") 
                                        ? DateTime.Parse(pathNode["createdAt"].As<string>()) 
                                        : DateTime.UtcNow,
                                    Tags = pathNode.Properties.ContainsKey("tags") 
                                        ? pathNode["tags"].As<List<string>>() ?? new List<string>()
                                        : new List<string>()
                                });
                            }
                        }
                        
                        foreach (var pathRelationship in path.Relationships)
                        {
                            var startNode = path.Nodes.FirstOrDefault(n => n.ElementId == pathRelationship.StartNodeElementId);
                            var endNode = path.Nodes.FirstOrDefault(n => n.ElementId == pathRelationship.EndNodeElementId);
                            
                            if (startNode != null && endNode != null)
                            {
                                relationships.Add(new GraphRelationship
                                {
                                    FromId = Guid.Parse(startNode["id"].As<string>()),
                                    ToId = Guid.Parse(endNode["id"].As<string>()),
                                    Type = pathRelationship.Properties.ContainsKey("type") 
                                        ? pathRelationship["type"].As<string>() 
                                        : pathRelationship.Type,
                                    Weight = pathRelationship.Properties.ContainsKey("weight") 
                                        ? pathRelationship["weight"].As<double>() 
                                        : 1.0
                                });
                            }
                        }
                    }
                }
            }
            
            result.Relationships = relationships;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching graph with query: {Query}", naturalLanguageQuery);
            throw;
        }
    }
    
    public async Task<string> GenerateCypherQueryAsync(string naturalLanguageQuery)
    {
        var prompt = $@"Convert the following natural language query to a Cypher query for Neo4j.

Schema:
- Node label: Memory
- Node properties: id (string UUID), type (string), source (string), title (string), tags (string array), confidence (double), createdAt (string ISO date), summary (string)
- Relationship: RELATES_TO
- Relationship properties: type (string), weight (double), createdAt (string ISO date)

Natural Language Query: {naturalLanguageQuery}

Instructions:
1. Generate a valid Cypher query that retrieves relevant nodes and relationships
2. Return ONLY the Cypher query, no explanation
3. Limit results to 50 unless specified otherwise
4. Common relationship types: extends, supports, contradicts, implements, references, related-to
5. For general searches, use pattern matching and text search on title and summary
6. Return nodes and relationships when relevant

Examples:
- ""Find all reference documents"" -> MATCH (m:Memory {{type: 'reference'}}) RETURN m LIMIT 50
- ""Show memories related to AI"" -> MATCH (m:Memory) WHERE m.title CONTAINS 'AI' OR m.summary CONTAINS 'AI' OR 'AI' IN m.tags RETURN m LIMIT 50
- ""Find memories that extend DDD concepts"" -> MATCH (m1:Memory)-[r:RELATES_TO {{type: 'extends'}}]->(m2:Memory) WHERE m2.title CONTAINS 'DDD' OR m2.summary CONTAINS 'DDD' RETURN m1, r, m2 LIMIT 50
- ""Show the most connected memories"" -> MATCH (m:Memory)-[r:RELATES_TO]-(other) WITH m, COUNT(r) as connections RETURN m, connections ORDER BY connections DESC LIMIT 20

Cypher Query:";

        var cypherQuery = await _llmService.CompleteAsync(prompt);
        
        cypherQuery = cypherQuery.Trim();
        if (cypherQuery.StartsWith("```"))
        {
            cypherQuery = cypherQuery.Split('\n')[1];
        }
        if (cypherQuery.EndsWith("```"))
        {
            cypherQuery = cypherQuery.Substring(0, cypherQuery.LastIndexOf("```")).Trim();
        }
        
        return cypherQuery;
    }
    
    private async Task<INode?> GetNodeById(string elementId)
    {
        try
        {
            var result = await _graphRepository.ExecuteReadAsync(async tx =>
            {
                var query = "MATCH (n) WHERE elementId(n) = $elementId RETURN n";
                var cursor = await tx.RunAsync(query, new { elementId });
                
                if (await cursor.FetchAsync())
                {
                    return cursor.Current["n"].As<INode>();
                }
                
                return null;
            });
            
            return result;
        }
        catch
        {
            return null;
        }
    }
}