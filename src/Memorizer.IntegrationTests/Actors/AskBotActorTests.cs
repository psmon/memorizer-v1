using Akka.Actor;
using Akka.TestKit.Xunit2;
using Memorizer.Actors;
using Memorizer.Controllers;
using Memorizer.Models;
using Memorizer.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Memorizer.IntegrationTests.Actors;

public class AskBotActorTests : TestKit
{
    private readonly Mock<ILlmService> _mockLlmService;
    private readonly Mock<ILogger<AskBotActorTests>> _mockLogger;

    public AskBotActorTests()
    {
        _mockLlmService = new Mock<ILlmService>();
        _mockLogger = new Mock<ILogger<AskBotActorTests>>();
    }

    [Fact]
    public async Task StreamingChatBotActor_Should_Forward_Reasoning_Steps_To_SSE_Bridge()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var receivedUpdates = new List<StreamingUpdate>();

        // Create SSE bridge that collects updates
        Func<StreamingUpdate, Task> handler = update =>
        {
            receivedUpdates.Add(update);
            return Task.CompletedTask;
        };
        var sseBridgeProps = Props.Create(() => new TestSSEBridgeActor(handler));
        var sseBridge = Sys.ActorOf(sseBridgeProps);

        // Create mock search and decision actors
        var searchMemoryActor = CreateTestProbe();
        var decisionActor = CreateTestProbe();

        // Setup LLM service
        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("This is a test response from the LLM.");

        // Create StreamingChatBotActor
        var chatBotProps = StreamingChatBotActor.Props(
            sessionId,
            searchMemoryActor.Ref,
            decisionActor.Ref,
            _mockLlmService.Object,
            sseBridge);
        var chatBot = Sys.ActorOf(chatBotProps);

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Test question",
            UserId = "test-user"
        };

        chatBot.Tell(request, TestActor);

        // Simulate search response with no results (triggers general response)
        searchMemoryActor.ExpectMsg<SearchMemoryRequest>();
        searchMemoryActor.Reply(new SearchMemoryResponse
        {
            SessionId = sessionId,
            OriginalQuery = "Test question",
            Memories = new List<Memory>(),
            SearchPerformed = true,
            RetryAttempts = 0
        });

        // Assert
        var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(5));
        Assert.Equal(sessionId, response.SessionId);
        Assert.Equal(ResponseType.General, response.Type);
        Assert.Contains("test response from the LLM", response.Message);

        // Verify reasoning steps were forwarded
        await Task.Delay(100); // Allow time for async processing
        Assert.True(receivedUpdates.Count > 0, "Should have received streaming updates");
        Assert.Contains(receivedUpdates, u => u.UpdateType == StreamUpdateType.Reasoning);
    }

    [Fact]
    public void ChatBotActor_Should_Handle_Memory_Based_Response()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var searchMemoryActor = CreateTestProbe();
        var decisionActor = CreateTestProbe();

        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Response based on the provided memories about Docker.");

        var chatBotProps = ChatBotActor.Props(
            sessionId,
            searchMemoryActor.Ref,
            decisionActor.Ref,
            _mockLlmService.Object);
        var chatBot = Sys.ActorOf(chatBotProps);

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Tell me about Docker",
            UserId = "test-user"
        };

        chatBot.Tell(request, TestActor);

        // Simulate search response with memories
        searchMemoryActor.ExpectMsg<SearchMemoryRequest>();
        var testMemories = new List<Memory>
        {
            new Memory
            {
                Id = Guid.NewGuid(),
                Title = "Docker Basics",
                Text = "Docker is a containerization platform",
                Type = "reference",
                Tags = new[] { "docker", "containers" }
            }
        };

        searchMemoryActor.Reply(new SearchMemoryResponse
        {
            SessionId = sessionId,
            OriginalQuery = "Tell me about Docker",
            Memories = testMemories,
            SearchPerformed = true,
            RetryAttempts = 0
        });

        // Simulate relevance evaluation
        decisionActor.ExpectMsg<EvaluateRelevanceRequest>();
        decisionActor.Reply(new EvaluateRelevanceResponse
        {
            SessionId = sessionId,
            HasRelevantMemories = true,
            RelevantMemories = testMemories,
            Reasoning = "Found relevant Docker documentation"
        });

        // Assert
        var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(5));
        Assert.Equal(sessionId, response.SessionId);
        Assert.Equal(ResponseType.MemoryBased, response.Type);
        Assert.Contains("Docker", response.Message);
        Assert.NotNull(response.ReferencedMemoryIds);
        Assert.Single(response.ReferencedMemoryIds);
    }

    [Fact]
    public void ChatBotActor_Should_Handle_Session_Timeout()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var searchMemoryActor = CreateTestProbe();
        var decisionActor = CreateTestProbe();

        var chatBotProps = ChatBotActor.Props(
            sessionId,
            searchMemoryActor.Ref,
            decisionActor.Ref,
            _mockLlmService.Object);
        var chatBot = Sys.ActorOf(chatBotProps);

        // Watch the actor
        Watch(chatBot);

        // Act - Send session timeout message
        chatBot.Tell(new SessionTimeout { SessionId = sessionId });

        // Assert - Actor should stop
        ExpectTerminated(chatBot, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ChatBotActor_Should_Handle_Error_Response()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var searchMemoryActor = CreateTestProbe();
        var decisionActor = CreateTestProbe();

        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM service error"));

        var chatBotProps = ChatBotActor.Props(
            sessionId,
            searchMemoryActor.Ref,
            decisionActor.Ref,
            _mockLlmService.Object);
        var chatBot = Sys.ActorOf(chatBotProps);

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Test question",
            UserId = "test-user"
        };

        chatBot.Tell(request, TestActor);

        // Simulate search response
        searchMemoryActor.ExpectMsg<SearchMemoryRequest>();
        searchMemoryActor.Reply(new SearchMemoryResponse
        {
            SessionId = sessionId,
            OriginalQuery = "Test question",
            Memories = new List<Memory>(),
            SearchPerformed = false,
            RetryAttempts = 0
        });

        // Assert
        var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(5));
        Assert.Equal(sessionId, response.SessionId);
        Assert.Equal(ResponseType.Error, response.Type);
        Assert.Contains("Failed to generate response", response.Message);
    }

    // Test SSE Bridge Actor for testing
    private class TestSSEBridgeActor : ReceiveActor
    {
        public TestSSEBridgeActor(Func<StreamingUpdate, Task> handler)
        {
            ReceiveAsync<StreamingUpdate>(async update => await handler(update));
        }
    }
}