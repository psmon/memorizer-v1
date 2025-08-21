using Akka.Actor;
using Memorizer.Services;
using Memorizer.Models;
using System.Text.Json;

namespace Memorizer.Actors;

public class GraphRelationshipActor : ReceiveActor
{
    private readonly IGraphSyncService _graphSyncService;
    private readonly ILlmService _llmService;
    private readonly ILogger<GraphRelationshipActor> _logger;
    
    public GraphRelationshipActor(
        IGraphSyncService graphSyncService,
        ILlmService llmService,
        ILogger<GraphRelationshipActor> logger)
    {
        _graphSyncService = graphSyncService;
        _llmService = llmService;
        _logger = logger;
        
        ReceiveAsync<ProcessMemoryForRelationships>(HandleProcessMemory);
        ReceiveAsync<CreateRelationshipsFromSuggestions>(HandleCreateRelationships);
        ReceiveAsync<BatchProcessMemories>(HandleBatchProcess);
    }
    
    private async Task HandleProcessMemory(ProcessMemoryForRelationships message)
    {
        try
        {
            _logger.LogInformation("Processing memory {MemoryId} for relationship suggestions", message.MemoryId);
            
            var suggestions = await _graphSyncService.SuggestRelationshipsAsync(message.MemoryId);
            
            if (suggestions.Any() && message.AutoCreate)
            {
                foreach (var suggestion in suggestions.Where(s => s.Weight >= message.MinConfidence))
                {
                    await _graphSyncService.CreateGraphRelationshipAsync(
                        suggestion.FromId,
                        suggestion.ToId,
                        suggestion.Type,
                        new Dictionary<string, object> { ["weight"] = suggestion.Weight });
                    
                    _logger.LogInformation(
                        "Created relationship: {FromId} -{Type}-> {ToId} (weight: {Weight})",
                        suggestion.FromId, suggestion.Type, suggestion.ToId, suggestion.Weight);
                }
            }
            
            Sender.Tell(new RelationshipProcessingResult
            {
                MemoryId = message.MemoryId,
                Success = true,
                SuggestionsCount = suggestions.Count,
                CreatedCount = message.AutoCreate ? suggestions.Count(s => s.Weight >= message.MinConfidence) : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing memory {MemoryId} for relationships", message.MemoryId);
            
            Sender.Tell(new RelationshipProcessingResult
            {
                MemoryId = message.MemoryId,
                Success = false,
                Error = ex.Message
            });
        }
    }
    
    private async Task HandleCreateRelationships(CreateRelationshipsFromSuggestions message)
    {
        try
        {
            var createdCount = 0;
            
            foreach (var suggestion in message.Suggestions)
            {
                var success = await _graphSyncService.CreateGraphRelationshipAsync(
                    suggestion.FromId,
                    suggestion.ToId,
                    suggestion.Type,
                    suggestion.Properties);
                
                if (success)
                {
                    createdCount++;
                }
            }
            
            Sender.Tell(new RelationshipCreationResult
            {
                Success = true,
                CreatedCount = createdCount,
                TotalRequested = message.Suggestions.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating relationships from suggestions");
            
            Sender.Tell(new RelationshipCreationResult
            {
                Success = false,
                Error = ex.Message
            });
        }
    }
    
    private async Task HandleBatchProcess(BatchProcessMemories message)
    {
        var results = new List<RelationshipProcessingResult>();
        
        foreach (var memoryId in message.MemoryIds)
        {
            try
            {
                var suggestions = await _graphSyncService.SuggestRelationshipsAsync(memoryId);
                var createdCount = 0;
                
                if (message.AutoCreate)
                {
                    foreach (var suggestion in suggestions.Where(s => s.Weight >= message.MinConfidence))
                    {
                        var success = await _graphSyncService.CreateGraphRelationshipAsync(
                            suggestion.FromId,
                            suggestion.ToId,
                            suggestion.Type,
                            new Dictionary<string, object> { ["weight"] = suggestion.Weight });
                        
                        if (success) createdCount++;
                    }
                }
                
                results.Add(new RelationshipProcessingResult
                {
                    MemoryId = memoryId,
                    Success = true,
                    SuggestionsCount = suggestions.Count,
                    CreatedCount = createdCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing memory {MemoryId} in batch", memoryId);
                
                results.Add(new RelationshipProcessingResult
                {
                    MemoryId = memoryId,
                    Success = false,
                    Error = ex.Message
                });
            }
        }
        
        Sender.Tell(new BatchProcessingResult
        {
            Results = results,
            TotalProcessed = results.Count,
            SuccessCount = results.Count(r => r.Success)
        });
    }
    
    public static Props Props(
        IGraphSyncService graphSyncService,
        ILlmService llmService,
        ILogger<GraphRelationshipActor> logger)
    {
        return Akka.Actor.Props.Create(() => 
            new GraphRelationshipActor(graphSyncService, llmService, logger));
    }
}

public class ProcessMemoryForRelationships
{
    public Guid MemoryId { get; set; }
    public bool AutoCreate { get; set; } = false;
    public double MinConfidence { get; set; } = 0.7;
}

public class CreateRelationshipsFromSuggestions
{
    public List<GraphRelationshipSuggestion> Suggestions { get; set; } = new();
}

public class GraphRelationshipSuggestion
{
    public Guid FromId { get; set; }
    public Guid ToId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class BatchProcessMemories
{
    public List<Guid> MemoryIds { get; set; } = new();
    public bool AutoCreate { get; set; } = false;
    public double MinConfidence { get; set; } = 0.7;
}

public class RelationshipProcessingResult
{
    public Guid MemoryId { get; set; }
    public bool Success { get; set; }
    public int SuggestionsCount { get; set; }
    public int CreatedCount { get; set; }
    public string? Error { get; set; }
}

public class RelationshipCreationResult
{
    public bool Success { get; set; }
    public int CreatedCount { get; set; }
    public int TotalRequested { get; set; }
    public string? Error { get; set; }
}

public class BatchProcessingResult
{
    public List<RelationshipProcessingResult> Results { get; set; } = new();
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
}