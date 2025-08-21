using Memorizer.Models;
using Neo4j.Driver;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;

namespace Memorizer.Services;

public interface IGraphSyncService
{
    Task<int> SyncMemoriesToGraphAsync(bool fullSync = false);
    Task<bool> CreateGraphRelationshipAsync(Guid fromId, Guid toId, string relationshipType, Dictionary<string, object>? properties = null);
    Task<List<GraphRelationship>> SuggestRelationshipsAsync(Guid memoryId);
    Task<GraphVisualizationData> GetGraphVisualizationAsync(int limit = 100);
    Task<bool> InitializeGraphSchemaAsync();
    Task CreateOrUpdateGraphNodeAsync(Memory memory);
}

public class GraphSyncService : IGraphSyncService
{
    private readonly IGraphRepository _graphRepository;
    private readonly IConfiguration _configuration;
    private readonly ILlmService _llmService;
    private readonly ILogger<GraphSyncService> _logger;
    private readonly string _postgresConnectionString;
    
    public GraphSyncService(
        IGraphRepository graphRepository,
        IConfiguration configuration,
        ILlmService llmService,
        ILogger<GraphSyncService> logger)
    {
        _graphRepository = graphRepository;
        _configuration = configuration;
        _llmService = llmService;
        _logger = logger;
        _postgresConnectionString = configuration.GetConnectionString("Storage") 
            ?? throw new InvalidOperationException("Storage connection string not configured");
    }
    
    public async Task<bool> InitializeGraphSchemaAsync()
    {
        try
        {
            await _graphRepository.ExecuteWriteAsync(async tx =>
            {
                var queries = new[]
                {
                    "CREATE CONSTRAINT memory_id IF NOT EXISTS FOR (m:Memory) REQUIRE m.id IS UNIQUE",
                    "CREATE INDEX memory_type IF NOT EXISTS FOR (m:Memory) ON (m.type)",
                    "CREATE INDEX memory_source IF NOT EXISTS FOR (m:Memory) ON (m.source)",
                    "CREATE INDEX memory_created IF NOT EXISTS FOR (m:Memory) ON (m.createdAt)",
                    "CREATE INDEX relationship_type IF NOT EXISTS FOR ()-[r:RELATES_TO]-() ON (r.type)"
                };
                
                foreach (var query in queries)
                {
                    await tx.RunAsync(query);
                }
            });
            
            _logger.LogInformation("Neo4j graph schema initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Neo4j graph schema");
            return false;
        }
    }
    
    public async Task<int> SyncMemoriesToGraphAsync(bool fullSync = false)
    {
        var syncedCount = 0;
        
        try
        {
            await using var pgConnection = new NpgsqlConnection(_postgresConnectionString);
            await pgConnection.OpenAsync();
            
            var lastSyncTime = fullSync ? DateTime.MinValue : await GetLastSyncTimeAsync();
            
            var query = @"
                SELECT id, type, source, title, tags, confidence, created_at, text
                FROM memories 
                WHERE created_at > @lastSync
                ORDER BY created_at";
            
            await using var cmd = new NpgsqlCommand(query, pgConnection);
            cmd.Parameters.AddWithValue("lastSync", lastSyncTime);
            
            await using var reader = await cmd.ExecuteReaderAsync();
            var memories = new List<Memory>();
            
            while (await reader.ReadAsync())
            {
                var memory = new Memory
                {
                    Id = reader.GetGuid(0),
                    Type = reader.GetString(1),
                    Source = reader.GetString(2),
                    Title = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Tags = reader.IsDBNull(4) ? Array.Empty<string>() : (string[])reader.GetValue(4),
                    Confidence = reader.GetDouble(5),
                    CreatedAt = reader.GetDateTime(6),
                    Text = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
                };
                memories.Add(memory);
            }
            
            reader.Close();
            
            foreach (var memory in memories)
            {
                await CreateOrUpdateGraphNodeAsync(memory);
                syncedCount++;
            }
            
            var relationshipsQuery = @"
                SELECT from_memory_id, to_memory_id, type, created_at
                FROM memory_relationships
                WHERE created_at > @lastSync";
            
            await using var relCmd = new NpgsqlCommand(relationshipsQuery, pgConnection);
            relCmd.Parameters.AddWithValue("lastSync", lastSyncTime);
            
            await using var relReader = await relCmd.ExecuteReaderAsync();
            
            while (await relReader.ReadAsync())
            {
                var fromId = relReader.GetGuid(0);
                var toId = relReader.GetGuid(1);
                var type = relReader.GetString(2);
                
                await CreateGraphRelationshipAsync(fromId, toId, type);
            }
            
            await UpdateLastSyncTimeAsync(DateTime.UtcNow);
            
            _logger.LogInformation("Synced {Count} memories to Neo4j graph", syncedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing memories to graph");
            throw;
        }
        
        return syncedCount;
    }
    
    public async Task CreateOrUpdateGraphNodeAsync(Memory memory)
    {
        await _graphRepository.ExecuteWriteAsync(async tx =>
        {
            var query = @"
                MERGE (m:Memory {id: $id})
                SET m.type = $type,
                    m.source = $source,
                    m.title = $title,
                    m.tags = $tags,
                    m.confidence = $confidence,
                    m.createdAt = $createdAt,
                    m.summary = $summary
                RETURN m";
            
            var summary = memory.Text?.Length > 200 
                ? memory.Text.Substring(0, 200) + "..." 
                : memory.Text;
            
            var parameters = new
            {
                id = memory.Id.ToString(),
                type = memory.Type,
                source = memory.Source,
                title = memory.Title ?? "",
                tags = memory.Tags?.ToArray() ?? Array.Empty<string>(),
                confidence = memory.Confidence,
                createdAt = memory.CreatedAt.ToString("o"),
                summary = summary ?? ""
            };
            
            await tx.RunAsync(query, parameters);
        });
    }
    
    public async Task<bool> CreateGraphRelationshipAsync(
        Guid fromId, 
        Guid toId, 
        string relationshipType,
        Dictionary<string, object>? properties = null)
    {
        try
        {
            await _graphRepository.ExecuteWriteAsync(async tx =>
            {
                var query = @"
                    MATCH (from:Memory {id: $fromId})
                    MATCH (to:Memory {id: $toId})
                    MERGE (from)-[r:RELATES_TO {type: $type}]->(to)
                    SET r.createdAt = $createdAt,
                        r.weight = $weight
                    RETURN r";
                
                var parameters = new Dictionary<string, object>
                {
                    ["fromId"] = fromId.ToString(),
                    ["toId"] = toId.ToString(),
                    ["type"] = relationshipType,
                    ["createdAt"] = DateTime.UtcNow.ToString("o"),
                    ["weight"] = properties?.GetValueOrDefault("weight", 1.0) ?? 1.0
                };
                
                if (properties != null)
                {
                    foreach (var prop in properties.Where(p => p.Key != "weight"))
                    {
                        parameters[$"r.{prop.Key}"] = prop.Value;
                    }
                }
                
                await tx.RunAsync(query, parameters);
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create graph relationship from {FromId} to {ToId}", fromId, toId);
            return false;
        }
    }
    
    public async Task<List<GraphRelationship>> SuggestRelationshipsAsync(Guid memoryId)
    {
        var suggestions = new List<GraphRelationship>();
        
        try
        {
            await using var pgConnection = new NpgsqlConnection(_postgresConnectionString);
            await pgConnection.OpenAsync();
            
            var query = @"
                SELECT id, title, text, type
                FROM memories
                WHERE id = @id";
            
            await using var cmd = new NpgsqlCommand(query, pgConnection);
            cmd.Parameters.AddWithValue("id", memoryId);
            
            await using var reader = await cmd.ExecuteReaderAsync();
            
            if (!await reader.ReadAsync())
                return suggestions;
            
            var sourceMemory = new
            {
                Id = reader.GetGuid(0),
                Title = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Text = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Type = reader.GetString(3)
            };
            
            reader.Close();
            
            var candidatesQuery = @"
                SELECT m.id, m.title, m.text, m.type,
                       1 - (m.embedding_metadata <=> 
                           (SELECT embedding_metadata FROM memories WHERE id = @id)) as similarity
                FROM memories m
                WHERE m.id != @id
                  AND m.embedding_metadata IS NOT NULL
                ORDER BY similarity DESC
                LIMIT 10";
            
            await using var candCmd = new NpgsqlCommand(candidatesQuery, pgConnection);
            candCmd.Parameters.AddWithValue("id", memoryId);
            
            await using var candReader = await candCmd.ExecuteReaderAsync();
            var candidates = new List<(Guid id, string title, string text, string type, double similarity)>();
            
            while (await candReader.ReadAsync())
            {
                candidates.Add((
                    candReader.GetGuid(0),
                    candReader.IsDBNull(1) ? "" : candReader.GetString(1),
                    candReader.IsDBNull(2) ? "" : candReader.GetString(2),
                    candReader.GetString(3),
                    candReader.GetDouble(4)
                ));
            }
            
            if (candidates.Any())
            {
                var prompt = $@"Analyze the following memory and suggest relationships to other memories.
Source Memory:
Title: {sourceMemory.Title}
Type: {sourceMemory.Type}
Content: {sourceMemory.Text?.Substring(0, Math.Min(500, sourceMemory.Text.Length))}

Candidate Memories:
{string.Join("\n", candidates.Select((c, i) => $"{i + 1}. Title: {c.title}, Type: {c.type}, Similarity: {c.similarity:F2}"))}

For each relevant relationship, suggest a type from: 
- extends (extends concepts)
- supports (provides supporting evidence)
- contradicts (presents opposing view)
- implements (practical implementation)
- references (direct reference)
- related-to (general relation)

Return as JSON array with format:
[{{""targetIndex"": 1, ""type"": ""extends"", ""confidence"": 0.8}}]";

                var llmResponse = await _llmService.CompleteAsync(prompt);
                
                if (!string.IsNullOrEmpty(llmResponse))
                {
                    try
                    {
                        var suggestedRelations = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(llmResponse);
                        
                        if (suggestedRelations != null)
                        {
                            foreach (var relation in suggestedRelations)
                            {
                                if (relation.TryGetValue("targetIndex", out var indexObj) &&
                                    relation.TryGetValue("type", out var typeObj) &&
                                    relation.TryGetValue("confidence", out var confObj))
                                {
                                    var index = Convert.ToInt32(indexObj) - 1;
                                    if (index >= 0 && index < candidates.Count)
                                    {
                                        suggestions.Add(new GraphRelationship
                                        {
                                            FromId = sourceMemory.Id,
                                            ToId = candidates[index].id,
                                            Type = typeObj.ToString() ?? "related-to",
                                            Weight = Convert.ToDouble(confObj),
                                            CreatedAt = DateTime.UtcNow
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse LLM relationship suggestions");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting relationships for memory {MemoryId}", memoryId);
        }
        
        return suggestions;
    }
    
    public async Task<GraphVisualizationData> GetGraphVisualizationAsync(int limit = 100)
    {
        var visualization = new GraphVisualizationData();
        
        try
        {
            var nodes = await _graphRepository.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (m:Memory)
                    RETURN m
                    ORDER BY m.createdAt DESC
                    LIMIT $limit";
                
                var cursor = await tx.RunAsync(query, new { limit });
                var results = await cursor.ToListAsync();
                
                return results.Select(record =>
                {
                    var node = record["m"].As<INode>();
                    return new GraphNode
                    {
                        Id = node["id"].As<string>(),
                        Label = node["title"].As<string>(),
                        Type = node["type"].As<string>(),
                        Color = GetColorForType(node["type"].As<string>()),
                        Size = (int)(node["confidence"].As<double>() * 20),
                        Data = new Dictionary<string, object>
                        {
                            ["tags"] = node["tags"].As<List<string>>() ?? new List<string>(),
                            ["source"] = node["source"].As<string>(),
                            ["createdAt"] = node["createdAt"].As<string>()
                        }
                    };
                }).ToList();
            });
            
            var relationships = await _graphRepository.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (from:Memory)-[r:RELATES_TO]->(to:Memory)
                    WHERE from.id IN $nodeIds AND to.id IN $nodeIds
                    RETURN from.id AS fromId, to.id AS toId, r.type AS type, r.weight AS weight";
                
                var nodeIds = nodes.Select(n => n.Id).ToList();
                var cursor = await tx.RunAsync(query, new { nodeIds });
                var results = await cursor.ToListAsync();
                
                return results.Select(record => new GraphEdge
                {
                    Id = $"{record["fromId"].As<string>()}_{record["toId"].As<string>()}",
                    Source = record["fromId"].As<string>(),
                    Target = record["toId"].As<string>(),
                    Label = record["type"].As<string>(),
                    Weight = record["weight"].As<double>(),
                    Color = GetColorForRelationType(record["type"].As<string>())
                }).ToList();
            });
            
            visualization.Nodes = nodes;
            visualization.Edges = relationships;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting graph visualization");
        }
        
        return visualization;
    }
    
    private string GetColorForType(string type)
    {
        return type?.ToLower() switch
        {
            "reference" => "#3b82f6",
            "how-to" => "#10b981",
            "system" => "#f59e0b",
            "conversation" => "#8b5cf6",
            _ => "#6b7280"
        };
    }
    
    private string GetColorForRelationType(string type)
    {
        return type?.ToLower() switch
        {
            "extends" => "#3b82f6",
            "supports" => "#10b981",
            "contradicts" => "#ef4444",
            "implements" => "#8b5cf6",
            "references" => "#f59e0b",
            _ => "#94a3b8"
        };
    }
    
    private async Task<DateTime> GetLastSyncTimeAsync()
    {
        try
        {
            var result = await _graphRepository.ExecuteReadAsync(async tx =>
            {
                var query = "MATCH (s:SyncState) RETURN s.lastSync AS lastSync";
                var cursor = await tx.RunAsync(query);
                
                if (await cursor.FetchAsync())
                {
                    var lastSyncStr = cursor.Current["lastSync"].As<string>();
                    return DateTime.Parse(lastSyncStr);
                }
                
                return DateTime.MinValue;
            });
            
            return result;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
    
    private async Task UpdateLastSyncTimeAsync(DateTime syncTime)
    {
        await _graphRepository.ExecuteWriteAsync(async tx =>
        {
            var query = @"
                MERGE (s:SyncState)
                SET s.lastSync = $syncTime
                RETURN s";
            
            await tx.RunAsync(query, new { syncTime = syncTime.ToString("o") });
        });
    }
}