using System.Text.Json;
using Memorizer.Controllers;
using Memorizer.Models;
using Memorizer.Services;
using Memorizer.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Pgvector;

namespace Memorizer.IntegrationTests.Controllers;

public sealed class NewsControllerTests
{
    private readonly Mock<IStorage> _storageMock;
    private readonly NewsController _controller;

    public NewsControllerTests()
    {
        _storageMock = new Mock<IStorage>();
        var loggerMock = new Mock<ILogger<NewsController>>();
        var serverSettings = new ServerSettings();
        _controller = new NewsController(_storageMock.Object, loggerMock.Object, serverSettings);
    }

    private static Memory CreateTestMemory(int index, string? title = null)
    {
        return new Memory
        {
            Id = Guid.NewGuid(),
            Type = "article",
            Content = JsonDocument.Parse("{}"),
            Text = $"Test article content line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6 for article {index}",
            Source = "test-source",
            Embedding = new Vector(new float[] { 0.1f, 0.2f }),
            Tags = new[] { "ai", "test" },
            Confidence = 0.9,
            CreatedAt = DateTime.UtcNow.AddHours(-index),
            UpdatedAt = DateTime.UtcNow.AddHours(-index),
            Title = title ?? $"Test Article {index}"
        };
    }

    [Fact]
    public void Index_ReturnsViewResult()
    {
        var result = _controller.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Article_ReturnsViewResult_WithArticleId()
    {
        var id = Guid.NewGuid();
        var result = _controller.Article(id);
        var viewResult = Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task GetArticles_AllCategory_ReturnsArticles()
    {
        var memories = Enumerable.Range(0, 5).Select(i => CreateTestMemory(i)).ToList();
        _storageMock.Setup(s => s.GetNewsArticlesByKeywords(null, 1, 20, default))
            .ReturnsAsync((memories, 5));

        var result = await _controller.GetArticles("all", 20, 0);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetArticles_VibeCategory_PassesKeywords()
    {
        var memories = new List<Memory> { CreateTestMemory(0, "Vibe Coding Article") };
        _storageMock.Setup(s => s.GetNewsArticlesByKeywords(
                It.Is<string[]>(k => k.Contains("vibe")),
                It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync((memories, 1));

        var result = await _controller.GetArticles("vibe", 20, 0);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetArticles_WithSearchQuery_OverridesCategory()
    {
        var memories = new List<Memory> { CreateTestMemory(0, "MCP Protocol Deep Dive") };
        _storageMock.Setup(s => s.GetBlogMemoriesPaginated(1, 20, "MCP", null, null, default))
            .ReturnsAsync((memories, 1));

        var result = await _controller.GetArticles("vibe", 20, 0, "MCP");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _storageMock.Verify(s => s.GetNewsArticlesByKeywords(
            It.IsAny<string[]?>(), It.IsAny<int>(), It.IsAny<int>(), default), Times.Never);
        _storageMock.Verify(s => s.GetBlogMemoriesPaginated(
            1, 20, "MCP", null, null, default), Times.Once);
    }

    [Fact]
    public async Task GetArticle_ExistingId_ReturnsArticle()
    {
        var memory = CreateTestMemory(0);
        _storageMock.Setup(s => s.Get(memory.Id, default)).ReturnsAsync(memory);

        var result = await _controller.GetArticle(memory.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetArticle_NonExistingId_Returns404()
    {
        var id = Guid.NewGuid();
        _storageMock.Setup(s => s.Get(id, default)).ReturnsAsync((Memory?)null);

        var result = await _controller.GetArticle(id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetRelatedArticles_WithRelationships_ReturnsRelated()
    {
        var mainId = Guid.NewGuid();
        var relatedMemory = CreateTestMemory(1, "Related Article");
        var relationships = new List<MemoryRelationship>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FromMemoryId = mainId,
                ToMemoryId = relatedMemory.Id,
                Type = "related"
            }
        };

        _storageMock.Setup(s => s.GetRelationships(mainId, null, default))
            .ReturnsAsync(relationships);
        _storageMock.Setup(s => s.GetMany(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(relatedMemory.Id)), default))
            .ReturnsAsync(new List<Memory> { relatedMemory });

        var result = await _controller.GetRelatedArticles(mainId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetRelatedArticles_NoRelationships_ReturnsEmpty()
    {
        var mainId = Guid.NewGuid();
        _storageMock.Setup(s => s.GetRelationships(mainId, null, default))
            .ReturnsAsync(new List<MemoryRelationship>());

        var result = await _controller.GetRelatedArticles(mainId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetCategories_ReturnsAllCategories()
    {
        var result = _controller.GetCategories();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetArticles_PreviewTruncation_Works()
    {
        var memory = new Memory
        {
            Id = Guid.NewGuid(),
            Type = "article",
            Content = JsonDocument.Parse("{}"),
            Text = "Line1\nLine2\nLine3\nLine4\nLine5\nLine6_should_be_trimmed\nLine7",
            Source = "test",
            Embedding = new Vector(new float[] { 0.1f }),
            Tags = new[] { "test" },
            Confidence = 0.9,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Title = "Truncation Test"
        };
        _storageMock.Setup(s => s.GetNewsArticlesByKeywords(null, 1, 20, default))
            .ReturnsAsync((new List<Memory> { memory }, 1));

        var result = await _controller.GetArticles("all", 20, 0);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var articles = doc.RootElement.GetProperty("articles");
        var preview = articles[0].GetProperty("preview").GetString();
        Assert.NotNull(preview);
        Assert.DoesNotContain("Line6_should_be_trimmed", preview);
        Assert.Contains("Line5", preview);
    }
}
