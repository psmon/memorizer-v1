using Akka.Actor;
using Akka.TestKit.Xunit2;
using Memorizer.Actors;
using Memorizer.Models;
using Memorizer.Services;
using Moq;
using Xunit;

namespace Memorizer.IntegrationTests.Actors;

public class DecisionActorTests : TestKit
{
    private readonly Mock<ILlmService> _mockLlmService;

    public DecisionActorTests()
    {
        _mockLlmService = new Mock<ILlmService>();
    }

    [Fact]
    public async Task DecisionActor_Should_Identify_Relevant_Memories()
    {
        // Arrange
        var memoryId = Guid.NewGuid();
        var testMemories = new List<Memory>
        {
            new Memory
            {
                Id = memoryId,
                Type = "reference",
                Text = "Docker is a containerization platform that packages applications",
                Title = "Docker Overview",
                Tags = new[] { "docker", "containers" },
                Confidence = 0.9
            },
            new Memory
            {
                Id = Guid.NewGuid(),
                Type = "reference",
                Text = "Unrelated content about cooking",
                Title = "Cooking Guide",
                Tags = new[] { "cooking" },
                Confidence = 0.8
            }
        };

        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync($@"RELEVANT: YES
REASONING: The first memory about Docker directly answers the user's question about containerization
RELEVANT_IDS: {memoryId}");

        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));

        // Act
        var request = new EvaluateRelevanceRequest
        {
            SessionId = "test-session",
            Query = "What is Docker and how does it work?",
            Memories = testMemories
        };

        var response = await decisionActor.Ask<EvaluateRelevanceResponse>(request, TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(response);
        Assert.Equal("test-session", response.SessionId);
        Assert.True(response.HasRelevantMemories);
        Assert.NotNull(response.RelevantMemories);
        Assert.Single(response.RelevantMemories);
        Assert.Equal(memoryId, response.RelevantMemories[0].Id);
        Assert.Contains("Docker", response.Reasoning);
    }

    [Fact]
    public async Task DecisionActor_Should_Handle_No_Relevant_Memories()
    {
        // Arrange
        var testMemories = new List<Memory>
        {
            new Memory
            {
                Id = Guid.NewGuid(),
                Type = "reference",
                Text = "Content about cooking recipes",
                Title = "Cooking Guide",
                Tags = new[] { "cooking", "recipes" },
                Confidence = 0.8
            }
        };

        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync(@"RELEVANT: NO
REASONING: The available memories about cooking are not relevant to the user's technical question
RELEVANT_IDS: ");

        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));

        // Act
        var request = new EvaluateRelevanceRequest
        {
            SessionId = "test-session",
            Query = "How do I implement OAuth2?",
            Memories = testMemories
        };

        var response = await decisionActor.Ask<EvaluateRelevanceResponse>(request, TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(response);
        Assert.Equal("test-session", response.SessionId);
        Assert.False(response.HasRelevantMemories);
        Assert.True(response.RelevantMemories == null || response.RelevantMemories.Count == 0);
        Assert.Contains("not relevant", response.Reasoning?.ToLower() ?? "");
    }

    [Fact]
    public async Task DecisionActor_Should_Handle_Empty_Memory_List()
    {
        // Arrange
        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));

        // Act
        var request = new EvaluateRelevanceRequest
        {
            SessionId = "test-session",
            Query = "Any query",
            Memories = new List<Memory>()
        };

        var response = await decisionActor.Ask<EvaluateRelevanceResponse>(request, TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(response);
        Assert.Equal("test-session", response.SessionId);
        Assert.False(response.HasRelevantMemories);
        Assert.True(response.RelevantMemories == null || response.RelevantMemories.Count == 0);
        Assert.Equal("No memories found to evaluate", response.Reasoning);

        // Verify LLM was never called for empty list
        _mockLlmService.Verify(x => x.CompleteAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task DecisionActor_Should_Handle_Multiple_Relevant_Memories()
    {
        // Arrange
        var memoryId1 = Guid.NewGuid();
        var memoryId2 = Guid.NewGuid();
        var testMemories = new List<Memory>
        {
            new Memory
            {
                Id = memoryId1,
                Type = "reference",
                Text = "Kubernetes basics",
                Title = "K8s Introduction",
                Tags = new[] { "kubernetes" },
                Confidence = 0.9
            },
            new Memory
            {
                Id = memoryId2,
                Type = "how-to",
                Text = "Kubernetes deployment guide",
                Title = "K8s Deployment",
                Tags = new[] { "kubernetes", "deployment" },
                Confidence = 0.85
            },
            new Memory
            {
                Id = Guid.NewGuid(),
                Type = "reference",
                Text = "Unrelated content",
                Title = "Other",
                Confidence = 0.7
            }
        };

        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync($@"RELEVANT: YES
REASONING: Both Kubernetes memories are relevant to the user's query
RELEVANT_IDS: {memoryId1}, {memoryId2}");

        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));

        // Act
        var request = new EvaluateRelevanceRequest
        {
            SessionId = "test-session",
            Query = "How do I deploy applications with Kubernetes?",
            Memories = testMemories
        };

        var response = await decisionActor.Ask<EvaluateRelevanceResponse>(request, TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(response);
        Assert.True(response.HasRelevantMemories);
        Assert.NotNull(response.RelevantMemories);
        Assert.Equal(2, response.RelevantMemories.Count);
        Assert.Contains(response.RelevantMemories, m => m.Id == memoryId1);
        Assert.Contains(response.RelevantMemories, m => m.Id == memoryId2);
    }

    [Fact]
    public async Task DecisionActor_Should_Handle_LLM_Errors_Conservatively()
    {
        // Arrange
        var testMemories = new List<Memory>
        {
            new Memory
            {
                Id = Guid.NewGuid(),
                Type = "reference",
                Text = "Test content",
                Title = "Test",
                Confidence = 0.8
            }
        };

        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ThrowsAsync(new Exception("LLM service error"));

        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));

        // Act
        var request = new EvaluateRelevanceRequest
        {
            SessionId = "test-session",
            Query = "Test query",
            Memories = testMemories
        };

        var response = await decisionActor.Ask<EvaluateRelevanceResponse>(request, TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(response);
        // Should conservatively include all memories on error
        Assert.True(response.HasRelevantMemories);
        Assert.NotNull(response.RelevantMemories);
        Assert.Single(response.RelevantMemories);
        Assert.Contains("Error during evaluation", response.Reasoning);
    }

    protected override void AfterAll()
    {
        Shutdown();
        base.AfterAll();
    }
}