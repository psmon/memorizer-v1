
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
                        // Handle Word nodes differently
                        if (node.Labels.Contains("Word"))
                        {
                            var word = node["name"].As<string>();
                            if (!nodeIds.Contains(word))
                            {
                                nodeIds.Add(word);
                                result.Nodes.Add(new GraphMemoryNode
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
                                    Summary = $"Keyword appearing {(node.Properties.ContainsKey("frequency") ? node["frequency"].As<int>() : 1)} times"
                                });
                            }
                        }
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
                                        : new List<string>(),
                                    Summary = node.Properties.ContainsKey("summary") ? node["summary"].As<string>() : null
                                });
                            }
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

## Comprehensive Examples

### Type-based Searches
- ""Find all reference documents"" -> MATCH (m:Memory {{type: 'reference'}}) RETURN m LIMIT 50
- ""Show how-to guides"" -> MATCH (m:Memory {{type: 'how-to'}}) RETURN m LIMIT 50
- ""Get system memories"" -> MATCH (m:Memory {{type: 'system'}}) RETURN m LIMIT 50

### Keyword and Tag Searches
- ""Find memories about Docker or Kubernetes"" -> MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word) WHERE w.name IN ['docker', 'kubernetes', 'container'] RETURN DISTINCT m, w LIMIT 50
- ""Show memories related to AI"" -> MATCH (m:Memory) WHERE m.title CONTAINS 'AI' OR m.summary CONTAINS 'AI' OR 'ai' IN m.tags OR 'artificial-intelligence' IN m.tags RETURN m LIMIT 50
- ""Find SSE or Server-Sent Events"" -> MATCH (m:Memory) WHERE m.title CONTAINS 'SSE' OR m.summary CONTAINS 'Server-Sent' OR 'sse' IN m.tags OR m.summary CONTAINS 'server-sent-events' RETURN m LIMIT 50
- ""Search for reactive programming"" -> MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word) WHERE w.name CONTAINS 'reactive' OR w.name = 'rxjs' OR w.name = 'reactor' RETURN DISTINCT m, w LIMIT 50

### Relationship Queries
- ""Find memories that extend DDD concepts"" -> MATCH (m1:Memory)-[r:RELATES_TO {{type: 'extends'}}]->(m2:Memory) WHERE m2.title CONTAINS 'DDD' OR m2.summary CONTAINS 'DDD' OR 'ddd' IN m2.tags RETURN m1, r, m2 LIMIT 50
- ""Show enhanced versions"" -> MATCH (m1:Memory)-[r:RELATES_TO {{type: 'enhanced-version'}}]->(m2:Memory) RETURN m1, r, m2 LIMIT 50
- ""Find examples of patterns"" -> MATCH (m1:Memory)-[r:RELATES_TO {{type: 'example-of'}}]->(m2:Memory) WHERE m2.title CONTAINS 'pattern' RETURN m1, r, m2 LIMIT 50
- ""Show memories that support each other"" -> MATCH (m1:Memory)-[r:RELATES_TO {{type: 'supports'}}]-(m2:Memory) RETURN m1, r, m2 LIMIT 30

### Analysis Queries
- ""Find the most connected memories"" -> MATCH (m:Memory)-[r]-(other) WITH m, COUNT(r) as connections WHERE connections > 2 RETURN m, connections ORDER BY connections DESC LIMIT 20
- ""Show most frequent keywords"" -> MATCH (w:Word) RETURN w ORDER BY w.frequency DESC LIMIT 20
- ""Find memories with common keywords"" -> MATCH (m1:Memory)-[:HAS_KEYWORD]->(w:Word)<-[:HAS_KEYWORD]-(m2:Memory) WHERE m1.id <> m2.id WITH m1, m2, COUNT(DISTINCT w) as common_keywords WHERE common_keywords > 2 RETURN m1, m2, common_keywords ORDER BY common_keywords DESC LIMIT 20
- ""Recent high-confidence memories"" -> MATCH (m:Memory) WHERE m.confidence > 0.8 RETURN m ORDER BY m.createdAt DESC LIMIT 30

### Complex Pattern Matching
- ""Find reference documents with examples"" -> MATCH (ref:Memory {{type: 'reference'}})<-[r:RELATES_TO {{type: 'example-of'}}]-(example:Memory) RETURN ref, r, example LIMIT 30
- ""Show memories connected through multiple hops"" -> MATCH path = (m1:Memory)-[:RELATES_TO*1..3]-(m2:Memory) WHERE m1.id <> m2.id RETURN path LIMIT 20
- ""Find hub memories (connected to many keywords)"" -> MATCH (m:Memory)-[r:HAS_KEYWORD]->(w:Word) WITH m, COUNT(DISTINCT w) as keyword_count WHERE keyword_count > 5 RETURN m, keyword_count ORDER BY keyword_count DESC LIMIT 20

### Source and Confidence Queries
- ""Find LLM-generated memories"" -> MATCH (m:Memory {{source: 'LLM'}}) RETURN m ORDER BY m.createdAt DESC LIMIT 50
- ""Show high confidence recent memories"" -> MATCH (m:Memory) WHERE m.confidence >= 0.9 AND m.createdAt > datetime('{{year}}-{{month}}-01T00:00:00Z') RETURN m ORDER BY m.createdAt DESC LIMIT 30

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