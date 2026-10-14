# 테스트 지형도 (Test Landscape)

> 사용 에이전트: [test-sentinel](../agents/test-sentinel.md)
> 원천: `src/Memorizer.IntegrationTests` 실측, `project-convention` 스킬,
> 하네스 도입 전 이력 `prompt/kr/15.x, 18, 26, 28, 49, 51`, `prompt/docs/05-WebSearchModule-TestResult.md` — **2026-10-14 현행 코드 대조 후 선별 도입**

## 스택 (실측)

xUnit · Moq · Akka.TestKit.Xunit2 · Akka.Hosting.TestKit · coverlet · 대상 프레임워크 **net9.0**

- `[Fact]` 63개, `[Fact(Skip = "Manual test ...")]` 3개, `[Trait("Category", "LiveWeb")]` 5개
- **Testcontainers.PostgreSql은 csproj에만 참조되고 사용처 0건** — DB 격리 테스트는 아직 존재하지 않는다
- 커버리지 설정: 루트 `coverlet.runsettings`

## 컴포넌트 ↔ 테스트 매핑 (2026-10-14 실측)

| 컴포넌트 | 테스트 | 상태 |
|---------|-------|------|
| ChatBotActor | ChatBotActorTests, ChatBotActorRealTests, ConversationContextTests, MultiSessionTests, LastResponseTests | 🟡 (아래 품질 결함) |
| AskBot (Controller+Actor) | AskBotActorTests, AskBotNoDuplicateTests | 🟡 중복 이벤트 회귀 테스트가 수동 Skip |
| SearchMemoryActor / DecisionActor | SearchMemoryActorTests, DecisionActorTests | 🟢 |
| SkillMakerActor | SkillMakerActorTests (4개) | 🟡 실제 크래시 경로(GatheringInfo→GeneratingSkill)·JSON 파싱 폴백·세션 타임아웃 미검증 |
| NewsController | NewsControllerTests (11개) | 🟢 |
| WebSearchService | WebSearchServiceTests(mock 9), WebSearchServiceLiveTests | 🟢 mock / 🟡 Live는 가짜 초록 |
| Custom 공급자 인증·임베딩 실패 처리 | CustomProviderAuthTests (7, stub HTTP) | 🟢 Bearer 전송, 키 없을 때 무헤더, 401·불완전 배치 시 예외 (2026-10-14) |
| MultiModalService | MultiModalServiceTests | 🔴 사내 LAN LLM 직접 호출 — LM Studio 토큰 인증 도입 후 키 없이 호출해 401 예상 |
| **MemoryTools (MCP)** | — | 🔴 무방비 |
| **GraphSync / Graph 검색 / GraphRelationship** | — | 🔴 무방비 (prompt 03·04에서 요청됐으나 미작성) |
| **MetadataEmbedding / TitleGeneration** | — | 🔴/🟡 무방비 |
| **Storage / SchemaMigrator** | — | 🔴 무방비 |
| **AuthenticationMiddleware / OAuth** | — (WebApplicationFactory 미사용) | 🔴 무방비 |
| **공유·ReMemorizer, PrdMaker/ShapeUp 메모리조각 선택, LLM 공급자 선택, UseExtendedModel 라우팅** | — | 🔴 무방비 |

> 점검 때마다 실측으로 갱신한다.

## 위험 가중치

| 가중치 | 해당 조건 | 예 |
|-------|---------|-----|
| ×3 | 데이터를 쓰거나 지운다 | Store, Delete, GraphSync(FullSync), 마이그레이션, 공유 upsert |
| ×3 | 외부 계약이다 (LLM 클라이언트·HTTP 클라이언트가 소비) | MCP 도구, OAuth 엔드포인트, 인증 미들웨어 |
| ×2 | 비동기 상태·타이머를 가진다 | 세션 액터, ReceiveTimeout, Task.Run+self.Tell |
| ×2 | **과거에 2회 이상 fix가 반복된 영역** | Graph 동기화, AskBot 비동기, 공유/ReMemorizer, 메모리조각 선택 |
| ×1 | 순수 변환·포맷팅 | 제목 생성, 결과 포맷터 |

## 알려진 테스트 품질 결함 (✅ 2026-10-14 검증)

| ID | 결함 | 근거 | 처방 방향 |
|----|------|------|----------|
| T1 | 라이브 테스트가 비활성/의존성 부재 시 `return` → **통과로 집계(가짜 초록)** | `WebSearchServiceLiveTests.cs:15, 108-113` | `Skip.If`(Xunit.SkippableFact) 또는 Trait 필터로 "건너뜀"을 드러냄 |
| T2 | 멀티모달 테스트가 사내 LAN LLM(`192.168.0.50`) 직접 호출, Trait/Skip 없음 | `MultiModalServiceTests.cs:21` | `Trait("Category","LiveLLM")` + env 게이트 |
| T3 | ChatBotActorRealTests가 실 LLM 의존, Trait 없음, 인프라 없으면 장시간 대기 | `ChatBotActorRealTests.cs` | T2와 동일 |
| T4 | 세션 타임아웃 테스트가 `Thread.Sleep(100)` 후 `Assert.True(true, ...)` — 아무것도 검증하지 않음 | `ChatBotActorTests.cs:184-190` | `Watch` + `ExpectTerminated` |
| T5 | Sleep/Delay 대기 다수 | `MultiSessionTests.cs`, `ChatBotActorTests.cs:187` 등 | `ExpectMsg`/`Within`/`AwaitAssert` |
| T6 | 중복 이벤트·다중 세션 회귀 테스트가 수동 Skip | `AskBotNoDuplicateTests.cs:26`, `MultiSessionTests.cs:20,139` | 실서버 의존을 걷어내고 TestKit으로 재작성 |

## 좋은 테스트 기준

- 액터: `ExpectMsg<T>` / `Within` / `ExpectTerminated` 사용, `Thread.Sleep` 대기 금지
- 외부 의존(LLM·웹·사내 서버)은 Moq로 대체하거나 **Trait + 환경변수 게이트**로 분리 — 모범: `RUN_WEB_LIVE_TESTS` + `Trait("Category","LiveWeb")` (🔒 `WebSearchServiceLiveTests.cs:12`)
- LLM 응답은 **형식만 assert**, 내용은 콘솔 출력으로 사람이 확인 (🔒 `ChatBotActorRealTests.cs:281-285` 패턴)
- 깨진 테스트를 고칠 때는 **원본(프로덕션) 코드를 수정하지 않는다** — 테스트 결함인지 프로덕션 결함인지 먼저 분류 (prompt 18·26 규칙)
- DB 테스트를 도입한다면 Testcontainers로 격리 (참조는 이미 있음)

## 검증 사다리 (개발자의 실제 검증 순서 — prompt 05-TestResult, 49~53)

1. `dotnet build` — 빌드 오류 0
2. 유닛/통합 테스트 (mock) — Live/LLM 계열 제외
3. env 게이트 라이브 테스트 (선택)
4. 로컬 실행 검증: docker는 **앱 컨테이너만 재빌드**(`--no-deps`), **postgres/neo4j 컨테이너는 down 금지**, DB는 **읽기로만 확인**
5. 최종 확인은 개발자가 직접 — 하네스는 결과와 함께 "사람이 확인할 항목"을 남긴다

## 회귀 패턴 경고

prompt 18 → 26에서 "테스트 대부분 실패" 상태가 **두 번** 반복됐다. 기능 개발 시 테스트를 함께 갱신하지 않는 패턴이 원인이다.
→ 변경 점검 시 "프로덕션 변경 파일 ↔ 대응 테스트 갱신 여부"를 항상 확인한다.
