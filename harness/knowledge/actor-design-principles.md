# 액터 설계 원칙 (Actor Design Principles)

> 사용 에이전트: [akka-net-specialist](../agents/akka-net-specialist.md)
> 원천: Akka.NET 공식 문서의 액터 모델 원칙, Aaronontheweb/dotnet-skills의 akka-net 설계 원칙, `.claude/skills/actor-model/SKILL.md`, `src/Memorizer/Actors/README.md`,
> 하네스 도입 전 이력 `prompt/worst-case/15`, `prompt/kr/15.01~15.03, 17, 32, 48, 49, 51`, `prompt/docs/00-ACTOR-AGENT.md` — **2026-10-14 현행 코드 대조 후 선별 도입**

## A1~A9 판단 기준

| # | 원칙 | 위반 신호 |
|---|------|---------|
| A1 | **상태는 액터 안에서만 바뀐다** | `Task.Run`/`ContinueWith` 콜백에서 필드 변경·읽기 |
| A2 | **액터 컨텍스트 밖에서는 `Sender`/`Context`를 쓰지 않는다** — `ReceiveAsync` 안의 await 뒤에서는 Akka가 컨텍스트를 보존하지만, **`Task.Run`·콜백 안에서는 보존되지 않는다**. 미리 `var self = Self; var replyTo = Sender;` 캡처 | `Task.Run` 안에서 `Sender`·`Context`·`Become` 사용 ("no active ActorContext" 크래시, prompt 49) |
| A3 | **비동기 결과는 메시지가 되어 돌아온다** — `PipeTo` 또는 캡처한 `self.Tell` | fire-and-forget Task, `async void` 핸들러 (prompt 15.03에서 제거됨) |
| A4 | **메일박스를 막지 않는다** | `.Result`, `.Wait()`, 동기 I/O, 긴 CPU 루프 |
| A5 | **타이머와 자원은 수명주기에 묶는다** | `PostStop` 정리 없음, `IWithTimers` 미사용 수동 스케줄, 종료 후 도착하는 Task 결과(dead letter) |
| A6 | **실패는 감독자가 결정하고, 요청자는 반드시 답을 받는다** | 예외를 삼켜 감독 무력화, `Status.Failure`를 특정 조건에서만 사용자에게 전달 |
| A7 | **메시지는 불변이다** | 메시지 클래스에 public setter·가변 컬렉션 |
| A8 | **`Become` 대기 상태에서 받은 요청은 버리지 않는다** — Stash 또는 명시적 "busy" 응답 | 대기 Receive가 `return false`로 새 요청을 unhandled 처리 |
| A9 | **외부 레지스트리(컨트롤러 static 딕셔너리 등)에 담긴 ActorRef는 액터 종료와 함께 지운다** | `ConcurrentDictionary<string, IActorRef>`에 TryRemove 경로 없음, `GetOrAdd` 안에서 `ActorOf`(팩토리 중복 실행 시 이름 충돌) |

### 처리량 원칙 (설계 판단)

- **싱글턴 + `ReceiveAsync`** 액터는 한 번에 한 메시지만 처리한다 → 그 액터가 LLM을 호출하면 **모든 세션이 직렬화**된다.
  세션 수에 비례해야 하는 작업은 세션별 자식 액터나 라우터(pool)로 분리한다.

## Memorizer 프로젝트 맥락

| 액터 | 성격 | 우선 확인 원칙 |
|------|------|-------------|
| ChatBotActor | 세션·3일 유휴 타이머, Task.Run 5곳, Become 대기 | A1, A2, A6, A8 |
| SearchMemoryActor / DecisionActor | **싱글턴**(`search-memory`, `decision`) + ReceiveAsync, LLM 호출 | 처리량 원칙, A6 |
| SkillMakerActor | 상태 머신(직군→하위스킬→GatheringInfo→GeneratingSkill), Task.Run | A2, A5 |
| MetadataEmbeddingActor | 임베딩 외부 호출, 쓰기 | A4, A6 |
| GraphSyncActor | Neo4j 쓰기, 컨트롤러의 Ask(5초) 실패 시 폴백 경로와 경합 | A6, A1 |
| GraphRelationshipActor | DI에 등록만 됨, 메시지를 보내는 곳 없음 (고아 가능성) | 존재 이유 확인 |
| TitleGenerationActor | LLM 호출 | A3, A4 |

## 알려진 결함 레지스트리 (✅ 2026-10-14 코드 검증)

점검 시 **먼저 이 목록의 해소 여부를 확인**하고, 해소되면 회귀 가드로 옮긴다.

| ID | 결함 | 심각도 | 근거 | 출처 |
|----|------|-------|------|------|
| AK1 | ChatBotActor가 `Task.Run` 안에서 `_conversationHistory`·`_reasoningSteps`를 변경하고 `_useExtendedModelForCurrentRequest`를 읽음. 이미 기본 상태로 돌아가 새 요청의 Clear와 경합 | **Critical** | `ChatBotActor.cs:626, 729, 804→831, 864, 988` | 15.03, 17, 48, 51 |
| AK2 | 대기(Become) 상태에서 새 `UserChatRequest`를 버림 (Stash 없음), 단계별 타임아웃 없음 | Moderate | `ChatBotActor.cs:224-251` | 15.03, 32 |
| AK3 | `Status.Failure`를 메시지에 "session"이 들어 있을 때만 사용자에게 전달 → 그 외 실패는 무응답 | Moderate | `ChatBotActor.cs:1254-1271` | 15.02 |
| AK4 | `AskBotController`·`ClaudeCodeController`의 static `SessionActors`가 액터 종료 후 지워지지 않음 → 죽은 Ref와 sse-bridge 누수, GetOrAdd 안의 ActorOf 중복 실행 시 이름 충돌 | Moderate | `AskBotController.cs:34,1248`, `ClaudeCodeController.cs:59,377` (참고: `ChatBotController.cs:122`는 제거함) | 15.01, 49 |
| AK5 | Search/Decision 싱글턴 직렬화 — "동시 요청 시 한쪽이 끊김"(15.03)의 원인 후보 | Moderate | `ServiceCollectionExtensions.cs:354-361`, `SearchMemoryActor.cs:95-97` | 15.03, docs/00 |
| AK6 | GraphSyncActor가 `FullSync`를 위치 인자로 `CreateRelationships`에 넘김. 액터 경로는 그래프를 초기화하지 않고 폴백만 초기화. Ask 실패 시 폴백과 경합 | Moderate | `GraphSyncActor.cs:152-155, 292-295`, `GraphSyncController.cs:280-329` | 02-FIX, 04 |
| AK7 | SkillMaker의 Task.Run에 취소 수단 없음 → 종료 후 결과가 dead letter | Info | `SkillMakerActor.cs:497-515, 626` | 49 |

## 회귀 가드 (🔒 이미 반영)

- `async void` 제거 → `Task.Run` + 캡처한 `self.Tell` (`ChatBotActor.cs:621-636, 808-821`; `SkillMakerActor.cs:180-195`)
- 비동기 시작 **전에** `Become` (`SkillMakerActor.cs:124, 289, 442`)
- `UseExtendedModel`을 하위 액터 메시지까지 전달 (`ChatBotMessages`, `SearchMemoryActor.cs:105`, `DecisionActor.cs:75`)
- 토큰 초과 시 3→1→일반 응답 폴백 (`ChatBotActor.cs:649`), 대화 10턴·단기 메모리 500자 (`ChatBotActor.cs:36-37`)
- GraphSyncActor DI는 `resolver.Props` 사용 (`ServiceCollectionExtensions.cs:349`)

## 폐기된 이력 (❌ 참고하지 말 것)

- 웹검색 전용 액터 3종(WebSearch/Fetch/EvidenceRank) 설계안 → ChatBotActor 내부 메서드로 구현됨 (`ChatBotActor.cs:1011, 1026`)
- 네이버/빙/구글 3곳 병렬 검색 → 한때 네이버 Headless 고정 → **2026-10-14 SearXNG 메타검색 + `WebSearch:ProviderChain` 폴백으로 교체** (`IWebSearchService.SearchWithFallbackAsync`), v1.4.7에서 Playwright 패키지 제거

## 권장 패턴 (요약)

```csharp
// A1 + A2 + A3: 상태 스냅샷을 넘기고, 결과는 메시지로 받아 액터 안에서 상태 변경
Receive<UserChatRequest>(msg =>
{
    var replyTo = Sender;
    var history = _conversationHistory.ToList();      // 스냅샷
    var useEx = _useExtendedModelForCurrentRequest;   // 값 캡처
    GenerateAsync(msg, history, useEx)
        .PipeTo(Self, success: r => new GeneratedResponse(r, replyTo),
                      failure: ex => new GenerationFailed(ex, replyTo));
});
Receive<GeneratedResponse>(r => { _conversationHistory.Add(...); r.ReplyTo.Tell(...); });

// A8: 대기 상태에서는 Stash
Become(Waiting);   // Waiting 안에서: Receive<UserChatRequest>(_ => Stash.Stash());
// 완료 시: Stash.UnstashAll(); Become(Ready);

// A5: 타이머는 IWithTimers로
Timers.StartSingleTimer("idle", new IdleTimeout(), TimeSpan.FromDays(3));
```
