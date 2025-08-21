using Akka.Actor;
using Akka.Event;
using Memorizer.Services;
using Memorizer.Models;

namespace Memorizer.Actors;

/// <summary>
/// Actor responsible for synchronizing all memories from PostgreSQL to Neo4j Graph Database
/// with batch processing and progress tracking
/// </summary>
public sealed class GraphSyncActor : ReceiveActor
{
    private readonly IStorage _storage;
    private readonly IGraphSyncService _graphSyncService;
    private readonly IGraphRepository _graphRepository;
    private readonly ILoggingAdapter _logger;

    private BatchState? _batch;
    
    private enum Status
    {
        Idle,
        Running,
        Completed
    }

    private class BatchState
    {
        public int Outstanding { get; set; }
        public int Success { get; set; }
        public int Failure { get; set; }
        public int RelationshipsCreated { get; set; }
        
        public Status CurrentStatus => Outstanding > 0 ? Status.Running : Status.Completed;
        public List<Guid> FailedIds { get; } = new();
        public string RequestedBy { get; set; } = string.Empty;
        public int Page { get; set; } // Start at 1
        public int PageSize { get; set; }
        public DateTime StartTime { get; set; }
        public int TotalMemories { get; set; }
        public bool FullSync { get; set; }
    }

    public GraphSyncActor(
        IStorage storage,
        IGraphSyncService graphSyncService,
        IGraphRepository graphRepository)
    {
        _storage = storage;
        _graphSyncService = graphSyncService;
        _graphRepository = graphRepository;
        _logger = Context.GetLogger();

        ReceiveAsync<SyncAllMemoriesToGraph>(HandleSyncAll);
        ReceiveAsync<ProcessMemoryForGraph>(HandleProcessMemory);
        Receive<GetGraphSyncStatus>(_ => HandleGetStatus());
        ReceiveAsync<InitializeGraphSchema>(HandleInitializeSchema);
    }

    private async Task HandleInitializeSchema(InitializeGraphSchema msg)
    {
        try
        {
            _logger.Info("Initializing Neo4j graph schema");
            var success = await _graphSyncService.InitializeGraphSchemaAsync();
            
            Sender.Tell(new GraphSchemaInitialized
            {
                Success = success,
                Message = success ? "Graph schema initialized successfully" : "Failed to initialize graph schema"
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error initializing graph schema");
            Sender.Tell(new GraphSchemaInitialized
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    private async Task HandleSyncAll(SyncAllMemoriesToGraph msg)
    {
        if (_batch is { CurrentStatus: Status.Running })
        {
            // Already running a batch, decline new request
            Sender.Tell(new GraphSyncStatus(
                IsRunning: true,
                Status: _batch.CurrentStatus.ToString(),
                TotalProcessed: _batch.Success + _batch.Failure,
                TotalSuccessful: _batch.Success,
                TotalFailed: _batch.Failure,
                Outstanding: _batch.Outstanding,
                RelationshipsCreated: _batch.RelationshipsCreated,
                FailedMemoryIds: _batch.FailedIds.ToList(),
                StartTime: _batch.StartTime,
                Duration: DateTime.UtcNow - _batch.StartTime,
                RequestedBy: _batch.RequestedBy,
                TotalMemories: _batch.TotalMemories,
                CurrentPage: _batch.Page
            ));
            return;
        }
        
        // Initialize graph schema first if needed
        if (msg.InitializeSchema)
        {
            await _graphSyncService.InitializeGraphSchemaAsync();
        }
        
        // Get total count of memories
        var (_, totalCount) = await _storage.GetMemoriesPaginated(1, 1);
        
        _logger.Info("Starting graph synchronization for {0} memories, page size {1}, requested by {2}", 
            totalCount, msg.PageSize, msg.RequestedBy);
        
        _batch = new BatchState
        {
            RequestedBy = msg.RequestedBy,
            Page = 1,
            PageSize = msg.PageSize,
            StartTime = DateTime.UtcNow,
            TotalMemories = totalCount,
            FullSync = msg.FullSync
        };
        
        await ProcessNextPage();
    }

    private async Task ProcessNextPage()
    {
        if (_batch == null) return;
        
        var (memories, _) = await _storage.GetMemoriesPaginated(_batch.Page, _batch.PageSize);
        
        if (memories.Count == 0)
        {
            PublishBatchCompleted();
            return;
        }

        _logger.Info("Processing {0} memories at page {1}/{2}", 
            memories.Count, _batch.Page, (_batch.TotalMemories + _batch.PageSize - 1) / _batch.PageSize);
        
        _batch.Outstanding = memories.Count;
        
        foreach (var memory in memories)
        {
            Self.Tell(new ProcessMemoryForGraph(
                memory.Id,
                memory,
                _batch.FullSync
            ));
        }
    }

    private async Task HandleProcessMemory(ProcessMemoryForGraph msg)
    {
        if (_batch == null) return;
        
        try
        {
            // Sync memory to graph
            await _graphSyncService.CreateOrUpdateGraphNodeAsync(msg.Memory);
            
            // Create LLM-suggested relationships if enabled
            if (msg.CreateRelationships)
            {
                var suggestions = await _graphSyncService.SuggestRelationshipsAsync(msg.MemoryId);
                var createdCount = 0;
                
                foreach (var suggestion in suggestions.Where(s => s.Weight >= 0.7))
                {
                    var success = await _graphSyncService.CreateGraphRelationshipAsync(
                        suggestion.FromId,
                        suggestion.ToId,
                        suggestion.Type,
                        new Dictionary<string, object> 
                        { 
                            ["weight"] = suggestion.Weight, 
                            ["llm_suggested"] = true 
                        });
                    
                    if (success) createdCount++;
                }
                
                _batch.RelationshipsCreated += createdCount;
            }
            
            _batch.Success++;
            _logger.Debug("Successfully synced memory {0} to graph", msg.MemoryId);
        }
        catch (Exception ex)
        {
            _batch.Failure++;
            _batch.FailedIds.Add(msg.MemoryId);
            _logger.Error(ex, "Failed to sync memory {0} to graph", msg.MemoryId);
        }
        finally
        {
            _batch.Outstanding--;
            
            if (_batch.Outstanding == 0)
            {
                // Page completed, move to next page
                _batch.Page++;
                await ProcessNextPage();
            }
        }
    }

    private void PublishBatchCompleted()
    {
        if (_batch == null) return;
        
        var duration = DateTime.UtcNow - _batch.StartTime;
        
        _logger.Info("Graph sync batch completed: {0} successful, {1} failed, {2} relationships created, duration: {3}",
            _batch.Success, _batch.Failure, _batch.RelationshipsCreated, duration);
        
        Context.System.EventStream.Publish(new GraphSyncCompleted
        {
            TotalProcessed = _batch.Success + _batch.Failure,
            TotalSuccessful = _batch.Success,
            TotalFailed = _batch.Failure,
            RelationshipsCreated = _batch.RelationshipsCreated,
            Duration = duration,
            RequestedBy = _batch.RequestedBy
        });
    }

    private GraphSyncStatus HandleGetStatus()
    {
        if (_batch == null)
        {
            return new GraphSyncStatus(
                IsRunning: false,
                Status: Status.Idle.ToString(),
                TotalProcessed: 0,
                TotalSuccessful: 0,
                TotalFailed: 0,
                Outstanding: 0,
                RelationshipsCreated: 0,
                FailedMemoryIds: new List<Guid>(),
                StartTime: DateTime.UtcNow,
                Duration: TimeSpan.Zero,
                RequestedBy: string.Empty,
                TotalMemories: 0,
                CurrentPage: 0
            );
        }

        return new GraphSyncStatus(
            IsRunning: _batch.CurrentStatus == Status.Running,
            Status: _batch.CurrentStatus.ToString(),
            TotalProcessed: _batch.Success + _batch.Failure,
            TotalSuccessful: _batch.Success,
            TotalFailed: _batch.Failure,
            Outstanding: _batch.Outstanding,
            RelationshipsCreated: _batch.RelationshipsCreated,
            FailedMemoryIds: _batch.FailedIds.ToList(),
            StartTime: _batch.StartTime,
            Duration: _batch.CurrentStatus == Status.Running ? DateTime.UtcNow - _batch.StartTime : null,
            RequestedBy: _batch.RequestedBy,
            TotalMemories: _batch.TotalMemories,
            CurrentPage: _batch.Page
        );
    }

    public static Props Props(
        IStorage storage,
        IGraphSyncService graphSyncService,
        IGraphRepository graphRepository)
    {
        return Akka.Actor.Props.Create(() => 
            new GraphSyncActor(storage, graphSyncService, graphRepository));
    }
}

// Messages
public record SyncAllMemoriesToGraph(
    int PageSize,
    string RequestedBy,
    bool FullSync = false,
    bool InitializeSchema = false
);

public record ProcessMemoryForGraph(
    Guid MemoryId,
    Memory Memory,
    bool CreateRelationships = true
);

public record GetGraphSyncStatus;

public record InitializeGraphSchema;

public record GraphSchemaInitialized
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public record GraphSyncStatus(
    bool IsRunning,
    string Status,
    int TotalProcessed,
    int TotalSuccessful,
    int TotalFailed,
    int Outstanding,
    int RelationshipsCreated,
    List<Guid> FailedMemoryIds,
    DateTime StartTime,
    TimeSpan? Duration,
    string RequestedBy,
    int TotalMemories,
    int CurrentPage
);

public record GraphSyncCompleted
{
    public int TotalProcessed { get; set; }
    public int TotalSuccessful { get; set; }
    public int TotalFailed { get; set; }
    public int RelationshipsCreated { get; set; }
    public TimeSpan Duration { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

// Actor key for dependency injection
public class GraphSyncActorKey { }