using Akka.Actor;
using Memorizer.Actors;
using Memorizer.Services;

namespace Memorizer.IntegrationTests.Actors;

/// <summary>
/// Test supervisor for ChatBotActor to handle parent messages
/// </summary>
public class TestChatBotSupervisor : ReceiveActor
{
    private IActorRef _testActor;
    private IActorRef _chatBotActor;

    public TestChatBotSupervisor(
        string sessionId,
        IActorRef searchMemoryActor,
        IActorRef decisionActor,
        ILlmService llmService,
        IActorRef testActor)
    {
        _testActor = testActor;

        // Create ChatBotActor as child
        _chatBotActor = Context.ActorOf(
            ChatBotActor.Props(sessionId, searchMemoryActor, decisionActor, llmService),
            $"chatbot-{sessionId}");

        // Forward UserChatRequest to ChatBotActor
        Receive<UserChatRequest>(request =>
        {
            _chatBotActor.Forward(request);
        });

        // Forward ChatBotResponse to test actor
        Receive<ChatBotResponse>(response =>
        {
            _testActor.Tell(response);
        });

        // Forward other messages
        Receive<SessionTimeout>(msg => _chatBotActor.Forward(msg));
        Receive<ResetSessionTimer>(msg => _chatBotActor.Forward(msg));
    }

    public static Props Props(
        string sessionId,
        IActorRef searchMemoryActor,
        IActorRef decisionActor,
        ILlmService llmService,
        IActorRef testActor)
    {
        return Akka.Actor.Props.Create(() => new TestChatBotSupervisor(
            sessionId,
            searchMemoryActor,
            decisionActor,
            llmService,
            testActor));
    }
}