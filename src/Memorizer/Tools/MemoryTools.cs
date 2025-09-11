using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using Memory = Memorizer.Models.Memory;
using System.Linq;
using System.Diagnostics;
using Memorizer.Services;
using Memorizer.Telemetry;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Memorizer.Models;

namespace PostgMem.Tools;

[McpServerToolType]
public class MemoryTools
{
    private readonly IStorage _storage;
    private readonly ILogger<MemoryTools> _logger;
    private readonly IGraphSearchService? _graphSearchService;

    public MemoryTools(IStorage storage, ILogger<MemoryTools> logger, IGraphSearchService? graphSearchService = null)
    {
        _storage = storage;
        _logger = logger;
        _graphSearchService = graphSearchService;
    }

    [McpServerTool, Description("Store a new memory in the database, optionally creating a relationship to another memory. Use this to save reference material, how-to guides, coding standards, or any information you (the LLM) may want to refer to when completing tasks. Include as much context as possible, such as markdown, code samples, and detailed explanations. Create relationships to link related reference materials or examples.")]
    public async Task<string> Store(
        [Description("The type of memory (e.g., 'conversation', 'document', 'reference', 'how-to', etc.). Use 'reference' or 'how-to' for reusable knowledge.")] string type,
        [Description("Plain text (markdown, code, prose, etc.) that you want to store and embed. If structural explanation is needed, you may use Mermaid markdown style.")]  string text,
        [Description("The source of the memory (e.g., 'user', 'system', 'LLM', etc.). Use 'LLM' if you are storing knowledge for your own future use.")] string source,
        [Description("Title for the memory. This is required and must not be null or empty.")] string title,
        [Description("Optional tags to categorize the memory. Use tags like 'coding-standard', 'unit-test', 'reference', 'how-to', etc. to make retrieval easier.")] string[]? tags = null,
        [Description("Confidence score for the memory (0.0 to 1.0)")] double confidence = 1.0,
        [Description("Optionally, the ID of a related memory. Use this to link related reference materials, how-tos, or examples.")] Guid? relatedTo = null,
        [Description("Optionally, the type of relationship to create (e.g., 'example-of', 'explains', 'related-to'). Use relationships to connect related knowledge.")] string? relationshipType = null,
        CancellationToken cancellationToken = default
    )
    {
        Memory memory = await _storage.StoreMemory(
            type,
            text,
            source,
            tags,
            confidence,
            title: title,
            cancellationToken: cancellationToken
        );

        // Handle manual relationship creation if specified
        if (relatedTo.HasValue && !string.IsNullOrWhiteSpace(relationshipType))
        {
            await _storage.CreateRelationship(memory.Id, relatedTo.Value, relationshipType, cancellationToken);
        }

        return $"Memory stored successfully with ID: {memory.Id}. You might want to call `CreateRelationship` to associate this memory with another memory for better context retrieval.";
    }

    [McpServerTool, Description("Search for memories similar to the provided text. Use this to retrieve reference material, how-tos, or examples relevant to the current task. Filtering by tags can help narrow down to specific types of knowledge.")]
    public async Task<string> Search(
        [Description("The text to search for similar memories. Use natural language queries to find relevant reference or how-to information.")] string query,
        [Description("Maximum number of results to return")] int limit = 10,
        [Description("Minimum similarity threshold (0.0 to 1.0)")] double minSimilarity = 0.7,
        [Description("Optional tags to filter memories (e.g., 'reference', 'how-to', 'coding-standard')")] string[]? filterTags = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryConfig.ActivitySource.StartActivity("MemoryTools.Search");
        
        // Add query details as Activity event with structured data
        activity?.AddEvent(new ActivityEvent("query.details", DateTimeOffset.UtcNow, new ActivityTagsCollection
        {
            {"query.text", query},
            {"query.limit", limit.ToString()},
            {"query.minSimilarity", minSimilarity.ToString()},
            {"query.filterTags", filterTags != null ? string.Join(", ", filterTags) : "none"}
        }));

        // Search for similar memories
        List<Memory> memories = await _storage.Search(
            query,
            limit,
            minSimilarity,
            filterTags,
            cancellationToken
        );

        // Log results count
        _logger.LogInformation("Memory search completed. Query: {Query}, ResultCount: {ResultCount}, Threshold: {Threshold}", query, memories.Count, minSimilarity);

        bool usedFallback = false;
        double actualThreshold = minSimilarity;

        // If no results found, try with a 10% lower threshold (but not below 0.0)
        if (memories.Count == 0 && minSimilarity > 0.0)
        {
            double fallbackThreshold = Math.Max(0.0, minSimilarity - 0.1);
            
            _logger.LogInformation("No results found at threshold {OriginalThreshold}, trying fallback search at {FallbackThreshold}", minSimilarity, fallbackThreshold);
            
            activity?.AddEvent(new ActivityEvent("fallback.search", DateTimeOffset.UtcNow, new ActivityTagsCollection
            {
                {"fallback.threshold", fallbackThreshold.ToString()},
                {"original.threshold", minSimilarity.ToString()}
            }));

            memories = await _storage.Search(
                query,
                limit,
                fallbackThreshold,
                filterTags,
                cancellationToken
            );

            if (memories.Count > 0)
            {
                usedFallback = true;
                actualThreshold = fallbackThreshold;
                _logger.LogInformation("Fallback search found {ResultCount} results at threshold {FallbackThreshold}", memories.Count, fallbackThreshold);
            }
        }

        if (memories.Count == 0)
        {
            activity?.SetStatus(ActivityStatusCode.Ok, "No results found even with fallback");
            return "No memories found matching your query, even with a relaxed similarity threshold. Try using different search terms or lowering the similarity threshold further.";
        }

        // Log detailed results for each memory
        foreach (var memory in memories)
        {
            var relevancyScore = memory.Similarity.HasValue ? (100 * (1 - memory.Similarity.Value)) : 0;
            var relationshipCount = memory.Relationships?.Count ?? 0;
            
            _logger.LogInformation("Search result: MemoryId: {MemoryId}, Title: {Title}, RelevancyScore: {RelevancyScore:F1}%, RelationshipCount: {RelationshipCount}",
                memory.Id, memory.Title ?? "Untitled", relevancyScore, relationshipCount);
            
            // Log relationships if they exist
            if (memory.Relationships is { Count: > 0 })
            {
                foreach (var rel in memory.Relationships)
                {
                    _logger.LogInformation("Memory relationship: MemoryId: {MemoryId}, RelationshipType: {RelationType}, FromId: {FromId}, ToId: {ToId}",
                        memory.Id, rel.Type, rel.FromMemoryId, rel.ToMemoryId);
                }
            }
        }

        // Format the results
        StringBuilder result = new();
        
        if (usedFallback)
        {
            result.AppendLine($"No results found at similarity threshold {minSimilarity:F1}, but found {memories.Count} memories at relaxed threshold {actualThreshold:F1}:");
        }
        else
        {
            result.AppendLine($"Found {memories.Count} memories:");
        }
        result.AppendLine();

        // Collect all related memory IDs for suggestion
        var relatedMemoryIds = new HashSet<Guid>();

        foreach (var memory in memories)
        {
            result.AppendLine($"ID: {memory.Id}");
            if (memory.Title != null)
            {
                result.AppendLine($"Title: {memory.Title}");
            }
            result.AppendLine($"Type: {memory.Type}");
            result.AppendLine($"Text: {memory.Text}");
            result.AppendLine($"Source: {memory.Source}");
            result.AppendLine(
                $"Tags: {(memory.Tags != null ? string.Join(", ", memory.Tags) : "none")}"
            );
            result.AppendLine($"Confidence: {memory.Confidence:F2}");
            if (memory.Similarity.HasValue)
            {
                double percent = 100 * (1 - memory.Similarity.Value);
                result.AppendLine($"Similarity: {percent:F1}%");
            }
            // List relationships and collect related IDs
            if (memory.Relationships is { Count: > 0 })
            {
                result.AppendLine($"🔗 Relationships ({memory.Relationships.Count}):");
                foreach (var rel in memory.Relationships)
                {
                    var relatedId = rel.FromMemoryId == memory.Id ? rel.ToMemoryId : rel.FromMemoryId;
                    var direction = rel.FromMemoryId == memory.Id ? "→" : "←";
                    var relatedTitle = rel.RelatedMemoryTitle ?? "Untitled";
                    var relatedType = rel.RelatedMemoryType ?? "unknown";
                    
                    result.AppendLine($"  • [{rel.Type.ToUpper()}] {direction} \"{relatedTitle}\" ({relatedType}) [ID: {relatedId}]");
                    
                    // Collect related memory IDs (excluding the current memory)
                    if (rel.FromMemoryId != memory.Id)
                        relatedMemoryIds.Add(rel.FromMemoryId);
                    if (rel.ToMemoryId != memory.Id)
                        relatedMemoryIds.Add(rel.ToMemoryId);
                }
            }
            result.AppendLine($"Created: {memory.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine();
        }

        // Add suggestion to load related memories if any exist
        if (relatedMemoryIds.Count > 0)
        {
            result.AppendLine("💡 Suggestion: These memories have relationships to other memories in the database.");
            result.AppendLine($"Consider using GetMany with these IDs to load related context: [{string.Join(", ", relatedMemoryIds)}]");
            result.AppendLine("This can provide additional relevant information and context for your task.");
        }

        activity?.SetStatus(ActivityStatusCode.Ok, $"Found {memories.Count} results" + (usedFallback ? " (with fallback)" : ""));
        return result.ToString();
    }

    [McpServerTool, Description("Retrieve a specific memory by ID. Use this to fetch a particular reference, how-to, or example by its unique identifier.")]
    public async Task<string> Get(
        [Description("The ID of the memory to retrieve. Use this to fetch a specific piece of reference or how-to information.")] Guid id,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryConfig.ActivitySource.StartActivity("MemoryTools.Get");
        
        // Add query details as Activity event
        activity?.AddEvent(new ActivityEvent("query.details", DateTimeOffset.UtcNow, new ActivityTagsCollection
        {
            {"query.id", id.ToString()}
        }));

        Memory? memory = await _storage.Get(id, cancellationToken);

        if (memory == null)
        {
            _logger.LogInformation("Memory not found for ID: {MemoryId}", id);
            activity?.SetStatus(ActivityStatusCode.Ok, "Memory not found");
            return $"Memory with ID {id} not found.";
        }

        // Log result details
        var relationshipCount = memory.Relationships?.Count ?? 0;
        _logger.LogInformation("Memory retrieved: MemoryId: {MemoryId}, Title: {Title}, Type: {Type}, RelationshipCount: {RelationshipCount}",
            memory.Id, memory.Title ?? "Untitled", memory.Type, relationshipCount);

        // Log relationships if they exist
        if (memory.Relationships is { Count: > 0 })
        {
            foreach (var rel in memory.Relationships)
            {
                _logger.LogInformation("Memory relationship: MemoryId: {MemoryId}, RelationshipType: {RelationType}, FromId: {FromId}, ToId: {ToId}",
                    memory.Id, rel.Type, rel.FromMemoryId, rel.ToMemoryId);
            }
        }

        StringBuilder result = new();
        result.AppendLine($"ID: {memory.Id}");
        if (memory.Title != null)
        {
            result.AppendLine($"Title: {memory.Title}");
        }
        result.AppendLine($"Type: {memory.Type}");
        result.AppendLine($"Text: {memory.Text}");
        result.AppendLine($"Source: {memory.Source}");
        result.AppendLine(
            $"Tags: {(memory.Tags != null ? string.Join(", ", memory.Tags) : "none")}"
        );
        result.AppendLine($"Confidence: {memory.Confidence:F2}");
        if (memory.Similarity.HasValue)
        {
            double percent = 100 * (1 - memory.Similarity.Value);
            result.AppendLine($"Similarity: {percent:F1}%");
        }
        
        // Collect related memory IDs for suggestion
        var relatedMemoryIds = new HashSet<Guid>();
        
        // List relationships
        if (memory.Relationships != null && memory.Relationships.Count > 0)
        {
            result.AppendLine($"🔗 Relationships ({memory.Relationships.Count}):");
            foreach (var rel in memory.Relationships)
            {
                var relatedId = rel.FromMemoryId == memory.Id ? rel.ToMemoryId : rel.FromMemoryId;
                var direction = rel.FromMemoryId == memory.Id ? "→" : "←";
                var relatedTitle = rel.RelatedMemoryTitle ?? "Untitled";
                var relatedType = rel.RelatedMemoryType ?? "unknown";
                
                result.AppendLine($"  • [{rel.Type.ToUpper()}] {direction} \"{relatedTitle}\" ({relatedType}) [ID: {relatedId}]");
                
                // Collect related memory IDs (excluding the current memory)
                if (rel.FromMemoryId != memory.Id)
                    relatedMemoryIds.Add(rel.FromMemoryId);
                if (rel.ToMemoryId != memory.Id)
                    relatedMemoryIds.Add(rel.ToMemoryId);
            }
        }
        result.AppendLine($"Created: {memory.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        result.AppendLine($"Updated: {memory.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

        // Add suggestion to load related memories if any exist
        if (relatedMemoryIds.Count > 0)
        {
            result.AppendLine();
            result.AppendLine("💡 Suggestion: This memory has relationships to other memories in the database.");
            result.AppendLine($"Consider using GetMany with these IDs to load related context: [{string.Join(", ", relatedMemoryIds)}]");
            result.AppendLine("This can provide additional relevant information and context for your task.");
        }

        activity?.SetStatus(ActivityStatusCode.Ok, "Memory retrieved successfully");
        return result.ToString();
    }

    [McpServerTool, Description("Delete a memory by ID. Use this to remove outdated or incorrect reference or how-to information.")]
    public async Task<string> Delete(
        [Description("The ID of the memory to delete. Use this to remove a specific piece of knowledge.")] Guid id,
        CancellationToken cancellationToken = default
    )
    {
        bool success = await _storage.Delete(id, cancellationToken);

        return success ? $"Memory with ID {id} deleted successfully." : $"Memory with ID {id} not found or could not be deleted.";
    }

    [McpServerTool, Description("Fetch multiple memories by their IDs. Use this to retrieve a set of related reference materials, how-tos, or examples.")]
    public async Task<string> GetMany(
        [Description("The list of memory IDs to fetch. Use this to retrieve multiple related pieces of knowledge at once.")] Guid[] ids,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = TelemetryConfig.ActivitySource.StartActivity("MemoryTools.GetMany");
        
        // Add query details as Activity event
        activity?.AddEvent(new ActivityEvent("query.details", DateTimeOffset.UtcNow, new ActivityTagsCollection
        {
            {"query.ids", string.Join(", ", ids)},
            {"query.count", ids.Length.ToString()}
        }));

        var memories = await _storage.GetMany(ids, cancellationToken);
        
        // Log results count
        _logger.LogInformation("GetMany completed. RequestedCount: {RequestedCount}, FoundCount: {FoundCount}", ids.Length, memories.Count);
        
        if (memories.Count == 0)
        {
            activity?.SetStatus(ActivityStatusCode.Ok, "No memories found");
            return "No memories found for the provided IDs.";
        }
        
        // Log details for each retrieved memory
        foreach (var memory in memories)
        {
            _logger.LogInformation("Memory retrieved: MemoryId: {MemoryId}, Title: {Title}, Type: {Type}",
                memory.Id, memory.Title ?? "Untitled", memory.Type);
        }

        StringBuilder result = new();
        result.AppendLine($"Found {memories.Count} memories:");
        result.AppendLine();
        
        // Collect all related memory IDs for suggestion
        var relatedMemoryIds = new HashSet<Guid>();
        
        foreach (var memory in memories)
        {
            result.AppendLine($"ID: {memory.Id}");
            if (memory.Title != null)
            {
                result.AppendLine($"Title: {memory.Title}");
            }
            result.AppendLine($"Type: {memory.Type}");
            result.AppendLine($"Text: {memory.Text}");
            result.AppendLine($"Source: {memory.Source}");
            result.AppendLine($"Tags: {(memory.Tags != null ? string.Join(", ", memory.Tags) : "none")}");
            result.AppendLine($"Confidence: {memory.Confidence:F2}");
            
            // List relationships and collect related IDs
            if (memory.Relationships is { Count: > 0 })
            {
                result.AppendLine($"🔗 Relationships ({memory.Relationships.Count}):");
                foreach (var rel in memory.Relationships)
                {
                    var relatedId = rel.FromMemoryId == memory.Id ? rel.ToMemoryId : rel.FromMemoryId;
                    var direction = rel.FromMemoryId == memory.Id ? "→" : "←";
                    var relatedTitle = rel.RelatedMemoryTitle ?? "Untitled";
                    var relatedType = rel.RelatedMemoryType ?? "unknown";
                    
                    result.AppendLine($"  • [{rel.Type.ToUpper()}] {direction} \"{relatedTitle}\" ({relatedType}) [ID: {relatedId}]");
                    
                    // Collect related memory IDs (excluding memories we already have)
                    if (rel.FromMemoryId != memory.Id && !ids.Contains(rel.FromMemoryId))
                        relatedMemoryIds.Add(rel.FromMemoryId);
                    if (rel.ToMemoryId != memory.Id && !ids.Contains(rel.ToMemoryId))
                        relatedMemoryIds.Add(rel.ToMemoryId);
                }
            }
            
            result.AppendLine($"Created: {memory.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            result.AppendLine();
        }
        
        // Add suggestion to load related memories if any exist
        if (relatedMemoryIds.Count > 0)
        {
            result.AppendLine("💡 Suggestion: These memories have relationships to other memories not included in this result.");
            result.AppendLine($"Consider using GetMany with these additional IDs to load more related context: [{string.Join(", ", relatedMemoryIds)}]");
            result.AppendLine("This can provide additional relevant information and context for your task.");
        }
        
        activity?.SetStatus(ActivityStatusCode.Ok, $"Retrieved {memories.Count} memories");
        return result.ToString();
    }

    [McpServerTool, Description("Create a relationship between two memories. Use this to link related reference materials, how-tos, or examples (e.g., 'example-of', 'explains', 'related-to'). Relationships help organize knowledge for easier retrieval and understanding.")]
    public async Task<string> CreateRelationship(
        [Description("The ID of the source memory (e.g., the reference or how-to that is providing context)")] Guid fromId,
        [Description("The ID of the target memory (e.g., the example or related reference)")] Guid toId,
        [Description("The type of relationship (e.g., 'example-of', 'explains', 'related-to'). Use relationships to connect and organize knowledge.")] string type,
        CancellationToken cancellationToken = default
    )
    {
        var rel = await _storage.CreateRelationship(fromId, toId, type, cancellationToken);
        return $"Relationship created: {rel.Id} from {rel.FromMemoryId} to {rel.ToMemoryId} (type: {rel.Type})";
    }
    
    [McpServerTool, Description(@"Search the graph database using natural language queries. This uses LLM to convert natural language to Cypher queries for Neo4j graph traversal.

## Supported Query Types:
### Memory Type Searches:
- 'Find all reference documents' - retrieves memories of type 'reference'
- 'Show how-to guides' - finds all how-to type memories
- 'Get system memories' - fetches system configuration memories

### Keyword and Tag Searches:
- 'Find memories about Docker or Kubernetes' - searches by keywords
- 'Show memories related to AI' - finds AI-related content by title, summary, or tags
- 'Search for reactive programming' - discovers content with reactive programming keywords

### Relationship Exploration:
- 'Find memories that extend DDD concepts' - discovers extension relationships
- 'Show enhanced versions' - finds memories with enhanced-version relationships
- 'Find examples of patterns' - locates example-of relationships
- 'Show memories that support each other' - finds supporting relationships

### Advanced Analysis:
- 'Find the most connected memories' - identifies hub nodes with many connections
- 'Show most frequent keywords' - analyzes keyword usage patterns
- 'Find memories with common keywords' - discovers related content through shared keywords
- 'Recent high-confidence memories' - filters by confidence and recency

### Graph Patterns:
- 'Find reference documents with examples' - explores reference-example relationships
- 'Show knowledge clusters' - identifies connected memory groups
- 'Find hub memories' - locates highly connected nodes
- 'Trace relationship chains' - follows relationship paths

### Source and Confidence:
- 'Find LLM-generated memories' - filters by source
- 'High confidence memory network' - filters by confidence scores
- 'Memory evolution over time' - tracks memory enhancements

Returns nodes with their properties (id, title, type, tags, summary) and relationships with types (extends, enhanced-version, supports, contradicts, implements, references, related-to, example-of, explains).")]
    public async Task<string> SearchGraph(
        [Description("Natural language query to search the graph. Examples: 'Find all reference documents with examples', 'Show the most connected memories about Docker', 'Find memories that extend domain-driven design concepts', 'Show hub memories with high confidence', 'Find knowledge clusters about AI'")] string query,
        CancellationToken cancellationToken = default
    )
    {
        if (_graphSearchService == null)
        {
            return "Graph search is not available. Neo4j integration may not be configured.";
        }
        
        try
        {
            using var activity = TelemetryConfig.ActivitySource.StartActivity("MemoryTools.SearchGraph");
            
            activity?.AddEvent(new ActivityEvent("query.details", DateTimeOffset.UtcNow, new ActivityTagsCollection
            {
                {"query.text", query}
            }));
            
            _logger.LogInformation("Executing graph search: {Query}", query);
            
            var result = await _graphSearchService.SearchGraphAsync(query);
            
            var sb = new StringBuilder();
            sb.AppendLine($"🔍 Graph Search Results: {result.Nodes.Count} nodes and {result.Relationships.Count} relationships");
            sb.AppendLine();
            
            // Group nodes by type for better organization
            var nodesByType = result.Nodes.GroupBy(n => n.Type).OrderBy(g => g.Key);
            var hubNodes = result.Nodes.Where(n => n.Metadata?.ContainsKey("isHub") == true && (bool)n.Metadata["isHub"]).ToList();
            
            if (hubNodes.Count > 0)
            {
                sb.AppendLine("🌟 Hub Nodes (highly connected):");
                foreach (var hub in hubNodes.Take(5))
                {
                    var connections = hub.Metadata?.ContainsKey("connectionCount") == true ? hub.Metadata["connectionCount"] : 0;
                    sb.AppendLine($"  • {hub.Title ?? "Untitled"} - {connections} connections (ID: {hub.Id})");
                }
                sb.AppendLine();
            }
            
            if (result.Nodes.Count > 0)
            {
                sb.AppendLine("📊 Nodes by Type:");
                foreach (var typeGroup in nodesByType)
                {
                    var nodeType = typeGroup.Key ?? "Unknown";
                    sb.AppendLine($"\n  [{nodeType.ToUpper()}] ({typeGroup.Count()} nodes):");
                    
                    foreach (var node in typeGroup.Take(10))
                    {
                        var connectionInfo = "";
                        if (node.Metadata?.ContainsKey("connectionCount") == true && (int)node.Metadata["connectionCount"] > 0)
                        {
                            connectionInfo = $" [{node.Metadata["connectionCount"]} connections]";
                        }
                        
                        sb.AppendLine($"    • {node.Title ?? "Untitled"}{connectionInfo}");
                        sb.AppendLine($"      ID: {node.Id}");
                        
                        if (node.Tags?.Count > 0)
                        {
                            sb.AppendLine($"      Tags: {string.Join(", ", node.Tags.Take(5))}");
                        }
                        
                        if (!string.IsNullOrEmpty(node.Summary))
                        {
                            var summaryPreview = node.Summary.Length > 150 
                                ? node.Summary.Substring(0, 147) + "..." 
                                : node.Summary;
                            sb.AppendLine($"      Summary: {summaryPreview}");
                        }
                        
                        if (node.Metadata?.ContainsKey("frequency") == true && nodeType == "Word")
                        {
                            sb.AppendLine($"      Frequency: {node.Metadata["frequency"]}");
                        }
                    }
                    
                    if (typeGroup.Count() > 10)
                    {
                        sb.AppendLine($"    ... and {typeGroup.Count() - 10} more {nodeType} nodes");
                    }
                }
            }
            
            if (result.Relationships.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("🔗 Relationships:");
                
                // Group relationships by type
                var relsByType = result.Relationships.GroupBy(r => 
                {
                    if (r.Metadata?.ContainsKey("relationshipSubtype") == true)
                        return r.Metadata["relationshipSubtype"].ToString();
                    return r.Type;
                }).OrderBy(g => g.Key);
                
                foreach (var relGroup in relsByType)
                {
                    sb.AppendLine($"\n  [{relGroup.Key?.ToUpper() ?? "UNKNOWN"}] ({relGroup.Count()} relationships):");
                    
                    foreach (var rel in relGroup.Take(5))
                    {
                        var fromNode = result.Nodes.FirstOrDefault(n => n.Id == rel.FromId);
                        var toNode = result.Nodes.FirstOrDefault(n => n.Id == rel.ToId);
                        
                        var fromTitle = fromNode?.Title ?? rel.FromId.ToString().Substring(0, 8) + "...";
                        var toTitle = toNode?.Title ?? rel.ToId.ToString().Substring(0, 8) + "...";
                        
                        var weightInfo = rel.Weight != 1.0 ? $" (weight: {rel.Weight:F2})" : "";
                        sb.AppendLine($"    • {fromTitle} → {toTitle}{weightInfo}");
                    }
                    
                    if (relGroup.Count() > 5)
                    {
                        sb.AppendLine($"    ... and {relGroup.Count() - 5} more {relGroup.Key} relationships");
                    }
                }
            }
            
            // Add summary statistics
            sb.AppendLine();
            sb.AppendLine("📈 Summary:");
            sb.AppendLine($"  • Total Nodes: {result.Nodes.Count}");
            sb.AppendLine($"  • Total Relationships: {result.Relationships.Count}");
            sb.AppendLine($"  • Node Types: {string.Join(", ", nodesByType.Select(g => $"{g.Key} ({g.Count()})"))}");
            if (hubNodes.Count > 0)
            {
                sb.AppendLine($"  • Hub Nodes: {hubNodes.Count}");
            }
            
            // Add suggestions for further exploration
            if (result.Nodes.Count > 0 && result.Nodes.Count < 50)
            {
                sb.AppendLine();
                sb.AppendLine("💡 Suggestions:");
                sb.AppendLine("  • Use GetMany to retrieve full content for specific nodes");
                sb.AppendLine("  • Search for related memories using common tags or keywords");
                if (hubNodes.Count > 0)
                {
                    sb.AppendLine($"  • Explore hub node '{hubNodes.First().Title}' for rich connections");
                }
            }
            
            activity?.SetStatus(ActivityStatusCode.Ok, $"Found {result.Nodes.Count} nodes, {result.Relationships.Count} relationships");
            
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing graph search: {Query}", query);
            return $"Error executing graph search: {ex.Message}";
        }
    }
    
    [McpServerTool, Description(@"Execute a direct Cypher query on the Neo4j graph database. This tool allows advanced users to write custom Cypher queries for complex graph traversals and analysis.

## Graph Schema:
### Nodes:
1. **Memory Node**
   - id: string (UUID) - unique identifier
   - type: string - values: 'reference', 'how-to', 'system', 'conversation', 'document'
   - source: string - origin (e.g., 'LLM', 'user', 'system')
   - title: string - descriptive title
   - summary: string - detailed content
   - tags: string[] - keyword tags
   - confidence: double (0.0-1.0)
   - createdAt: string (ISO datetime)

2. **Word Node**
   - name: string - the keyword
   - language: string - language code
   - frequency: int - usage count
   - createdAt/updatedAt: string (ISO datetime)

### Relationships:
1. **RELATES_TO** (Memory -> Memory)
   - type: string - subtype (extends, enhanced-version, supports, contradicts, implements, references, related-to, example-of, explains)
   - weight: double (0.0-1.0)
   - createdAt: string (ISO datetime)

2. **HAS_KEYWORD** (Memory -> Word)
   - relevance: double (0.0-1.0)
   - createdAt: string (ISO datetime)

## Query Guidelines:
- Use MATCH for pattern matching
- Use WHERE for filtering conditions
- Use RETURN to specify output
- Use OPTIONAL MATCH for optional patterns
- Use LIMIT to restrict results (recommended: max 100)
- Use ORDER BY for sorting
- Use WITH for query chaining
- Use COLLECT() for aggregations
- Use DISTINCT to avoid duplicates

## Example Queries:
### Basic Patterns:
```cypher
# Find all reference documents
MATCH (m:Memory {type: 'reference'}) RETURN m LIMIT 50

# Find memories with specific tag
MATCH (m:Memory) WHERE 'docker' IN m.tags RETURN m

# Find memories by keyword
MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word {name: 'kubernetes'}) RETURN m, w
```

### Relationship Queries:
```cypher
# Find extension relationships
MATCH (m1:Memory)-[r:RELATES_TO {type: 'extends'}]->(m2:Memory) RETURN m1, r, m2 LIMIT 30

# Find memories with examples
MATCH (ref:Memory {type: 'reference'})<-[:RELATES_TO {type: 'example-of'}]-(example:Memory) RETURN ref, example

# Find connected memory clusters
MATCH path = (m1:Memory)-[:RELATES_TO*1..3]-(m2:Memory) RETURN path LIMIT 20
```

### Advanced Analysis:
```cypher
# Find hub nodes (highly connected)
MATCH (m:Memory)
WITH m, SIZE([(m)-[]-() | 1]) as degree
WHERE degree > 3
RETURN m, degree ORDER BY degree DESC LIMIT 20

# Find memories with common keywords
MATCH (m1:Memory)-[:HAS_KEYWORD]->(w:Word)<-[:HAS_KEYWORD]-(m2:Memory)
WHERE id(m1) < id(m2)
WITH m1, m2, COLLECT(DISTINCT w.name) as common_keywords
WHERE SIZE(common_keywords) > 2
RETURN m1, m2, common_keywords

# Recent high-confidence memories
MATCH (m:Memory)
WHERE m.confidence > 0.8 AND m.createdAt > datetime() - duration('P30D')
RETURN m ORDER BY m.createdAt DESC LIMIT 30
```

## IMPORTANT:
- This tool executes READ-ONLY queries
- Avoid queries that modify data (CREATE, MERGE, SET, DELETE)
- Complex queries may impact performance
- Always include LIMIT clause for large result sets
- Results are formatted as nodes and relationships")]
    public async Task<string> SearchGraphByCypher(
        [Description(@"The Cypher query to execute. Must be a valid Neo4j Cypher query. Example: 'MATCH (m:Memory {type: ""reference""}) RETURN m LIMIT 10'. Use single quotes for string literals in WHERE clauses, double quotes for property values in node patterns.")] string cypherQuery,
        CancellationToken cancellationToken = default
    )
    {
        if (_graphSearchService == null)
        {
            return "Graph search is not available. Neo4j integration may not be configured.";
        }
        
        try
        {
            using var activity = TelemetryConfig.ActivitySource.StartActivity("MemoryTools.SearchGraphByCypher");
            
            // Basic validation to ensure it's a read-only query
            var queryUpper = cypherQuery.ToUpperInvariant();
            var modifyingKeywords = new[] { "CREATE", "MERGE", "SET", "DELETE", "REMOVE", "DROP", "DETACH" };
            
            foreach (var keyword in modifyingKeywords)
            {
                if (queryUpper.Contains(keyword))
                {
                    _logger.LogWarning("Attempted to execute modifying Cypher query: {Query}", cypherQuery);
                    return $"Error: This tool only supports read-only queries. Modifying operations ({keyword}) are not allowed.";
                }
            }
            
            // Add a default LIMIT if not present to prevent overwhelming results
            if (!queryUpper.Contains("LIMIT"))
            {
                cypherQuery = cypherQuery.TrimEnd(';', ' ') + " LIMIT 100";
                _logger.LogInformation("Added default LIMIT 100 to query without limit clause");
            }
            
            activity?.AddEvent(new ActivityEvent("query.details", DateTimeOffset.UtcNow, new ActivityTagsCollection
            {
                {"query.cypher", cypherQuery}
            }));
            
            _logger.LogInformation("Executing direct Cypher query: {Query}", cypherQuery);
            
            // Execute the query directly through GraphRepository
            var records = await _graphSearchService.ExecuteRawCypherQuery(cypherQuery);
            
            var result = new GraphSearchResult();
            var nodeIds = new HashSet<string>();
            var wordNodeIds = new HashSet<string>();
            var relationships = new List<GraphRelationship>();
            var nodeConnectionCounts = new Dictionary<string, int>();
            
            // Process the results similar to SearchGraphAsync
            foreach (var record in records)
            {
                foreach (var value in record.Values.Values)
                {
                    if (value is INode node)
                    {
                        await ProcessNodeForCypher(node, nodeIds, wordNodeIds, result, nodeConnectionCounts);
                    }
                    else if (value is IRelationship relationship)
                    {
                        await ProcessRelationshipForCypher(relationship, relationships, nodeConnectionCounts);
                    }
                    else if (value is IPath path)
                    {
                        await ProcessPathForCypher(path, nodeIds, wordNodeIds, result, relationships, nodeConnectionCounts);
                    }
                }
            }
            
            result.Relationships = relationships;
            
            // Format the results
            var sb = new StringBuilder();
            sb.AppendLine($"🔍 Direct Cypher Query Results: {result.Nodes.Count} nodes and {result.Relationships.Count} relationships");
            sb.AppendLine();
            
            if (result.Nodes.Count > 0)
            {
                // Group nodes by type
                var nodesByType = result.Nodes.GroupBy(n => n.Type).OrderBy(g => g.Key);
                
                sb.AppendLine("📊 Nodes by Type:");
                foreach (var typeGroup in nodesByType)
                {
                    var nodeType = typeGroup.Key ?? "Unknown";
                    sb.AppendLine($"\n  [{nodeType.ToUpper()}] ({typeGroup.Count()} nodes):");
                    
                    foreach (var node in typeGroup.Take(20))
                    {
                        sb.AppendLine($"    • {node.Title ?? "Untitled"}");
                        sb.AppendLine($"      ID: {node.Id}");
                        
                        if (node.Tags?.Count > 0)
                        {
                            sb.AppendLine($"      Tags: {string.Join(", ", node.Tags.Take(5))}");
                        }
                        
                        if (!string.IsNullOrEmpty(node.Summary))
                        {
                            var summaryPreview = node.Summary.Length > 150 
                                ? node.Summary.Substring(0, 147) + "..." 
                                : node.Summary;
                            sb.AppendLine($"      Summary: {summaryPreview}");
                        }
                    }
                    
                    if (typeGroup.Count() > 20)
                    {
                        sb.AppendLine($"    ... and {typeGroup.Count() - 20} more {nodeType} nodes");
                    }
                }
            }
            
            if (result.Relationships.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("🔗 Relationships:");
                
                var relsByType = result.Relationships.GroupBy(r => r.Type).OrderBy(g => g.Key);
                
                foreach (var relGroup in relsByType)
                {
                    sb.AppendLine($"\n  [{relGroup.Key?.ToUpper() ?? "UNKNOWN"}] ({relGroup.Count()} relationships):");
                    
                    foreach (var rel in relGroup.Take(10))
                    {
                        sb.AppendLine($"    • {rel.FromId} → {rel.ToId}");
                    }
                    
                    if (relGroup.Count() > 10)
                    {
                        sb.AppendLine($"    ... and {relGroup.Count() - 10} more {relGroup.Key} relationships");
                    }
                }
            }
            
            if (result.Nodes.Count == 0 && result.Relationships.Count == 0)
            {
                sb.AppendLine("No results found for the given query.");
            }
            
            activity?.SetStatus(ActivityStatusCode.Ok, $"Found {result.Nodes.Count} nodes, {result.Relationships.Count} relationships");
            
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Cypher query: {Query}", cypherQuery);
            return $"Error executing Cypher query: {ex.Message}\nPlease ensure your query syntax is valid and follows Neo4j Cypher conventions.";
        }
    }
    
    // Helper methods for SearchGraphByCypher
    private Task ProcessNodeForCypher(INode node, HashSet<string> nodeIds, HashSet<string> wordNodeIds, 
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
                    Id = Guid.NewGuid(),
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
                
                if (!connectionCounts.ContainsKey(nodeId))
                    connectionCounts[nodeId] = 0;
                    
                result.Nodes.Add(memoryNode);
            }
        }
        return Task.CompletedTask;
    }
    
    private Task ProcessRelationshipForCypher(IRelationship relationship, List<GraphRelationship> relationships,
        Dictionary<string, int> connectionCounts)
    {
        try
        {
            // For Cypher queries, we might not have access to GetNodeById, so we'll create simplified relationships
            var rel = new GraphRelationship
            {
                // We'll use placeholder GUIDs if we can't resolve the actual node IDs
                FromId = Guid.NewGuid(),
                ToId = Guid.NewGuid(),
                Type = relationship.Type,
                Weight = relationship.Properties.ContainsKey("weight") 
                    ? relationship["weight"].As<double>() 
                    : 1.0,
                CreatedAt = relationship.Properties.ContainsKey("createdAt")
                    ? DateTime.Parse(relationship["createdAt"].As<string>())
                    : DateTime.UtcNow,
                Metadata = new Dictionary<string, object>()
            };
            
            if (relationship.Properties.ContainsKey("type"))
            {
                rel.Metadata["relationshipSubtype"] = relationship["type"].As<string>();
            }
            
            relationships.Add(rel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process relationship in Cypher query result");
        }
        
        return Task.CompletedTask;
    }
    
    private async Task ProcessPathForCypher(IPath path, HashSet<string> nodeIds, HashSet<string> wordNodeIds,
        GraphSearchResult result, List<GraphRelationship> relationships, Dictionary<string, int> connectionCounts)
    {
        foreach (var pathNode in path.Nodes)
        {
            await ProcessNodeForCypher(pathNode, nodeIds, wordNodeIds, result, connectionCounts);
        }
        
        foreach (var pathRelationship in path.Relationships)
        {
            await ProcessRelationshipForCypher(pathRelationship, relationships, connectionCounts);
        }
    }
}
