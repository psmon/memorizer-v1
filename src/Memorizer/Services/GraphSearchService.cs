
using Neo4j.Driver;
using Memorizer.Models;
using Memorizer.Prompts;
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
            var wordNodeIds = new HashSet<string>();
            var relationships = new List<GraphRelationship>();
            var nodeConnectionCounts = new Dictionary<string, int>();
            
            // First pass: collect all nodes and relationships
            foreach (var record in records)
            {
                foreach (var value in record.Values.Values)
                {
                    if (value is INode node)
                    {
                        await ProcessNode(node, nodeIds, wordNodeIds, result, nodeConnectionCounts);
                    }
                    else if (value is IRelationship relationship)
                    {
                        await ProcessRelationship(relationship, relationships, nodeConnectionCounts);
                    }
                    else if (value is IPath path)
                    {
                        await ProcessPath(path, nodeIds, wordNodeIds, result, relationships, nodeConnectionCounts);
                    }
                }
            }
            
            // Enrich nodes with connection information
            EnrichNodesWithConnectionData(result.Nodes, nodeConnectionCounts);
            
            // If we have nodes, expand to include their direct connections for richer context
            if (result.Nodes.Count > 0 && result.Nodes.Count < 10)
            {
                await ExpandWithDirectConnections(result, nodeIds, wordNodeIds, relationships);
            }
            
            result.Relationships = relationships;
            
            // Log search metrics
            _logger.LogInformation("Graph search completed: {NodeCount} nodes, {RelationshipCount} relationships, Query: {Query}", 
                result.Nodes.Count, result.Relationships.Count, naturalLanguageQuery);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching graph with query: {Query}", naturalLanguageQuery);
            throw;
        }
    }
    
    private Task ProcessNode(INode node, HashSet<string> nodeIds, HashSet<string> wordNodeIds, 
        GraphSearchResult result, Dictionary<string, int> connectionCounts)
    {
        if (node.Labels.Contains("Word"))
        {
            var word = node["name"].As<string>();
            if (!wordNodeIds.Contains(word))
            {
                wordNodeIds.Add(word);
                var wordNode = new GraphMemoryNode
                {
                    Id = Guid.NewGuid(), // Word nodes don't have GUIDs
                    Title = word,
                    Type = "Word",
                    Source = "keyword",
                    Confidence = node.Properties.ContainsKey("frequency") ? node["frequency"].As<double>() / 100.0 : 1.0,
                    CreatedAt = node.Properties.ContainsKey("createdAt") 
                        ? DateTime.Parse(node["createdAt"].As<string>()) 
                        : DateTime.UtcNow,
                    Tags = new List<string> { node.Properties.ContainsKey("language") ? node["language"].As<string>() : "en" },
                    Summary = $"Keyword appearing {(node.Properties.ContainsKey("frequency") ? node["frequency"].As<int>() : 1)} times",
                    Metadata = new Dictionary<string, object>
                    {
                        ["frequency"] = node.Properties.ContainsKey("frequency") ? node["frequency"].As<int>() : 1,
                        ["language"] = node.Properties.ContainsKey("language") ? node["language"].As<string>() : "en"
                    }
                };
                result.Nodes.Add(wordNode);
            }
        }
        else if (node.Labels.Contains("Memory"))
        {
            var nodeId = node["id"].As<string>();
            if (!nodeIds.Contains(nodeId))
            {
                nodeIds.Add(nodeId);
                var memoryNode = new GraphMemoryNode
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
                    Summary = node.Properties.ContainsKey("summary") ? node["summary"].As<string>() : null,
                    Metadata = new Dictionary<string, object>()
                };
                
                // Initialize connection count
                if (!connectionCounts.ContainsKey(nodeId))
                    connectionCounts[nodeId] = 0;
                    
                result.Nodes.Add(memoryNode);
            }
        }
        return Task.CompletedTask;
    }
    
    private async Task ProcessRelationship(IRelationship relationship, List<GraphRelationship> relationships,
        Dictionary<string, int> connectionCounts)
    {
        var startNode = await GetNodeById(relationship.StartNodeElementId);
        var endNode = await GetNodeById(relationship.EndNodeElementId);
        
        if (startNode != null && endNode != null)
        {
            var startId = GetNodeIdString(startNode);
            var endId = GetNodeIdString(endNode);
            
            // Skip Word node relationships for GUID-based relationships
            if (startNode.Labels.Contains("Memory") && endNode.Labels.Contains("Memory"))
            {
                var rel = new GraphRelationship
                {
                    FromId = Guid.Parse(startId),
                    ToId = Guid.Parse(endId),
                    Type = relationship.Properties.ContainsKey("type") 
                        ? relationship["type"].As<string>() 
                        : relationship.Type,
                    Weight = relationship.Properties.ContainsKey("weight") 
                        ? relationship["weight"].As<double>() 
                        : (relationship.Properties.ContainsKey("relevance") 
                            ? relationship["relevance"].As<double>() 
                            : 1.0),
                    CreatedAt = relationship.Properties.ContainsKey("createdAt")
                        ? DateTime.Parse(relationship["createdAt"].As<string>())
                        : DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>()
                };
                
                // Add relationship-specific metadata
                if (relationship.Type == "RELATES_TO" && relationship.Properties.ContainsKey("type"))
                {
                    rel.Metadata["relationshipSubtype"] = relationship["type"].As<string>();
                }
                
                relationships.Add(rel);
                
                // Update connection counts
                if (connectionCounts.ContainsKey(startId))
                    connectionCounts[startId]++;
                if (connectionCounts.ContainsKey(endId))
                    connectionCounts[endId]++;
            }
        }
    }
    
    private async Task ProcessPath(IPath path, HashSet<string> nodeIds, HashSet<string> wordNodeIds,
        GraphSearchResult result, List<GraphRelationship> relationships, Dictionary<string, int> connectionCounts)
    {
        foreach (var pathNode in path.Nodes)
        {
            await ProcessNode(pathNode, nodeIds, wordNodeIds, result, connectionCounts);
        }
        
        foreach (var pathRelationship in path.Relationships)
        {
            await ProcessRelationship(pathRelationship, relationships, connectionCounts);
        }
    }
    
    private void EnrichNodesWithConnectionData(List<GraphMemoryNode> nodes, Dictionary<string, int> connectionCounts)
    {
        foreach (var node in nodes.Where(n => n.Type != "Word"))
        {
            var nodeIdStr = node.Id.ToString();
            if (connectionCounts.ContainsKey(nodeIdStr))
            {
                node.Metadata["connectionCount"] = connectionCounts[nodeIdStr];
                node.Metadata["isHub"] = connectionCounts[nodeIdStr] > 3;
            }
        }
    }
    
    private async Task ExpandWithDirectConnections(GraphSearchResult result, HashSet<string> nodeIds,
        HashSet<string> wordNodeIds, List<GraphRelationship> relationships)
    {
        try
        {
            // Get IDs of current Memory nodes (not Word nodes)
            var memoryNodeIds = result.Nodes
                .Where(n => n.Type != "Word")
                .Select(n => n.Id.ToString())
                .Take(5) // Limit expansion to first 5 nodes
                .ToList();
            
            if (memoryNodeIds.Count == 0) return;
            
            // Query for direct connections
            var expandQuery = $@"
                MATCH (m:Memory)-[r:RELATES_TO]-(connected:Memory)
                WHERE m.id IN [{string.Join(",", memoryNodeIds.Select(id => $"'{id}'"))}]
                  AND NOT connected.id IN [{string.Join(",", nodeIds.Select(id => $"'{id}'"))}]
                RETURN DISTINCT connected, r
                LIMIT 20";
            
            var expandedRecords = await _graphRepository.RunQueryAsync(expandQuery);
            
            foreach (var record in expandedRecords)
            {
                foreach (var value in record.Values.Values)
                {
                    if (value is INode node && node.Labels.Contains("Memory"))
                    {
                        await ProcessNode(node, nodeIds, wordNodeIds, result, new Dictionary<string, int>());
                    }
                    else if (value is IRelationship relationship)
                    {
                        await ProcessRelationship(relationship, relationships, new Dictionary<string, int>());
                    }
                }
            }
            
            _logger.LogInformation("Expanded graph with {Count} additional connected nodes", 
                expandedRecords.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to expand with direct connections, continuing with original results");
        }
    }
    
    private string GetNodeIdString(INode node)
    {
        if (node.Labels.Contains("Word"))
        {
            return node["name"].As<string>();
        }
        return node["id"].As<string>();
    }
    
    public async Task<string> GenerateCypherQueryAsync(string naturalLanguageQuery)
    {
        // Use centralized prompt template for Cypher query generation
        var prompt = PromptTemplates.CreateGraphQueryPrompt(naturalLanguageQuery);
        
        var cypherQuery = await _llmService.CompleteAsync(prompt);
        
        // Clean up the response
        cypherQuery = cypherQuery.Trim();
        
        // Remove markdown code blocks if present
        if (cypherQuery.StartsWith("```"))
        {
            var lines = cypherQuery.Split('\n');
            cypherQuery = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
        }
        
        // Remove any trailing explanations
        var queryLines = cypherQuery.Split('\n');
        var cleanLines = new List<string>();
        foreach (var line in queryLines)
        {
            var trimmedLine = line.Trim();
            // Stop if we hit an explanation marker
            if (trimmedLine.StartsWith("Explanation:") || 
                trimmedLine.StartsWith("Note:") || 
                trimmedLine.StartsWith("This query") ||
                trimmedLine.StartsWith("//") ||
                (trimmedLine.Length > 0 && !IsCypherKeyword(trimmedLine)))
            {
                break;
            }
            cleanLines.Add(line);
        }
        cypherQuery = string.Join('\n', cleanLines).Trim();
        
        // Final cleanup
        cypherQuery = cypherQuery.Replace("```cypher", "").Replace("```", "").Trim();
        
        return cypherQuery;
    }
    
    private bool IsCypherKeyword(string line)
    {
        var upperLine = line.ToUpper();
        var cypherKeywords = new[] { "MATCH", "WHERE", "RETURN", "WITH", "ORDER", "LIMIT", 
                                      "CREATE", "DELETE", "MERGE", "SET", "REMOVE", "UNION", 
                                      "OPTIONAL", "SKIP", "DISTINCT", "AS", "BY", "DESC", "ASC",
                                      "AND", "OR", "NOT", "IN", "CONTAINS", "STARTS", "ENDS" };
        return cypherKeywords.Any(keyword => upperLine.Contains(keyword));
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