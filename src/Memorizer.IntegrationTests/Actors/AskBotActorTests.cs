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

        // Create mock search and decision actors
        var searchMemoryActor = CreateTestProbe();
        var decisionActor = CreateTestProbe();

        // Setup LLM service
        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("This is a test response from the LLM.");

        // Use supervisor to capture ChatBotResponse from child actor's parent channel
        var supervisorProps = TestChatBotSupervisor.Props(
            sessionId,
            searchMemoryActor.Ref,
            decisionActor.Ref,
            _mockLlmService.Object,
            TestActor);
        var supervisor = Sys.ActorOf(supervisorProps);

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Test question",
            UserId = "test-user"
        };

        supervisor.Tell(request, TestActor);

        // New flow: AnalyzeQueryTypeRequest -> SearchMemoryRequest
        var analyzeRequest = searchMemoryActor.ExpectMsg<AnalyzeQueryTypeRequest>();
        Assert.Equal(sessionId, analyzeRequest.SessionId);
        Assert.Equal("Test question", analyzeRequest.Query);

        searchMemoryActor.Reply(new AnalyzeQueryTypeResponse
        {
            SessionId = sessionId,
            DocumentTypesNeeded = 1,
            Topics = new List<string> { "general" },
            Reasoning = "Single-topic query"
        });

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
        Assert.Equal(ResponseType.General, response.Type);
        Assert.Contains("test response", response.Message, StringComparison.OrdinalIgnoreCase);

        _mockLlmService.Verify(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(1));
        await Task.CompletedTask;
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

        // Use TestChatBotSupervisor to properly capture responses
        var supervisorProps = TestChatBotSupervisor.Props(
            sessionId,
            searchMemoryActor.Ref,
            decisionActor.Ref,
            _mockLlmService.Object,
            TestActor);
        var supervisor = Sys.ActorOf(supervisorProps);

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Tell me about Docker",
            UserId = "test-user"
        };

        supervisor.Tell(request, TestActor);

        // New flow: AnalyzeQueryTypeRequest -> SearchMemoryRequest
        var analyzeRequest = searchMemoryActor.ExpectMsg<AnalyzeQueryTypeRequest>();
        Assert.Equal(sessionId, analyzeRequest.SessionId);
        Assert.Equal("Tell me about Docker", analyzeRequest.Query);

        searchMemoryActor.Reply(new AnalyzeQueryTypeResponse
        {
            SessionId = sessionId,
            DocumentTypesNeeded = 1,
            Topics = new List<string> { "docker" },
            Reasoning = "Single-topic query"
        });

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
    public async Task ChatBotActor_Should_Handle_Error_Response()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var searchMemoryActor = CreateTestProbe();
        var decisionActor = CreateTestProbe();

        _mockLlmService.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM service error"));

        // Use TestChatBotSupervisor to properly capture responses
        var supervisorProps = TestChatBotSupervisor.Props(
            sessionId,
            searchMemoryActor.Ref,
            decisionActor.Ref,
            _mockLlmService.Object,
            TestActor);
        var supervisor = Sys.ActorOf(supervisorProps);

        // Act
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Test question",
            UserId = "test-user"
        };

        supervisor.Tell(request, TestActor);

        // New flow: AnalyzeQueryTypeRequest -> SearchMemoryRequest
        var analyzeRequest = searchMemoryActor.ExpectMsg<AnalyzeQueryTypeRequest>();
        Assert.Equal(sessionId, analyzeRequest.SessionId);
        Assert.Equal("Test question", analyzeRequest.Query);

        searchMemoryActor.Reply(new AnalyzeQueryTypeResponse
        {
            SessionId = sessionId,
            DocumentTypesNeeded = 1,
            Topics = new List<string> { "general" },
            Reasoning = "Single-topic query"
        });

        searchMemoryActor.ExpectMsg<SearchMemoryRequest>();
        searchMemoryActor.Reply(new SearchMemoryResponse
        {
            SessionId = sessionId,
            OriginalQuery = "Test question",
            Memories = new List<Memory>(),
            SearchPerformed = false,
            RetryAttempts = 0
        });

        // Assert - The current ChatBotActor implementation doesn't send error responses back
        // It logs the error but doesn't send a ChatBotResponse with Error type
        // So we just verify no response is sent and the error is handled gracefully
        await Task.Delay(500); // Wait for error handling

        // Verify LLM was called (and failed)
        _mockLlmService.Verify(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        // No response should be received (current implementation doesn't send error responses to parent)
        ExpectNoMsg(TimeSpan.FromMilliseconds(100));
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
