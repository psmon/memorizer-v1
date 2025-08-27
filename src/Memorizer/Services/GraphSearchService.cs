
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
        var prompt = $@"You are a Neo4j Cypher query expert. Convert the following natural language query to a precise Cypher query.

## Graph Database Schema

### Nodes
1. **Memory Node**
   - id: string (UUID) - unique identifier
   - type: string - values: 'reference', 'how-to', 'system', 'conversation', 'document'
   - source: string - origin of memory (e.g., 'LLM', 'user', 'system')
   - title: string - descriptive title
   - summary: string - detailed content/description
   - tags: string[] - array of keyword tags
   - confidence: double (0.0-1.0) - confidence score
   - createdAt: string (ISO datetime) - creation timestamp

2. **Word Node**
   - name: string - the keyword/word
   - language: string - language code (e.g., 'en', 'ko')
   - frequency: int - usage frequency count
   - createdAt: string (ISO datetime)
   - updatedAt: string (ISO datetime)

### Relationships
1. **RELATES_TO** (Memory -> Memory)
   - type: string - relationship subtype
   - Common types: 'extends', 'enhanced-version', 'supports', 'contradicts', 'implements', 'references', 'related-to', 'example-of', 'explains'
   - weight: double (0.0-1.0) - relationship strength
   - createdAt: string (ISO datetime)

2. **HAS_KEYWORD** (Memory -> Word)
   - relevance: double (0.0-1.0) - keyword relevance to memory
   - createdAt: string (ISO datetime)

## Query Guidelines
1. Use case-insensitive matching with CONTAINS for text searches
2. Always include LIMIT clause (default 50 unless specified)
3. Return nodes and relationships when traversing paths
4. Use DISTINCT when necessary to avoid duplicates
5. For keyword searches, utilize the Word nodes and HAS_KEYWORD relationships
6. Consider multiple search patterns (title, summary, tags) for comprehensive results
7. Use WITH clauses for complex aggregations
8. Order results by relevance when applicable

## Natural Language Query: {naturalLanguageQuery}

## Comprehensive Examples with Relationship Context

### Type-based Searches with Connections
- ""Find all reference documents"" -> MATCH (m:Memory {{type: 'reference'}}) OPTIONAL MATCH (m)-[r:RELATES_TO]-(connected:Memory) RETURN m, r, connected LIMIT 50
- ""Show how-to guides"" -> MATCH (m:Memory {{type: 'how-to'}}) OPTIONAL MATCH (m)-[:HAS_KEYWORD]->(w:Word) RETURN m, w LIMIT 50
- ""Get system memories with their relationships"" -> MATCH (m:Memory {{type: 'system'}}) OPTIONAL MATCH (m)-[r]-(other) RETURN m, r, other LIMIT 50

### Enhanced Keyword and Tag Searches
- ""Find memories about Docker or Kubernetes"" -> MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word) WHERE toLower(w.name) IN ['docker', 'kubernetes', 'container', 'k8s', 'containerization'] WITH m, COLLECT(w) as keywords OPTIONAL MATCH (m)-[r:RELATES_TO]-(related:Memory) RETURN m, keywords, r, related LIMIT 50
- ""Show memories related to AI"" -> MATCH (m:Memory) WHERE toLower(m.title) CONTAINS 'ai' OR toLower(m.summary) CONTAINS 'artificial intelligence' OR ANY(tag IN m.tags WHERE toLower(tag) IN ['ai', 'artificial-intelligence', 'machine-learning', 'ml', 'deep-learning']) OPTIONAL MATCH (m)-[r]-(connected) RETURN m, r, connected LIMIT 50
- ""Find SSE or Server-Sent Events"" -> MATCH (m:Memory) WHERE toLower(m.title) CONTAINS 'sse' OR toLower(m.summary) CONTAINS 'server-sent' OR ANY(tag IN m.tags WHERE toLower(tag) CONTAINS 'sse' OR toLower(tag) CONTAINS 'server-sent') RETURN m LIMIT 50
- ""Search for reactive programming"" -> MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word) WHERE toLower(w.name) CONTAINS 'reactive' OR w.name IN ['rxjs', 'reactor', 'akka', 'flux', 'mono'] WITH m, COLLECT(w) as keywords RETURN m, keywords LIMIT 50

### Rich Relationship Queries
- ""Find memories that extend DDD concepts"" -> MATCH (m1:Memory)-[r:RELATES_TO {{type: 'extends'}}]->(m2:Memory) WHERE toLower(m2.title) CONTAINS 'ddd' OR toLower(m2.summary) CONTAINS 'domain-driven' OR ANY(tag IN m2.tags WHERE toLower(tag) IN ['ddd', 'domain-driven-design']) WITH m1, r, m2 OPTIONAL MATCH (m1)-[:HAS_KEYWORD]->(w:Word) RETURN m1, r, m2, COLLECT(DISTINCT w) as keywords LIMIT 50
- ""Show enhanced versions with context"" -> MATCH (original:Memory)<-[r:RELATES_TO {{type: 'enhanced-version'}}]-(enhanced:Memory) WITH original, r, enhanced OPTIONAL MATCH (enhanced)-[:HAS_KEYWORD]->(w:Word) RETURN original, r, enhanced, COLLECT(w) as keywords ORDER BY enhanced.createdAt DESC LIMIT 50
- ""Find examples of patterns"" -> MATCH (example:Memory)-[r:RELATES_TO {{type: 'example-of'}}]->(pattern:Memory) WHERE toLower(pattern.title) CONTAINS 'pattern' OR toLower(pattern.type) = 'pattern' WITH example, r, pattern OPTIONAL MATCH (example)-[r2]-(other:Memory) WHERE other.id <> pattern.id RETURN example, r, pattern, COLLECT(DISTINCT other) as relatedExamples LIMIT 30
- ""Show memories that support each other"" -> MATCH (m1:Memory)-[r:RELATES_TO {{type: 'supports'}}]-(m2:Memory) WHERE id(m1) < id(m2) RETURN m1, r, m2 LIMIT 30

### Advanced Analysis Queries
- ""Find the most connected memories"" -> MATCH (m:Memory) WITH m, SIZE([(m)-[]-() | 1]) as degree WHERE degree > 2 OPTIONAL MATCH (m)-[r]-(connected) RETURN m, degree, COLLECT(DISTINCT {{node: connected, relationship: type(r)}}) as connections ORDER BY degree DESC LIMIT 20
- ""Show most frequent keywords with their memories"" -> MATCH (w:Word)<-[:HAS_KEYWORD]-(m:Memory) WITH w, COUNT(DISTINCT m) as memoryCount, COLLECT(DISTINCT m.title)[..5] as sampleTitles WHERE memoryCount > 1 RETURN w, memoryCount, sampleTitles ORDER BY w.frequency DESC, memoryCount DESC LIMIT 20
- ""Find memories with common keywords"" -> MATCH (m1:Memory)-[:HAS_KEYWORD]->(w:Word)<-[:HAS_KEYWORD]-(m2:Memory) WHERE id(m1) < id(m2) WITH m1, m2, COLLECT(DISTINCT w.name) as common_keywords, COUNT(DISTINCT w) as keyword_count WHERE keyword_count > 2 RETURN m1, m2, common_keywords, keyword_count ORDER BY keyword_count DESC LIMIT 20
- ""Recent high-confidence memories with relationships"" -> MATCH (m:Memory) WHERE m.confidence > 0.8 AND m.createdAt > datetime() - duration('P30D') OPTIONAL MATCH (m)-[r:RELATES_TO]-(related:Memory) RETURN m, COLLECT(DISTINCT {{memory: related, type: r.type}}) as relationships ORDER BY m.createdAt DESC LIMIT 30

### Complex Graph Patterns
- ""Find reference documents with examples"" -> MATCH (ref:Memory {{type: 'reference'}})<-[r:RELATES_TO {{type: 'example-of'}}]-(example:Memory) WITH ref, COLLECT(example) as examples OPTIONAL MATCH (ref)-[:HAS_KEYWORD]->(w:Word) RETURN ref, examples, COLLECT(DISTINCT w) as keywords LIMIT 30
- ""Show knowledge clusters"" -> MATCH path = (m1:Memory)-[:RELATES_TO*1..3]-(m2:Memory) WHERE m1.id <> m2.id WITH m1, m2, path, length(path) as distance RETURN path ORDER BY distance LIMIT 20
- ""Find hub memories"" -> MATCH (m:Memory) WITH m, SIZE([(m)-[:HAS_KEYWORD]->() | 1]) as keywordCount, SIZE([(m)-[:RELATES_TO]-() | 1]) as relationCount WHERE keywordCount > 5 OR relationCount > 3 RETURN m, keywordCount, relationCount, (keywordCount + relationCount * 2) as hubScore ORDER BY hubScore DESC LIMIT 20
- ""Trace relationship chains"" -> MATCH path = (start:Memory)-[:RELATES_TO*1..4]->(end:Memory) WHERE start.type = 'reference' AND end.type = 'example' RETURN path LIMIT 10

### Source and Confidence Analysis
- ""Find LLM-generated memories with connections"" -> MATCH (m:Memory {{source: 'LLM'}}) OPTIONAL MATCH (m)-[r]-(connected:Memory) WITH m, COLLECT(DISTINCT connected) as connections RETURN m, connections, SIZE(connections) as connectionCount ORDER BY m.createdAt DESC LIMIT 50
- ""High confidence memory network"" -> MATCH (m:Memory) WHERE m.confidence >= 0.9 OPTIONAL MATCH (m)-[r:RELATES_TO]-(related:Memory) WHERE related.confidence >= 0.8 RETURN m, COLLECT(DISTINCT related) as highConfidenceNetwork ORDER BY m.createdAt DESC LIMIT 30
- ""Memory evolution over time"" -> MATCH (m1:Memory)-[r:RELATES_TO {{type: 'enhanced-version'}}]->(m2:Memory) WHERE m1.createdAt < m2.createdAt RETURN m1, r, m2, duration.between(m1.createdAt, m2.createdAt) as timeDiff ORDER BY timeDiff LIMIT 20

## IMPORTANT INSTRUCTIONS
1. Return ONLY a valid Neo4j Cypher query
2. Do NOT include any explanations, comments, or markdown
3. Do NOT include backticks or code blocks
4. Do NOT include any text before or after the query
5. The response must be directly executable in Neo4j

Generate the Cypher query now:";

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