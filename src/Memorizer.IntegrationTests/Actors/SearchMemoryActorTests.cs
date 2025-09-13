using Akka.Actor;
using Akka.TestKit.Xunit2;
using Memorizer.Actors;
using Memorizer.Models;
using Memorizer.Services;
using Moq;
using Xunit;

namespace Memorizer.IntegrationTests.Actors;

public class SearchMemoryActorTests : TestKit
{
    private readonly Mock<IStorage> _mockStorage;
    private readonly Mock<ILlmService> _mockLlmService;

    public SearchMemoryActorTests()
    {
        _mockStorage = new Mock<IStorage>();
        _mockLlmService = new Mock<ILlmService>();
    }

    [Fact]
    public async Task SearchMemoryActor_Should_Return_Results_When_Search_Is_Needed()
    {
        // Arrange
        var testMemories = new List<Memory>
        {
            new Memory
            {
                Id = Guid.NewGuid(),
                Type = "reference",
                Text = "Docker is a containerization platform",
                Title = "Docker Guide",
                Tags = new[] { "docker", "containers" },
                Confidence = 0.95
            }
        };

        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string prompt, CancellationToken ct) =>
            {
                if (prompt.Contains("memory search is needed"))
                    return "YES";
                if (prompt.Contains("Transform"))
                    return "docker containerization platform";
                return "";
            });

        _mockStorage.Setup(x => x.Search(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), null, default))
            .ReturnsAsync(testMemories);

        var searchActor = Sys.ActorOf(SearchMemoryActor.Props(_mockStorage.Object, _mockLlmService.Object));

        // Act
        var request = new SearchMemoryRequest
        {
            Query = "How does Docker work?",
            SessionId = "test-session",
            MaxResults = 5,
            MinSimilarity = 0.3
        };

        var response = await searchActor.Ask<SearchMemoryResponse>(request, TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(response);
        Assert.Equal("test-session", response.SessionId);
        Assert.True(response.SearchPerformed);
        Assert.Single(response.Memories);
        Assert.Contains("Docker", response.Memories[0].Text);
        Assert.NotNull(response.TransformedQuery);
        Assert.Equal(0, response.RetryAttempts);
    }

    [Fact]
    public async Task SearchMemoryActor_Should_Skip_Search_When_Not_Needed()
    {
        // Arrange
        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync("NO");

        var searchActor = Sys.ActorOf(SearchMemoryActor.Props(_mockStorage.Object, _mockLlmService.Object));

        // Act
        var request = new SearchMemoryRequest
        {
            Query = "Hello there",
            SessionId = "test-session",
            MaxResults = 5,
            MinSimilarity = 0.3
        };

        var response = await searchActor.Ask<SearchMemoryResponse>(request, TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(response);
        Assert.Equal("test-session", response.SessionId);
        Assert.False(response.SearchPerformed);
        Assert.Empty(response.Memories);
        Assert.Equal(0, response.RetryAttempts);

        // Verify storage was never called
        _mockStorage.Verify(x => x.Search(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), null, default), Times.Never);
    }

    [Fact]
    public async Task SearchMemoryActor_Should_Retry_With_Keywords_When_No_Results()
    {
        // Arrange
        var searchCallCount = 0;
        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string prompt, CancellationToken ct) =>
            {
                if (prompt.Contains("memory search is needed"))
                    return "YES";
                if (prompt.Contains("Transform"))
                    return "kubernetes container orchestration";
                if (prompt.Contains("Extract"))
                    return "kubernetes, container, orchestration";
                return "";
            });

        _mockStorage.Setup(x => x.Search(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), null, default))
            .ReturnsAsync(() =>
            {
                searchCallCount++;
                // Return empty for first attempt, then return results
                if (searchCallCount == 1)
                    return new List<Memory>();

                return new List<Memory>
                {
                    new Memory
                    {
                        Id = Guid.NewGuid(),
                        Type = "reference",
                        Text = "Kubernetes orchestrates containers",
                        Title = "K8s Guide",
                        Tags = new[] { "kubernetes" },
                        Confidence = 0.8
                    }
                };
            });

        var searchActor = Sys.ActorOf(SearchMemoryActor.Props(_mockStorage.Object, _mockLlmService.Object));

        // Act
        var request = new SearchMemoryRequest
        {
            Query = "Tell me about Kubernetes",
            SessionId = "test-session",
            MaxResults = 5,
            MinSimilarity = 0.3
        };

        var response = await searchActor.Ask<SearchMemoryResponse>(request, TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(response);
        Assert.True(response.SearchPerformed);
        Assert.Single(response.Memories);
        Assert.True(response.RetryAttempts > 0);
        Assert.NotNull(response.ExtractedKeywords);
        Assert.NotEmpty(response.ExtractedKeywords);

        // Verify multiple search attempts were made
        _mockStorage.Verify(x => x.Search(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), null, default), Times.AtLeast(2));
    }

    [Fact]
    public async Task SearchMemoryActor_Should_Handle_LLM_Errors_Gracefully()
    {
        // Arrange
        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ThrowsAsync(new Exception("LLM service error"));

        _mockStorage.Setup(x => x.Search(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), null, default))
            .ReturnsAsync(new List<Memory>
            {
                new Memory
                {
                    Id = Guid.NewGuid(),
                    Type = "reference",
                    Text = "Test content",
                    Title = "Test",
                    Confidence = 0.7
                }
            });

        var searchActor = Sys.ActorOf(SearchMemoryActor.Props(_mockStorage.Object, _mockLlmService.Object));

        // Act
        var request = new SearchMemoryRequest
        {
            Query = "Test query",
            SessionId = "test-session",
            MaxResults = 5,
            MinSimilarity = 0.3
        };

        var response = await searchActor.Ask<SearchMemoryResponse>(request, TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(response);
        // Should default to performing search when LLM fails
        Assert.True(response.SearchPerformed);
        // Should use original query when transformation fails
        Assert.Single(response.Memories);
    }

    protected override void AfterAll()
    {
        Shutdown();
        base.AfterAll();
    }
}