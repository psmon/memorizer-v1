using Akka.Actor;
using Akka.TestKit.Xunit2;
using Memorizer.Actors;
using Memorizer.Services;
using Moq;
using Xunit;

namespace Memorizer.IntegrationTests.Actors;

public class SkillMakerActorTests : TestKit
{
    private readonly Mock<ILlmExService> _mockLlmExService;

    public SkillMakerActorTests()
    {
        _mockLlmExService = new Mock<ILlmExService>();
    }

    [Fact]
    public void SkillMakerActor_Should_Send_Welcome_Options_Via_Bridge()
    {
        // Arrange
        var sessionId = "test-skill-session-1";
        var sseBridge = CreateTestProbe();

        var actor = Sys.ActorOf(SkillMakerActor.Props(sessionId, _mockLlmExService.Object, sseBridge));

        // Act - send a category message
        _mockLlmExService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync(@"[{""name"":""코드 리뷰"",""description"":""코드 리뷰 자동화""},{""name"":""테스트 생성"",""description"":""테스트 코드 생성""}]");

        actor.Tell(new SkillMakerUserMessage
        {
            SessionId = sessionId,
            Message = "개발자",
            MessageType = SkillMakerMessageType.Category
        });

        // Assert - should receive phase update then options
        var phaseUpdate = sseBridge.ExpectMsg<SkillMakerStreamingUpdate>(TimeSpan.FromSeconds(5));
        Assert.Equal(SkillMakerUpdateType.Phase, phaseUpdate.UpdateType);

        var optionsUpdate = sseBridge.ExpectMsg<SkillMakerStreamingUpdate>(TimeSpan.FromSeconds(10));
        Assert.Equal(SkillMakerUpdateType.Options, optionsUpdate.UpdateType);
        Assert.NotNull(optionsUpdate.Options);
        Assert.True(optionsUpdate.Options.Count > 0);
    }

    [Fact]
    public void SkillMakerActor_Should_Handle_Category_Then_SubSkill_Selection()
    {
        // Arrange
        var sessionId = "test-skill-session-2";
        var sseBridge = CreateTestProbe();

        _mockLlmExService.Setup(x => x.CompleteAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string prompt, CancellationToken ct) =>
            {
                if (prompt.Contains("직군에서 유용한 Claude Code 스킬 5개를 추천"))
                    return @"[{""name"":""커밋 메시지 생성"",""description"":""Git 커밋 메시지 자동 생성""}]";
                if (prompt.Contains(@"이미 충분한 정보가 수집되었다면 정확히 ""READY"""))
                    return @"{""question"":""이 스킬의 주요 입력 파일은 무엇인가요?"",""insight"":""입력 컨텍스트가 명확해야 재현 가능한 워크플로우를 설계할 수 있습니다."",""example"":""예: src/**/*.cs 파일과 최근 Git diff를 입력으로 사용""}";

                return "READY";
            });

        var mockStream = AsyncEnumerableHelper.Create(new[] { "---\nname: test\n---\n# Test" });
        _mockLlmExService.Setup(x => x.CompleteStreamingAsync(It.IsAny<string>(), default))
            .Returns(mockStream);

        var actor = Sys.ActorOf(SkillMakerActor.Props(sessionId, _mockLlmExService.Object, sseBridge));

        // Act 1 - Select category
        actor.Tell(new SkillMakerUserMessage
        {
            SessionId = sessionId,
            Message = "개발자",
            MessageType = SkillMakerMessageType.Category
        });

        // Expect phase + options
        sseBridge.ExpectMsg<SkillMakerStreamingUpdate>(TimeSpan.FromSeconds(5));
        sseBridge.ExpectMsg<SkillMakerStreamingUpdate>(TimeSpan.FromSeconds(10));

        // Act 2 - Select sub-skill
        actor.Tell(new SkillMakerUserMessage
        {
            SessionId = sessionId,
            Message = "커밋 메시지 생성",
            MessageType = SkillMakerMessageType.SubSkill
        });

        // Assert - follow-up should now support JSON(question/insight/example) format
        var update = sseBridge.ExpectMsg<SkillMakerStreamingUpdate>(TimeSpan.FromSeconds(10));
        Assert.NotNull(update);
        Assert.Equal(SkillMakerUpdateType.Question, update.UpdateType);
        Assert.False(string.IsNullOrWhiteSpace(update.Content));
        Assert.False(string.IsNullOrWhiteSpace(update.Insight));
        Assert.False(string.IsNullOrWhiteSpace(update.Example));
    }

    [Fact]
    public void SkillMakerActor_Should_Create_Via_Props_Factory()
    {
        // Arrange
        var sessionId = "test-skill-session-3";
        var sseBridge = CreateTestProbe();

        // Act
        var actor = Sys.ActorOf(SkillMakerActor.Props(sessionId, _mockLlmExService.Object, sseBridge));

        // Assert - actor should be created without errors
        Assert.NotNull(actor);
    }

    [Fact]
    public async Task SkillMakerSSEBridgeActor_Should_Forward_Updates()
    {
        // Arrange
        var sessionId = "test-bridge-session";
        SkillMakerStreamingUpdate? receivedUpdate = null;
        var tcs = new TaskCompletionSource<bool>();

        var bridgeActor = Sys.ActorOf(SkillMakerSSEBridgeActor.Props(sessionId, update =>
        {
            receivedUpdate = update;
            tcs.SetResult(true);
            return Task.CompletedTask;
        }));

        // Act
        var testUpdate = new SkillMakerStreamingUpdate
        {
            SessionId = sessionId,
            UpdateType = SkillMakerUpdateType.Phase,
            Content = "Test phase"
        };
        bridgeActor.Tell(testUpdate);

        // Assert
        await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.NotNull(receivedUpdate);
        Assert.Equal("Test phase", receivedUpdate.Content);
        Assert.Equal(SkillMakerUpdateType.Phase, receivedUpdate.UpdateType);
    }
}

/// <summary>
/// Helper to create IAsyncEnumerable from array for testing
/// </summary>
internal static class AsyncEnumerableHelper
{
    public static async IAsyncEnumerable<string> Create(string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Delay(1);
        }
    }
}
