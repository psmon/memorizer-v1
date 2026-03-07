using Memorizer.Models;
using Memorizer.Services;
using Microsoft.AspNetCore.Mvc;

namespace Memorizer.Controllers;

[Route("news/ai-tech-now")]
public class NewsController : Controller
{
    private readonly IStorage _storage;
    private readonly ILogger<NewsController> _logger;

    private static readonly Dictionary<string, string[]> CategoryKeywords = new()
    {
        ["all"] = Array.Empty<string>(),
        ["vibe"] = new[] { "vibe", "vibes", "바이브", "viving", "vibecoding" },
        ["claude-code"] = new[] { "claude code", "claude-code", "클로드", "claude", "anthropic" },
        ["openai"] = new[] { "openai", "gpt", "chatgpt", "o1", "o3", "o4" },
        ["architecture"] = new[] { "architecture", "아키텍처", "설계", "design pattern", "시스템 설계" },
        ["agentic"] = new[] { "agent", "agentic", "에이전트", "ai agent", "mcp" }
    };

    public NewsController(IStorage storage, ILogger<NewsController> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("article/{id}")]
    public IActionResult Article(Guid id)
    {
        ViewBag.ArticleId = id;
        return View();
    }

    [HttpGet("api/articles")]
    public async Task<ActionResult> GetArticles(
        string category = "all",
        int count = 20,
        int offset = 0,
        string? searchQuery = null)
    {
        try
        {
            var page = (offset / Math.Max(count, 1)) + 1;
            List<Memorizer.Models.Memory> memories;
            int totalCount;

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                // Keyword search overrides category/filter entirely
                (memories, totalCount) = await _storage.GetBlogMemoriesPaginated(
                    page: page,
                    pageSize: count,
                    searchQuery: searchQuery);
            }
            else
            {
                var keywords = GetCategoryKeywords(category);
                (memories, totalCount) = await _storage.GetNewsArticlesByKeywords(
                    keywords: keywords.Length > 0 ? keywords : null,
                    page: page,
                    pageSize: count);
            }

            var articles = memories.Select(m => new
            {
                id = m.Id,
                title = m.Title ?? "(Untitled)",
                text = m.Text,
                preview = GetPreview(m.Text, 5),
                source = m.Source,
                tags = m.Tags,
                type = m.Type,
                createdAt = m.CreatedAt,
                updatedAt = m.UpdatedAt
            }).ToList();

            return Ok(new { articles, totalCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting news articles");
            return StatusCode(500, new { error = "Failed to load articles" });
        }
    }

    [HttpGet("api/article/{id}")]
    public async Task<ActionResult> GetArticle(Guid id)
    {
        try
        {
            var memory = await _storage.Get(id);
            if (memory == null)
                return NotFound();

            return Ok(new
            {
                id = memory.Id,
                title = memory.Title ?? "(Untitled)",
                text = memory.Text,
                source = memory.Source,
                tags = memory.Tags,
                type = memory.Type,
                createdAt = memory.CreatedAt,
                updatedAt = memory.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting article {Id}", id);
            return StatusCode(500, new { error = "Failed to load article" });
        }
    }

    [HttpGet("api/article/{id}/related")]
    public async Task<ActionResult> GetRelatedArticles(Guid id)
    {
        try
        {
            var relationships = await _storage.GetRelationships(id);
            var relatedIds = relationships
                .Select(r => r.FromMemoryId == id ? r.ToMemoryId : r.FromMemoryId)
                .Distinct()
                .ToList();

            if (relatedIds.Count == 0)
                return Ok(new { articles = Array.Empty<object>() });

            var relatedMemories = await _storage.GetMany(relatedIds);
            var articles = relatedMemories.Select(m => new
            {
                id = m.Id,
                title = m.Title ?? "(Untitled)",
                preview = GetPreview(m.Text, 3),
                createdAt = m.CreatedAt,
                type = m.Type
            }).ToList();

            return Ok(new { articles });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related articles for {Id}", id);
            return StatusCode(500, new { error = "Failed to load related articles" });
        }
    }

    [HttpGet("api/categories")]
    public ActionResult GetCategories()
    {
        var categories = CategoryKeywords.Keys.Select(k => new { key = k, label = GetCategoryLabel(k) });
        return Ok(categories);
    }

    private static string[] GetCategoryKeywords(string category)
    {
        if (CategoryKeywords.TryGetValue(category.ToLowerInvariant(), out var keywords))
            return keywords;
        return Array.Empty<string>();
    }

    private static string GetPreview(string text, int maxLines)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var preview = string.Join("\n", lines.Take(maxLines));
        if (preview.Length > 500)
            preview = preview[..500] + "...";
        return preview;
    }

    private static string GetCategoryLabel(string key) => key switch
    {
        "all" => "All",
        "vibe" => "Vibe",
        "claude-code" => "Claude-code",
        "openai" => "OpenAI",
        "architecture" => "Architecture",
        "agentic" => "Agentic",
        _ => key
    };
}
