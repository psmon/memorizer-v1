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

            // Get filtered and paginated memories
            var (memories, totalCount) = await GetFilteredMemories(
                page, 
                pageSize, 
                searchQuery, 
                typeFilters, 
                tagFilters);

            // Get all distinct types and tags for filter buttons
            var allTypes = await _storage.GetDistinctMemoryTypes();
            var allTags = await GetAllDistinctTags();

            // Count memories for each type and tag
            var typeCounts = new Dictionary<string, int>();
            var tagCounts = new Dictionary<string, int>();

            foreach (var type in allTypes)
            {
                var (_, count) = await GetFilteredMemories(
                    1, 1, searchQuery, new[] { type }, tagFilters);
                typeCounts[type] = count;
            }

            foreach (var tag in allTags)
            {
                var (_, count) = await GetFilteredMemories(
                    1, 1, searchQuery, typeFilters, new[] { tag });
                tagCounts[tag] = count;
            }

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

    private async Task<(List<Memory> Memories, int TotalCount)> GetFilteredMemories(
        int page, 
        int pageSize, 
        string? searchQuery, 
        string[]? typeFilters, 
        string[]? tagFilters)
    {
        // Get all memories first (in production, this should be optimized with proper SQL filtering)
        var (allMemories, _) = await _storage.GetMemoriesPaginated(1, 10000);

        // Apply filters
        var filtered = allMemories.AsEnumerable();

        // Apply search query filter (title and content)
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var query = searchQuery.ToLower();
            filtered = filtered.Where(m => 
                (m.Title != null && m.Title.ToLower().Contains(query)) ||
                (m.Text != null && m.Text.ToLower().Contains(query)));
        }

        // Apply type filter
        if (typeFilters != null && typeFilters.Length > 0)
        {
            filtered = filtered.Where(m => typeFilters.Contains(m.Type));
        }

        // Apply tag filter
        if (tagFilters != null && tagFilters.Length > 0)
        {
            filtered = filtered.Where(m => 
                m.Tags != null && m.Tags.Any(t => tagFilters.Contains(t)));
        }

        // Order by creation date (newest first)
        filtered = filtered.OrderByDescending(m => m.CreatedAt);

        // Get total count before pagination
        var totalCount = filtered.Count();

        // Apply pagination
        var pagedMemories = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedMemories, totalCount);
    }

    private async Task<List<string>> GetAllDistinctTags()
    {
        // Get all memories to extract distinct tags
        var (allMemories, _) = await _storage.GetMemoriesPaginated(1, 10000);
        
        var allTags = allMemories
            .Where(m => m.Tags != null)
            .SelectMany(m => m.Tags!)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        return allTags;
    }
}

public class BlogMemoryListResponse : MemoryListResponse
{
    public List<string> AllTypes { get; set; } = new();
    public List<string> AllTags { get; set; } = new();
    public Dictionary<string, int> TypeCounts { get; set; } = new();
    public Dictionary<string, int> TagCounts { get; set; } = new();
}