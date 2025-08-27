using Akka.Actor;
using Akka.Hosting;
using Memorizer.Actors;
using Memorizer.Services;
using Microsoft.AspNetCore.Mvc;

namespace Memorizer.Controllers;

[Route("ui/tools/graph-sync")]
public class GraphSyncController : Controller
{
    private readonly IActorRef? _graphSyncActor;
    private readonly IGraphSyncService _graphSyncService;
    private readonly IStorage _storage;
    private readonly ILogger<GraphSyncController> _logger;

    public GraphSyncController(
        IServiceProvider serviceProvider,
        IGraphSyncService graphSyncService,
        IStorage storage,
        ILogger<GraphSyncController> logger)
    {
        _logger = logger;
        _graphSyncService = graphSyncService;
        _storage = storage;
        
        // Try to get the actor, but don't fail if it's not available
        try
        {
            var actorRefProvider = serviceProvider.GetService<IRequiredActor<GraphSyncActorKey>>();
            _graphSyncActor = actorRefProvider?.ActorRef;
            if (_graphSyncActor == null)
            {
                _logger.LogWarning("GraphSyncActor not available - using direct service calls");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get GraphSyncActor - using direct service calls");
        }
    }

    /// <summary>
    /// Display the graph sync tool page
    /// </summary>
    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        return View("~/Views/Tools/GraphSync.cshtml");
    }

    /// <summary>
    /// Start full graph synchronization from PostgreSQL to Neo4j
    /// </summary>
    [HttpPost]
    [Route("start")]
    public async Task<IActionResult> StartGraphSync(
        int pageSize = 100, 
        bool fullSync = true, 
        bool initializeSchema = false)
    {
        try
        {
            _logger.LogInformation("Starting graph synchronization with page size {PageSize}, full sync: {FullSync}, initialize schema: {InitSchema}", 
                pageSize, fullSync, initializeSchema);

            // If actor is available, use it
            if (_graphSyncActor != null)
            {
                try
                {
                    // Check if a batch is already running
                    var status = await _graphSyncActor.Ask<GraphSyncStatus>(
                        new GetGraphSyncStatus(), 
                        TimeSpan.FromSeconds(5));
                    
                    if (status.IsRunning)
                    {
                        return Json(new {
                            success = false,
                            message = "A graph synchronization job is already in progress.",
                            status = status.Status,
                            totalProcessed = status.TotalProcessed
                        });
                    }

                    var syncMessage = new SyncAllMemoriesToGraph(
                        PageSize: pageSize,
                        RequestedBy: User.Identity?.Name ?? "Anonymous",
                        FullSync: fullSync,
                        InitializeSchema: initializeSchema
                    );

                    _graphSyncActor.Tell(syncMessage);

                    return Json(new { 
                        success = true, 
                        message = $"Graph synchronization started with page size {pageSize}." 
                    });
                }
                catch (Exception actorEx)
                {
                    _logger.LogWarning(actorEx, "Actor call failed, falling back to direct service call");
                }
            }

            // Fallback to direct service call
            _logger.LogInformation("Using direct service call for graph synchronization");

            // Initialize schema if requested
            if (initializeSchema)
            {
                await _graphSyncService.InitializeGraphSchemaAsync();
            }

            // Run synchronization in background task
            _ = Task.Run(async () =>
            {
                try
                {
                    var syncedCount = await _graphSyncService.SyncMemoriesToGraphAsync(fullSync);
                    _logger.LogInformation("Graph synchronization completed. Synced {Count} memories", syncedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during graph synchronization");
                }
            });

            return Json(new { 
                success = true, 
                message = $"Graph synchronization started with page size {pageSize}. Check logs for progress." 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting graph synchronization: {Error}", ex.Message);
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Initialize Neo4j graph schema
    /// </summary>
    [HttpPost]
    [Route("initialize-schema")]
    public async Task<IActionResult> InitializeSchema()
    {
        try
        {
            _logger.LogInformation("Initializing Neo4j graph schema");

            // If actor is available, use it
            if (_graphSyncActor != null)
            {
                try
                {
                    var result = await _graphSyncActor.Ask<GraphSchemaInitialized>(
                        new InitializeGraphSchema(),
                        TimeSpan.FromSeconds(30));

                    return Json(new { 
                        success = result.Success, 
                        message = result.Message 
                    });
                }
                catch (Exception actorEx)
                {
                    _logger.LogWarning(actorEx, "Actor call failed, falling back to direct service call");
                }
            }

            // Fallback to direct service call
            var success = await _graphSyncService.InitializeGraphSchemaAsync();
            
            return Json(new { 
                success = success, 
                message = success ? "Graph schema initialized successfully" : "Failed to initialize graph schema"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing graph schema: {Error}", ex.Message);
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get the status of graph synchronization operations
    /// </summary>
    [HttpGet]
    [Route("status")]
    public async Task<IActionResult> GetGraphSyncStatus()
    {
        try
        {
            // If actor is available, use it
            if (_graphSyncActor != null)
            {
                try
                {
                    var status = await _graphSyncActor.Ask<GraphSyncStatus>(
                        new GetGraphSyncStatus(), 
                        TimeSpan.FromSeconds(5));
                    
                    var progressPercentage = status.TotalMemories > 0 
                        ? (status.TotalProcessed * 100.0 / status.TotalMemories) 
                        : 0;

                    return Json(new {
                        success = true,
                        status = status.Status,
                        isRunning = status.IsRunning,
                        outstanding = status.Outstanding,
                        totalProcessed = status.TotalProcessed,
                        totalSuccessful = status.TotalSuccessful,
                        totalFailed = status.TotalFailed,
                        relationshipsCreated = status.RelationshipsCreated,
                        requestedBy = status.RequestedBy,
                        startTime = status.StartTime,
                        duration = status.Duration?.TotalSeconds,
                        failedMemoryIds = status.FailedMemoryIds,
                        totalMemories = status.TotalMemories,
                        currentPage = status.CurrentPage,
                        progressPercentage = progressPercentage
                    });
                }
                catch (Exception actorEx)
                {
                    _logger.LogDebug(actorEx, "Actor status check failed");
                }
            }

            // Fallback to basic status
            return Json(new {
                success = true,
                status = "Unknown",
                isRunning = false,
                outstanding = 0,
                totalProcessed = 0,
                totalSuccessful = 0,
                totalFailed = 0,
                relationshipsCreated = 0,
                requestedBy = "",
                startTime = DateTime.UtcNow,
                duration = 0,
                failedMemoryIds = new List<Guid>(),
                totalMemories = 0,
                currentPage = 0,
                progressPercentage = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting graph sync status: {Error}", ex.Message);
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    /// <summary>
    /// API endpoint to start graph synchronization (for programmatic access)
    /// </summary>
    [HttpPost]
    [Route("/api/graph/sync")]
    public async Task<IActionResult> ApiStartGraphSync(
        [FromBody] GraphSyncRequest request)
    {
        try
        {
            _logger.LogInformation("Starting graph synchronization via API with full sync: {FullSync}, initialize schema: {InitSchema}", 
                request.FullSync ?? true, request.InitializeSchema ?? false);

            // If actor is available, try to use it
            if (_graphSyncActor != null)
            {
                try
                {
                    // Check if a batch is already running
                    var status = await _graphSyncActor.Ask<GraphSyncStatus>(
                        new GetGraphSyncStatus(), 
                        TimeSpan.FromSeconds(5));
                    
                    if (status.IsRunning)
                    {
                        return StatusCode(409, new {
                            error = "Conflict",
                            message = "A graph synchronization job is already in progress",
                            currentStatus = status
                        });
                    }

                    var syncMessage = new SyncAllMemoriesToGraph(
                        PageSize: request.PageSize ?? 100,
                        RequestedBy: User.Identity?.Name ?? "API",
                        FullSync: request.FullSync ?? true,
                        InitializeSchema: request.InitializeSchema ?? false
                    );

                    _graphSyncActor.Tell(syncMessage);

                    return Accepted(new { 
                        message = "Graph synchronization started",
                        pageSize = request.PageSize ?? 100,
                        fullSync = request.FullSync ?? true,
                        initializeSchema = request.InitializeSchema ?? false
                    });
                }
                catch (Exception actorEx)
                {
                    _logger.LogWarning(actorEx, "Actor call failed, falling back to direct service call");
                }
            }

            // Fallback to direct service call
            _logger.LogInformation("Using direct service call for graph synchronization");

            // Initialize schema if requested
            if (request.InitializeSchema ?? false)
            {
                await _graphSyncService.InitializeGraphSchemaAsync();
            }

            // Run synchronization in background task
            _ = Task.Run(async () =>
            {
                try
                {
                    var syncedCount = await _graphSyncService.SyncMemoriesToGraphAsync(request.FullSync ?? true);
                    _logger.LogInformation("Graph synchronization completed. Synced {Count} memories", syncedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during graph synchronization");
                }
            });

            return Accepted(new { 
                message = "Graph synchronization started (direct service)",
                pageSize = request.PageSize ?? 100,
                fullSync = request.FullSync ?? true,
                initializeSchema = request.InitializeSchema ?? false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting graph synchronization via API: {Error}", ex.Message);
            return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
        }
    }
}

public class GraphSyncRequest
{
    public int? PageSize { get; set; }
    public bool? FullSync { get; set; }
    public bool? InitializeSchema { get; set; }
}