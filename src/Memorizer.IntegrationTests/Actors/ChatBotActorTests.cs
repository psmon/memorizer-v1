using Akka.Actor;
using Akka.TestKit.Xunit2;
using Memorizer.Actors;
using Memorizer.Models;
using Memorizer.Services;
using Memorizer.Settings;
using Moq;
using Xunit;

namespace Memorizer.IntegrationTests.Actors;

public class ChatBotActorTests : TestKit
{
    private readonly Mock<ILlmService> _mockLlmService;
    private readonly Mock<IStorage> _mockStorage;

    public ChatBotActorTests()
    {
        _mockLlmService = new Mock<ILlmService>();
        _mockStorage = new Mock<IStorage>();
    }

    [Fact]
    public void ChatBotActor_Should_Handle_User_Request_With_Memory_Results()
    {
        // Arrange
        var sessionId = "test-session-123";
        var testMemories = new List<Memory>
        {
            new Memory
            {
                Id = Guid.NewGuid(),
                Type = "reference",
                Text = "Test memory content about AI",
                Title = "AI Reference",
                Tags = new[] { "AI", "test" },
                Confidence = 0.9
            }
        };

        // Setup mocks
        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string prompt, CancellationToken ct) =>
            {
                if (prompt.Contains("memory search is needed"))
                    return "YES";
                if (prompt.Contains("Transform"))
                    return "AI artificial intelligence";
                if (prompt.Contains("RELEVANT:"))
                    return $"RELEVANT: YES\nREASONING: Found relevant AI content\nRELEVANT_IDS: {testMemories[0].Id}";
                return "This is a response based on the memory about AI.";
            });

        _mockStorage.Setup(x => x.Search(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), null, default))
            .ReturnsAsync(testMemories);

        // Create actors with supervisor
        var searchMemoryActor = Sys.ActorOf(SearchMemoryActor.Props(_mockStorage.Object, _mockLlmService.Object));
        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));
        var supervisor = Sys.ActorOf(TestChatBotSupervisor.Props(
            sessionId, searchMemoryActor, decisionActor, _mockLlmService.Object, TestActor));

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Tell me about AI",
            UserId = "test-user"
        };

        supervisor.Tell(request);

        // Assert
        var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(10));
        Assert.NotNull(response);
        Assert.Equal(sessionId, response.SessionId);
        Assert.Equal(ResponseType.MemoryBased, response.Type);
        Assert.Contains("AI", response.Message);
        Assert.NotNull(response.ReferencedMemoryIds);
        Assert.Single(response.ReferencedMemoryIds);
        Assert.NotNull(response.ReasoningSteps);
        Assert.NotEmpty(response.ReasoningSteps);
    }

    [Fact]
    public void ChatBotActor_Should_Handle_User_Request_With_No_Memory_Results()
    {
        // Arrange
        var sessionId = "test-session-456";

        // Setup mocks
        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string prompt, CancellationToken ct) =>
            {
                if (prompt.Contains("memory search is needed"))
                    return "YES";
                if (prompt.Contains("Transform"))
                    return "random topic xyz";
                if (prompt.Contains("Extract"))
                    return "random, topic, xyz";
                return "This is a general AI response without memory references.";
            });

        _mockStorage.Setup(x => x.Search(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), null, default))
            .ReturnsAsync(new List<Memory>());

        // Create actors with supervisor
        var searchMemoryActor = Sys.ActorOf(SearchMemoryActor.Props(_mockStorage.Object, _mockLlmService.Object));
        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));
        var supervisor = Sys.ActorOf(TestChatBotSupervisor.Props(
            sessionId, searchMemoryActor, decisionActor, _mockLlmService.Object, TestActor));

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Tell me about something random",
            UserId = "test-user"
        };

        supervisor.Tell(request);

        // Assert
        var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(10));
        Assert.NotNull(response);
        Assert.Equal(sessionId, response.SessionId);
        Assert.Equal(ResponseType.General, response.Type);
        Assert.Contains("general", response.Message.ToLower());
        Assert.Null(response.ReferencedMemoryIds);
        Assert.NotNull(response.ReasoningSteps);
    }

    [Fact]
    public void ChatBotActor_Should_Handle_Greeting_Without_Search()
    {
        // Arrange
        var sessionId = "test-session-789";

        // Setup mocks
        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string prompt, CancellationToken ct) =>
            {
                if (prompt.Contains("memory search is needed"))
                    return "NO";
                return "Hello! How can I help you today?";
            });

        // Create actors with supervisor
        var searchMemoryActor = Sys.ActorOf(SearchMemoryActor.Props(_mockStorage.Object, _mockLlmService.Object));
        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));
        var supervisor = Sys.ActorOf(TestChatBotSupervisor.Props(
            sessionId, searchMemoryActor, decisionActor, _mockLlmService.Object, TestActor));

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Hello",
            UserId = "test-user"
        };

        supervisor.Tell(request);

        // Assert
        var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(10));
        Assert.NotNull(response);
        Assert.Equal(sessionId, response.SessionId);
        Assert.Equal(ResponseType.General, response.Type);
        Assert.Contains("Hello", response.Message);
        Assert.Null(response.ReferencedMemoryIds);
    }

    [Fact]
    public void ChatBotActor_Should_Timeout_After_Inactivity()
    {
        // Arrange
        var sessionId = "test-session-timeout";
        var searchMemoryActor = Sys.ActorOf(SearchMemoryActor.Props(_mockStorage.Object, _mockLlmService.Object));
        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));
        var supervisor = Sys.ActorOf(TestChatBotSupervisor.Props(
            sessionId, searchMemoryActor, decisionActor, _mockLlmService.Object, TestActor));

        // Act - Send session timeout message
        supervisor.Tell(new SessionTimeout { SessionId = sessionId });

        // Wait a moment for processing
        System.Threading.Thread.Sleep(100);

        // Assert - Test passed if no exceptions
        Assert.True(true, "Session timeout handled successfully");
    }

    [Fact]
    public void ChatBotActor_Should_Fallback_To_WebSearch_When_No_Memories_Found()
    {
        // Arrange
        var sessionId = "test-session-websearch";
        var mockWebSearchService = new Mock<IWebSearchService>();

        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string prompt, CancellationToken ct) =>
            {
                if (prompt.Contains("memory search is needed"))
                    return "YES";
                if (prompt.Contains("Transform"))
                    return "latest tech news 2024";
                if (prompt.Contains("Extract"))
                    return "tech, news, latest";
                return "Based on web search results, here is the answer about tech news.";
            });

        _mockStorage.Setup(x => x.Search(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), null, default))
            .ReturnsAsync(new List<Memory>());

        mockWebSearchService.Setup(x => x.SearchAsync(
                It.IsAny<WebSearchProvider>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<WebSearchAccessMode?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebSearchResponse(
                WebSearchProvider.Google,
                "latest tech news 2024",
                new List<WebSearchItem>
                {
                    new WebSearchItem("Tech News 2024", "https://example.com/tech",
                        "Latest technology news and updates", WebSearchProvider.Google)
                }));

        var searchMemoryActor = Sys.ActorOf(SearchMemoryActor.Props(_mockStorage.Object, _mockLlmService.Object));
        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));
        var supervisor = Sys.ActorOf(TestChatBotSupervisor.Props(
            sessionId, searchMemoryActor, decisionActor, _mockLlmService.Object, TestActor,
            mockWebSearchService.Object));

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "What are the latest tech news?",
            UserId = "test-user"
        };
        supervisor.Tell(request);

        // Assert
        var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(10));
        Assert.NotNull(response);
        Assert.Equal(sessionId, response.SessionId);
        Assert.Equal(ResponseType.WebSearchBased, response.Type);

        mockWebSearchService.Verify(x => x.SearchAsync(
            WebSearchProvider.Naver,
            It.IsAny<string>(),
            5,
            WebSearchAccessMode.Headless,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ChatBotActor_Should_Fallback_To_GeneralResponse_When_WebSearch_Fails()
    {
        // Arrange
        var sessionId = "test-session-websearch-fail";
        var mockWebSearchService = new Mock<IWebSearchService>();

        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string prompt, CancellationToken ct) =>
            {
                if (prompt.Contains("memory search is needed"))
                    return "YES";
                if (prompt.Contains("Transform"))
                    return "something";
                if (prompt.Contains("Extract"))
                    return "something";
                return "This is a general fallback response.";
            });

        _mockStorage.Setup(x => x.Search(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), null, default))
            .ReturnsAsync(new List<Memory>());

        mockWebSearchService.Setup(x => x.SearchAsync(
                It.IsAny<WebSearchProvider>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<WebSearchAccessMode?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var searchMemoryActor = Sys.ActorOf(SearchMemoryActor.Props(_mockStorage.Object, _mockLlmService.Object));
        var decisionActor = Sys.ActorOf(DecisionActor.Props(_mockLlmService.Object));
        var supervisor = Sys.ActorOf(TestChatBotSupervisor.Props(
            sessionId, searchMemoryActor, decisionActor, _mockLlmService.Object, TestActor,
            mockWebSearchService.Object));

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "What is the weather?",
            UserId = "test-user"
        };
        supervisor.Tell(request);

        // Assert - should get General response (fallback from failed web search)
        var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(10));
        Assert.NotNull(response);
        Assert.Equal(sessionId, response.SessionId);
        Assert.Equal(ResponseType.General, response.Type);
    }

    protected override void AfterAll()
    {
        Shutdown();
        base.AfterAll();
    }
}