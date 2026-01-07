---
name: actor-model
description: Memorizer 프로젝트의 Akka.NET 액터 모델 구현 스킬. 세션 관리, 타이머 기반 자동 종료, 메시지 기반 통신, 상태 관리가 필요한 비동기 워크플로우 구현 시 사용. ChatBotActor, SearchMemoryActor, DecisionActor 패턴 참조.
---

# 액터 모델 구현 스킬

Memorizer 프로젝트에서 Akka.NET 기반 액터 모델을 구현하는 방법을 안내합니다.

## 프로젝트 구조

```
src/Memorizer/
├── Actors/
│   ├── ChatBotActor.cs           # 세션 기반 채팅봇 액터
│   ├── ChatBotMessages.cs        # 메시지 정의
│   ├── SearchMemoryActor.cs      # 메모리 검색 액터
│   ├── DecisionActor.cs          # 관련성 판단 액터
│   ├── TitleGenerationActor.cs   # 제목 생성 액터
│   ├── MetadataEmbeddingActor.cs # 임베딩 처리 액터
│   ├── GraphSyncActor.cs         # 그래프 동기화 액터
│   └── GraphRelationshipActor.cs # 관계 생성 액터
└── Program.cs                    # ActorSystem 등록
```

## 액터 모델 기본 패턴

### 1. 기본 액터 정의

```csharp
using Akka.Actor;
using Akka.Event;

public class MyActor : ReceiveActor
{
    private readonly ILoggingAdapter _logger;

    public MyActor()
    {
        _logger = Context.GetLogger();

        // 메시지 핸들러 등록
        Receive<MyRequest>(HandleRequest);
        Receive<MyOtherRequest>(HandleOtherRequest);
    }

    private void HandleRequest(MyRequest request)
    {
        _logger.Info("Processing request: {0}", request.Id);

        // 처리 로직
        var response = new MyResponse { Result = "Success" };
        Sender.Tell(response);
    }

    private void HandleOtherRequest(MyOtherRequest request)
    {
        // 다른 처리 로직
    }

    // Props 팩토리 메서드 (권장)
    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new MyActor());
    }
}
```

### 2. DI가 필요한 액터

```csharp
public class ServiceActor : ReceiveActor
{
    private readonly IMyService _service;
    private readonly ILoggingAdapter _logger;

    public ServiceActor(IMyService service)
    {
        _service = service;
        _logger = Context.GetLogger();

        Receive<ProcessRequest>(HandleProcess);
    }

    private void HandleProcess(ProcessRequest request)
    {
        // 비동기 작업은 PipeTo 또는 Task.Run + Tell 사용
        var self = Self;
        Task.Run(async () =>
        {
            try
            {
                var result = await _service.ProcessAsync(request.Data);
                self.Tell(new ProcessResponse { Result = result });
            }
            catch (Exception ex)
            {
                self.Tell(new Status.Failure(ex));
            }
        });
    }

    public static Props Props(IMyService service)
    {
        return Akka.Actor.Props.Create(() => new ServiceActor(service));
    }
}
```

## 타이머 기반 자동 종료 패턴

세션 기반 액터에서 비활성 시 자동 종료하는 패턴입니다.

```csharp
public class SessionActor : ReceiveActor, IWithTimers
{
    private const string SessionTimerKey = "session-timeout";
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromDays(3);

    public ITimerScheduler Timers { get; set; } = null!;

    public SessionActor(string sessionId)
    {
        Receive<UserRequest>(HandleRequest);
        Receive<SessionTimeout>(HandleTimeout);
        Receive<ResetTimer>(HandleResetTimer);

        // 초기 타이머 시작
        ResetSessionTimer();
    }

    private void HandleRequest(UserRequest request)
    {
        // 활동 시 타이머 리셋
        ResetSessionTimer();

        // 요청 처리
    }

    private void ResetSessionTimer()
    {
        Timers.StartSingleTimer(
            SessionTimerKey,
            new SessionTimeout(),
            SessionTimeout
        );
    }

    private void HandleTimeout(SessionTimeout _)
    {
        // 리소스 정리
        Context.Stop(Self);
    }

    private void HandleResetTimer(ResetTimer _)
    {
        ResetSessionTimer();
    }
}
```

## 상태 전환 패턴 (Become)

복잡한 워크플로우에서 상태에 따라 다른 메시지 처리가 필요한 경우:

```csharp
public class WorkflowActor : ReceiveActor
{
    public WorkflowActor()
    {
        // 초기 상태: 대기
        Become(WaitingForInput);
    }

    private void WaitingForInput()
    {
        Receive<StartProcess>(msg =>
        {
            // 처리 시작
            ProcessStep1(msg);
            Become(WaitingForStep1Response);
        });
    }

    private void WaitingForStep1Response()
    {
        Receive<Step1Response>(response =>
        {
            // Step1 완료, Step2 시작
            ProcessStep2(response);
            Become(WaitingForStep2Response);
        });

        // 다른 메시지도 처리 가능
        Receive<CancelRequest>(_ =>
        {
            Become(WaitingForInput);
        });
    }

    private void WaitingForStep2Response()
    {
        Receive<Step2Response>(response =>
        {
            // 완료, 결과 반환
            Sender.Tell(new FinalResult(response));
            Become(WaitingForInput);
        });
    }
}
```

## 액터 간 통신 패턴

### 1. Ask 패턴 (응답 대기)

```csharp
// 타임아웃과 함께 응답 대기
var response = await _actorRef.Ask<MyResponse>(
    new MyRequest(),
    TimeSpan.FromSeconds(30)
);
```

### 2. Tell 패턴 (Fire and Forget)

```csharp
// 응답 기대 없이 전송
_actorRef.Tell(new MyMessage());

// 응답을 받을 대상 지정
_actorRef.Tell(new MyRequest(), Self);
```

### 3. Forward 패턴

```csharp
// 원래 발신자 유지하며 전달
_anotherActor.Forward(message);
```

## 메시지 정의 패턴

```csharp
// 요청/응답 메시지
public record MyRequest(string Data, Guid CorrelationId);
public record MyResponse(string Result, Guid CorrelationId);

// 상태 변경 메시지
public record SessionTimeout;
public record ResetTimer;

// 복잡한 메시지
public class ChatBotResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ResponseType Type { get; set; }
    public List<Guid>? ReferencedMemoryIds { get; set; }
    public List<string> ReasoningSteps { get; set; } = new();
}
```

## 액터 시스템 등록 (Program.cs)

```csharp
// ActorSystem 등록
builder.Services.AddSingleton(sp =>
{
    var config = ConfigurationFactory.ParseString(@"
        akka {
            loglevel = INFO
            actor {
                debug {
                    receive = on
                    autoreceive = on
                    lifecycle = on
                }
            }
        }
    ");

    return ActorSystem.Create("memorizer-system", config);
});

// 싱글톤 액터 생성
builder.Services.AddSingleton<IActorRef>(sp =>
{
    var system = sp.GetRequiredService<ActorSystem>();
    var llmService = sp.GetRequiredService<ILlmService>();

    return system.ActorOf(
        SearchMemoryActor.Props(llmService),
        "search-memory-actor"
    );
});
```

## 유닛 테스트 패턴

```csharp
using Akka.TestKit.Xunit2;

public class MyActorTests : TestKit
{
    [Fact]
    public void Should_respond_to_request()
    {
        // Arrange
        var actor = Sys.ActorOf(MyActor.Props());

        // Act
        actor.Tell(new MyRequest { Data = "test" });

        // Assert
        var response = ExpectMsg<MyResponse>();
        Assert.Equal("Success", response.Result);
    }

    [Fact]
    public void Should_timeout_after_inactivity()
    {
        // Arrange
        var probe = CreateTestProbe();
        var actor = Sys.ActorOf(SessionActor.Props("test-session"));

        // Act & Assert
        Watch(actor);
        ExpectTerminated(actor, TimeSpan.FromSeconds(5));
    }
}
```

## ChatBotActor 패턴 예시

실제 프로젝트의 ChatBotActor 워크플로우:

```
1. UserChatRequest 수신
   ↓
2. AnalyzeQueryTypeRequest → SearchMemoryActor
   ↓
3. SearchMemoryRequest / MultiTopicSearchRequest
   ↓
4. EvaluateRelevanceRequest → DecisionActor
   ↓
5. GenerateResponse (LLM 사용)
   ↓
6. ChatBotResponse 반환
```

## 주의사항

1. **비동기 작업**: 액터 내에서 `await`는 지양, `Task.Run + Tell` 또는 `PipeTo` 사용
2. **상태 관리**: 액터의 상태는 스레드 세이프, 외부 공유 상태 주의
3. **메시지 불변성**: 메시지 객체는 불변으로 설계
4. **에러 처리**: `Status.Failure` 메시지로 에러 전파
5. **타이머**: `IWithTimers` 인터페이스와 `StartSingleTimer` 사용
