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
/// Deterministic actor tests for last-response context behavior.
/// External DB/LLM dependencies are intentionally removed.
/// </summary>
public class LastResponseTests : TestKit
{
    private readonly Mock<ILlmService> _mockLlmService = new();
    private readonly List<string> _capturedPrompts = new();

    public LastResponseTests()
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
    public void ChatBotActor_Should_Store_And_Reference_Last_Important_Response()
    {
        var sessionId = Guid.NewGuid().ToString();
        var searchMemoryActor = CreateTestProbe();
        var decisionActor = CreateTestProbe();
        var supervisor = CreateSupervisor(sessionId, searchMemoryActor.Ref, decisionActor.Ref);

        var solidMemory = CreateMemory(
            "SOLID Principles",
            "SOLID includes SRP, OCP, LSP, ISP, and DIP. Dependency Inversion Principle says high-level modules should depend on abstractions.");

        SendAndResolve(supervisor, searchMemoryActor, decisionActor, sessionId,
            "What are the SOLID principles in software engineering?",
            new List<Memory> { solidMemory },
            searchPerformed: true,
            hasRelevantMemories: true);

        SendAndResolve(supervisor, searchMemoryActor, decisionActor, sessionId,
            "Let's talk about something else. What time is it?",
            new List<Memory>(),
            searchPerformed: false,
            hasRelevantMemories: false);

        var response3 = SendAndResolve(supervisor, searchMemoryActor, decisionActor, sessionId,
            "Going back to the principles we discussed, which one deals with dependency?",
            new List<Memory>(),
            searchPerformed: false,
            hasRelevantMemories: false);

        Assert.Contains("Dependency Inversion", response3.Message, StringComparison.OrdinalIgnoreCase);

        var thirdPrompt = _capturedPrompts.Last(p => p.Contains("Current User Query: Going back to the principles we discussed, which one deals with dependency?"));
        Assert.Contains("Previous Response Context:", thirdPrompt);
        Assert.Contains("Dependency Inversion", thirdPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChatBotActor_Should_Selectively_Include_Last_Response()
    {
        var sessionId = Guid.NewGuid().ToString();
        var searchMemoryActor = CreateTestProbe();
        var decisionActor = CreateTestProbe();
        var supervisor = CreateSupervisor(sessionId, searchMemoryActor.Ref, decisionActor.Ref);

        SendAndResolve(supervisor, searchMemoryActor, decisionActor, sessionId,
            "Hi there!",
            new List<Memory>(),
            searchPerformed: false,
            hasRelevantMemories: false);

        SendAndResolve(supervisor, searchMemoryActor, decisionActor, sessionId,
            "Explain dependency injection in detail",
            new List<Memory>(),
            searchPerformed: false,
            hasRelevantMemories: false);

        var response3 = SendAndResolve(supervisor, searchMemoryActor, decisionActor, sessionId,
            "How does that relate to the SOLID principles?",
            new List<Memory>(),
            searchPerformed: false,
            hasRelevantMemories: false);

        Assert.Contains("SOLID", response3.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dependency injection", response3.Message, StringComparison.OrdinalIgnoreCase);

        var prompt = _capturedPrompts.Last(p => p.Contains("Current User Query: How does that relate to the SOLID principles?"));
        Assert.Contains("Previous Response Context:", prompt);
        Assert.Contains("dependency injection", prompt, StringComparison.OrdinalIgnoreCase);

        // Greeting can remain in recent conversation, but should not become the "important response" context.
        var importantSection = prompt.Split("Previous Response Context:").Last();
        var recentConversationIndex = importantSection.IndexOf("Recent Conversation:", StringComparison.Ordinal);
        if (recentConversationIndex >= 0)
        {
            importantSection = importantSection.Substring(0, recentConversationIndex);
        }
        Assert.DoesNotContain("Hi there!", importantSection, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChatBotActor_Should_Handle_Memory_Based_Response_Storage()
    {
        var sessionId = Guid.NewGuid().ToString();
        var searchMemoryActor = CreateTestProbe();
        var decisionActor = CreateTestProbe();
        var supervisor = CreateSupervisor(sessionId, searchMemoryActor.Ref, decisionActor.Ref);

        var solidMemory = CreateMemory(
            "SOLID Principles",
            "Dependency Inversion Principle (DIP) says high-level modules should not depend on low-level modules.");

        var response1 = SendAndResolve(supervisor, searchMemoryActor, decisionActor, sessionId,
            "Tell me about SOLID principles",
            new List<Memory> { solidMemory },
            searchPerformed: true,
            hasRelevantMemories: true);

        Assert.Equal(ResponseType.MemoryBased, response1.Type);

        var response2 = SendAndResolve(supervisor, searchMemoryActor, decisionActor, sessionId,
            "Which principle is about dependencies?",
            new List<Memory>(),
            searchPerformed: false,
            hasRelevantMemories: false);

        Assert.Contains("Dependency Inversion", response2.Message, StringComparison.OrdinalIgnoreCase);

        var prompt = _capturedPrompts.Last(p => p.Contains("Current User Query: Which principle is about dependencies?"));
        Assert.Contains("Previous Response Context:", prompt);
        Assert.Contains("DIP", prompt, StringComparison.OrdinalIgnoreCase);
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

    private ChatBotResponse SendAndResolve(
        IActorRef supervisor,
        TestProbe searchProbe,
        TestProbe decisionProbe,
        string sessionId,
        string message,
        List<Memory> memories,
        bool searchPerformed,
        bool hasRelevantMemories)
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
            Memories = memories,
            SearchPerformed = searchPerformed,
            RetryAttempts = 0
        });

        if (searchPerformed && memories.Count > 0)
        {
            var eval = decisionProbe.ExpectMsg<EvaluateRelevanceRequest>(TimeSpan.FromSeconds(3));
            Assert.Equal(sessionId, eval.SessionId);
            Assert.Equal(message, eval.Query);

            decisionProbe.Reply(new EvaluateRelevanceResponse
            {
                SessionId = sessionId,
                HasRelevantMemories = hasRelevantMemories,
                RelevantMemories = hasRelevantMemories ? memories : new List<Memory>(),
                Reasoning = hasRelevantMemories ? "relevant" : "not relevant"
            });
        }

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
            if (prompt.Contains("SOLID", StringComparison.OrdinalIgnoreCase) || prompt.Contains("DIP", StringComparison.OrdinalIgnoreCase))
            {
                return "Dependency Inversion (DIP): high-level modules should depend on abstractions.";
            }

            if (prompt.Contains("dependency injection", StringComparison.OrdinalIgnoreCase))
            {
                return "Dependency injection decouples object construction from behavior via abstraction.";
            }

            return "Important context summary.";
        }

        if (prompt.Contains("Extract the most important information from the following conversation exchange", StringComparison.Ordinal))
        {
            return "Short-term memory summary.";
        }

        if (prompt.Contains("Current User Query: What are the SOLID principles in software engineering?", StringComparison.Ordinal))
        {
            return "SOLID includes SRP, OCP, LSP, ISP, and Dependency Inversion Principle (DIP).";
        }

        if (prompt.Contains("Current User Query: Tell me about SOLID principles", StringComparison.Ordinal))
        {
            return "SOLID principles include DIP, which addresses dependency direction between modules.";
        }

        if (prompt.Contains("Current User Query: Going back to the principles we discussed, which one deals with dependency?", StringComparison.Ordinal))
        {
            return "That is Dependency Inversion Principle (DIP).";
        }

        if (prompt.Contains("Current User Query: Which principle is about dependencies?", StringComparison.Ordinal))
        {
            return "The principle is Dependency Inversion Principle (DIP).";
        }

        if (prompt.Contains("Current User Query: Explain dependency injection in detail", StringComparison.Ordinal))
        {
            return "Dependency injection is a design pattern that supplies dependencies from the outside, improving testability and modularity in large systems.";
        }

        if (prompt.Contains("Current User Query: How does that relate to the SOLID principles?", StringComparison.Ordinal))
        {
            return "Dependency injection supports SOLID, especially DIP, by making dependencies rely on abstractions.";
        }

        if (prompt.Contains("Current User Query: Hi there!", StringComparison.Ordinal))
        {
            return "Hi! How can I help you today?";
        }

        if (prompt.Contains("Current User Query: Let's talk about something else. What time is it?", StringComparison.Ordinal))
        {
            return "I cannot read system clock here, but I can help with timezone conversion.";
        }

        return "General assistant response.";
    }
}
