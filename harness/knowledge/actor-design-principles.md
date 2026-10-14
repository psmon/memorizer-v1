# 액터 설계 원칙 (Actor Design Principles)

> 사용 에이전트: [akka-net-specialist](../agents/akka-net-specialist.md)
> 원천: Akka.NET 공식 문서의 액터 모델 원칙, Aaronontheweb/dotnet-skills의 akka-net 설계 원칙, `.claude/skills/actor-model/SKILL.md`, `src/Memorizer/Actors/README.md`

## A1~A7 판단 기준

| # | 원칙 | 위반 신호 |
|---|------|---------|
| A1 | **상태는 액터 안에서만 바뀐다** | `Task.Run`/`ContinueWith` 콜백에서 필드 변경 |
| A2 | **await 뒤의 `Sender`는 믿지 않는다** — 미리 지역변수로 캡처 | `ReceiveAsync` 안에서 `await` 후 `Sender.Tell` |
| A3 | **비동기 결과는 `PipeTo`로 메시지가 되어 돌아온다** | fire-and-forget Task, `async void` 핸들러 |
| A4 | **메일박스를 막지 않는다** | `.Result`, `.Wait()`, 동기 I/O, 긴 CPU 루프 |
| A5 | **타이머와 자원은 수명주기에 묶는다** | `PostStop`에서 정리 없음, `ReceiveTimeout` 해제 누락, `IWithTimers` 미사용 수동 스케줄 |
| A6 | **실패는 감독자가 결정한다** | try/catch로 모든 예외를 삼켜 감독 무력화, `SupervisorStrategy` 미명시 |
| A7 | **메시지는 불변이다** | 메시지 클래스에 public setter·가변 컬렉션 |

## Memorizer 프로젝트 맥락

| 액터 | 성격 | 우선 확인 원칙 |
|------|------|-------------|
| ChatBotActor | 세션·타이머 기반 자동 종료, 핸들러 가장 많음 | A2, A3, A5 |
| SearchMemoryActor / DecisionActor | pgvector 검색 + LLM 판단 | A3, A4, A6 |
| MetadataEmbeddingActor | 임베딩 외부 호출, 쓰기 | A4, A6 |
| GraphSyncActor / GraphRelationshipActor | Neo4j 쓰기, 동기화 | A6 (재시도/중복 쓰기), A1 |
| TitleGenerationActor / SkillMakerActor | LLM 호출 | A3, A4 |

## 권장 패턴 (요약)

```csharp
// A2 + A3: Sender 캡처 후 PipeTo
Receive<Search>(msg =>
{
    var replyTo = Sender;
    _service.SearchAsync(msg.Query)
        .PipeTo(replyTo, success: r => new SearchResult(r), failure: ex => new Status.Failure(ex));
});

// A5: 타이머는 IWithTimers로
public ITimerScheduler Timers { get; set; } = null!;
Timers.StartSingleTimer("idle", new IdleTimeout(), TimeSpan.FromMinutes(5));
```
