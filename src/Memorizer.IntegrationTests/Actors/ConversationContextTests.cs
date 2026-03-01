using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Memorizer.Actors;
using Memorizer.Models;
using Memorizer.Services;
using Moq;
using Xunit;

namespace Memorizer.IntegrationTests.Actors;

/// <summary>
/// Deterministic actor tests for session context behavior.
/// External services are mocked to avoid flakiness.
/// </summary>
public class ConversationContextTests : TestKit
{
    private readonly Mock<ILlmService> _mockLlmService = new();
    private readonly List<string> _capturedPrompts = new();

    public ConversationContextTests()
    {
        _mockLlmService
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string prompt, CancellationToken _) =>
            {
                _capturedPrompts.Add(prompt);
                return ResolveLlmResponse(prompt);
            });
    }

    [Fact]
    public void ChatBotActor_Should_Maintain_Conversation_Context()
    {
        var sessionId = Guid.NewGuid().ToString();
        var search = CreateTestProbe();
        var decision = CreateTestProbe();
        var supervisor = CreateSupervisor(sessionId, search.Ref, decision.Ref);

        SendGeneral(supervisor, search, sessionId, "Who are you?");
        SendGeneral(supervisor, search, sessionId, "What can you do?");
        var response3 = SendGeneral(supervisor, search, sessionId, "Thanks, that helps.");

        Assert.NotNull(response3);
        var thirdPrompt = _capturedPrompts.Last(p => p.Contains("Current User Query: Thanks, that helps."));
        Assert.Contains("Recent Conversation:", thirdPrompt);
        Assert.Contains("Who are you?", thirdPrompt);
        Assert.Contains("What can you do?", thirdPrompt);
    }

    [Fact]
    public void ChatBotActor_Should_Manage_Conversation_History_Limit()
    {
        var sessionId = Guid.NewGuid().ToString();
        var search = CreateTestProbe();
        var decision = CreateTestProbe();
        var supervisor = CreateSupervisor(sessionId, search.Ref, decision.Ref);

        for (int i = 1; i <= 12; i++)
        {
            var response = SendGeneral(supervisor, search, sessionId, $"This is message number {i}");
            Assert.NotNull(response);
        }

        var final = SendGeneral(supervisor, search, sessionId, "Do you remember what we talked about in the beginning?");
        Assert.NotNull(final);

        var finalPrompt = _capturedPrompts.Last(p => p.Contains("Current User Query: Do you remember what we talked about in the beginning?"));
        Assert.Contains("Session Context (Important Information):", finalPrompt);
        Assert.DoesNotContain("User: This is message number 1\n", finalPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Different_Sessions_Should_Have_Isolated_Contexts()
    {
        var sessionId1 = Guid.NewGuid().ToString();
        var sessionId2 = Guid.NewGuid().ToString();

        var search = CreateTestProbe();
        var decision = CreateTestProbe();

        var supervisor1 = CreateSupervisor(sessionId1, search.Ref, decision.Ref);
        var supervisor2 = CreateSupervisor(sessionId2, search.Ref, decision.Ref);

        var reactiveMemory = CreateMemory(
            "Reactive Streams",
            "Reactive Streams supports asynchronous stream processing with non-blocking back pressure.");
        var aiMemory = CreateMemory(
            "AI Development",
            "AI development includes problem definition, data preparation, training, validation, and monitoring.");

        SendMemoryBased(supervisor1, search, decision, sessionId1, "Tell me about Reactive Streams", reactiveMemory);
        SendMemoryBased(supervisor2, search, decision, sessionId2, "Tell me about AI development", aiMemory);

        var response1 = SendGeneral(supervisor1, search, sessionId1, "What are the main benefits of it?");
        var response2 = SendGeneral(supervisor2, search, sessionId2, "What are the main phases of it?");

        Assert.Contains("stream", response1.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("phase", response2.Message, StringComparison.OrdinalIgnoreCase);

        var prompt1 = _capturedPrompts.Last(p => p.Contains("Current User Query: What are the main benefits of it?"));
        Assert.Contains("Reactive Streams", prompt1, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AI development", prompt1, StringComparison.OrdinalIgnoreCase);

        var prompt2 = _capturedPrompts.Last(p => p.Contains("Current User Query: What are the main phases of it?"));
        Assert.Contains("AI development", prompt2, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Reactive Streams", prompt2, StringComparison.OrdinalIgnoreCase);
    }

    private IActorRef CreateSupervisor(string sessionId, IActorRef searchMemoryActor, IActorRef decisionActor)
    {
        var supervisorProps = TestChatBotSupervisor.Props(
            sessionId,
            searchMemoryActor,
            decisionActor,
            _mockLlmService.Object,
            TestActor);
        return Sys.ActorOf(supervisorProps, $"supervisor-{sessionId}");
    }

    private ChatBotResponse SendGeneral(IActorRef supervisor, TestProbe searchProbe, string sessionId, string message)
    {
        supervisor.Tell(new UserChatRequest
        {
            SessionId = sessionId,
            Message = message,
            UserId = "test-user"
        }, TestActor);

        var analyze = searchProbe.ExpectMsg<AnalyzeQueryTypeRequest>(TimeSpan.FromSeconds(3));
        Assert.Equal(sessionId, analyze.SessionId);
        Assert.Equal(message, analyze.Query);

        searchProbe.Reply(new AnalyzeQueryTypeResponse
        {
            SessionId = sessionId,
            DocumentTypesNeeded = 1,
            Topics = new List<string> { "single-topic" },
            Reasoning = "single topic"
        });

        var searchRequest = searchProbe.ExpectMsg<SearchMemoryRequest>(TimeSpan.FromSeconds(3));
        Assert.Equal(sessionId, searchRequest.SessionId);
        Assert.Equal(message, searchRequest.Query);

        searchProbe.Reply(new SearchMemoryResponse
        {
            SessionId = sessionId,
            OriginalQuery = message,
            Memories = new List<Memory>(),
            SearchPerformed = false,
            RetryAttempts = 0
        });

        return ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(5));
    }

    private ChatBotResponse SendMemoryBased(
        IActorRef supervisor,
        TestProbe searchProbe,
        TestProbe decisionProbe,
        string sessionId,
        string message,
        Memory memory)
    {
        supervisor.Tell(new UserChatRequest
        {
            SessionId = sessionId,
            Message = message,
            UserId = "test-user"
        }, TestActor);

        var analyze = searchProbe.ExpectMsg<AnalyzeQueryTypeRequest>(TimeSpan.FromSeconds(3));
        Assert.Equal(sessionId, analyze.SessionId);
        Assert.Equal(message, analyze.Query);

        searchProbe.Reply(new AnalyzeQueryTypeResponse
        {
            SessionId = sessionId,
            DocumentTypesNeeded = 1,
            Topics = new List<string> { "single-topic" },
            Reasoning = "single topic"
        });

        var searchRequest = searchProbe.ExpectMsg<SearchMemoryRequest>(TimeSpan.FromSeconds(3));
        Assert.Equal(sessionId, searchRequest.SessionId);
        Assert.Equal(message, searchRequest.Query);

        var memories = new List<Memory> { memory };
        searchProbe.Reply(new SearchMemoryResponse
        {
            SessionId = sessionId,
            OriginalQuery = message,
            Memories = memories,
            SearchPerformed = true,
            RetryAttempts = 0
        });

        var eval = decisionProbe.ExpectMsg<EvaluateRelevanceRequest>(TimeSpan.FromSeconds(3));
        Assert.Equal(sessionId, eval.SessionId);
        Assert.Equal(message, eval.Query);

        decisionProbe.Reply(new EvaluateRelevanceResponse
        {
            SessionId = sessionId,
            HasRelevantMemories = true,
            RelevantMemories = memories,
            Reasoning = "relevant"
        });

        return ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(5));
    }

    private static Memory CreateMemory(string title, string text)
    {
        return new Memory
        {
            Id = Guid.NewGuid(),
            Title = title,
            Text = text,
            Type = "reference",
            Tags = new[] { "test" }
        };
    }

    private static string ResolveLlmResponse(string prompt)
    {
        if (prompt.Contains("Extract the key information from this assistant response", StringComparison.Ordinal))
        {
            if (prompt.Contains("Reactive Streams", StringComparison.OrdinalIgnoreCase))
            {
                return "Reactive Streams supports async stream processing with back pressure.";
            }

            if (prompt.Contains("AI development", StringComparison.OrdinalIgnoreCase))
            {
                return "AI development phases include problem definition, data prep, training, validation.";
            }

            return "Important response summary.";
        }

        if (prompt.Contains("Extract the most important information from the following conversation exchange", StringComparison.Ordinal))
        {
            return "Pruned conversation summary.";
        }

        if (prompt.Contains("Current User Query: Tell me about Reactive Streams", StringComparison.Ordinal))
        {
            return "Reactive Streams enables asynchronous processing and non-blocking back pressure for robust stream systems.";
        }

        if (prompt.Contains("Current User Query: Tell me about AI development", StringComparison.Ordinal))
        {
            return "AI development includes phases such as problem definition, data preparation, model training, and monitoring.";
        }

        if (prompt.Contains("Current User Query: What are the main benefits of it?", StringComparison.Ordinal))
        {
            return "Main benefits include asynchronous stream handling and back pressure control.";
        }

        if (prompt.Contains("Current User Query: What are the main phases of it?", StringComparison.Ordinal))
        {
            return "Main phases include problem definition, data preparation, training, validation, and monitoring.";
        }

        if (prompt.Contains("Current User Query: Who are you?", StringComparison.Ordinal))
        {
            return "I am ASKBot, a conversational AI assistant.";
        }

        if (prompt.Contains("Current User Query: What can you do?", StringComparison.Ordinal))
        {
            return "I can answer questions using memory context and provide concise technical guidance.";
        }

        if (prompt.Contains("Current User Query: Thanks, that helps.", StringComparison.Ordinal))
        {
            return "Glad it helped. I can continue with deeper details whenever you want.";
        }

        if (prompt.Contains("Current User Query: This is message number", StringComparison.Ordinal))
        {
            return "Acknowledged.";
        }

        if (prompt.Contains("Current User Query: Do you remember what we talked about in the beginning?", StringComparison.Ordinal))
        {
            return "I remember the key points from earlier through summarized session context.";
        }

        return "General assistant response.";
    }
}
