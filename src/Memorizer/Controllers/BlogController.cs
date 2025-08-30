using Memorizer.Models;
using Memorizer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Memorizer.Controllers;

[Route("ui/blog")]
public class BlogController : Controller
{
    private readonly IStorage _storage;
    private readonly ILogger<BlogController> _logger;

    public BlogController(IStorage storage, ILogger<BlogController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// Blog memories page - accessible without authentication at /ui/blog
    /// </summary>
    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// API endpoint for paginated blog memories with filtering
    /// </summary>
    [HttpGet]
    [Route("api/memories")]
    public async Task<ActionResult<BlogMemoryListResponse>> GetBlogMemories(
        int page = 1, 
        int pageSize = 10,
        string? searchQuery = null,
        string? types = null,
        string? tags = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            // Parse filter arrays from comma-separated strings
            var typeFilters = string.IsNullOrEmpty(types) 
                ? null 
                : types.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            var tagFilters = string.IsNullOrEmpty(tags) 
                ? null 
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries);

            // Get filtered and paginated memories using optimized database queries
            var (memories, totalCount) = await _storage.GetBlogMemoriesPaginated(
                page, 
                pageSize, 
                searchQuery, 
                typeFilters, 
                tagFilters);

            // Get all distinct types and tags with counts using optimized queries
            var allTypes = await _storage.GetDistinctMemoryTypes();
            var typeCounts = await _storage.GetTypeCountsForBlog(searchQuery, tagFilters);
            var tagCounts = await _storage.GetTagCountsForBlog(searchQuery, typeFilters, 20);
            var allTags = tagCounts.Keys.OrderBy(t => t).ToList();

            return Ok(new BlogMemoryListResponse
            {
                Memories = memories,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                AllTypes = allTypes,
                AllTags = allTags,
                TypeCounts = typeCounts,
                TagCounts = tagCounts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog memories");
            return StatusCode(500, new { error = "Failed to load memories" });
        }
    }

    /// <summary>
    /// Get a specific memory by ID for popup display
    /// </summary>
    [HttpGet]
    [Route("api/memory/{id}")]
    public async Task<ActionResult<Memory>> GetMemory(Guid id)
    {
        var memory = await _storage.Get(id);
        if (memory == null)
        {
            return NotFound();
        }
        return Ok(memory);
    }

    // Note: These methods have been replaced with optimized database queries in IStorage
    // The filtering, searching, and counting is now done at the database level for better performance
}

public class BlogMemoryListResponse : MemoryListResponse
{
    public List<string> AllTypes { get; set; } = new();
    public List<string> AllTags { get; set; } = new();
    public Dictionary<string, int> TypeCounts { get; set; } = new();
    public Dictionary<string, int> TagCounts { get; set; } = new();
}