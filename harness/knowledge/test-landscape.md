# 테스트 지형도 (Test Landscape)

> 사용 에이전트: [test-sentinel](../agents/test-sentinel.md)
> 원천: `src/Memorizer.IntegrationTests` 실측 (2026-10-14), `project-convention` 스킬의 테스트 패턴

## 스택

xUnit · Moq · Akka.TestKit.Xunit2 · Akka.Hosting.TestKit · Testcontainers.PostgreSql · coverlet

- `[Fact]` 63개, `[Fact(Skip = "Manual test ...")]` 3개, `[Trait("Category", "LiveWeb")]` 5개 (외부 웹 의존)
- 커버리지 설정: 루트 `coverlet.runsettings`

## 컴포넌트 ↔ 테스트 매핑 (2026-10-14 실측)

| 컴포넌트 | 테스트 | 상태 |
|---------|-------|------|
| ChatBotActor | ChatBotActorTests, ChatBotActorRealTests, ConversationContextTests, MultiSessionTests, LastResponseTests | 🟢 |
| AskBot (Controller+Actor) | AskBotActorTests, AskBotNoDuplicateTests | 🟢 |
| SearchMemoryActor | SearchMemoryActorTests | 🟢 |
| DecisionActor | DecisionActorTests | 🟢 |
| SkillMakerActor | SkillMakerActorTests | 🟢 |
| NewsController | NewsControllerTests | 🟢 |
| MultiModalService | MultiModalServiceTests | 🟢 |
| WebSearchService | WebSearchServiceTests, WebSearchServiceLiveTests | 🟢 (Live 격리) |
| **MemoryTools (MCP)** | — | 🔴 무방비 |
| **GraphSyncActor** | — | 🔴 무방비 |
| **GraphRelationshipActor** | — | 🔴 무방비 |
| **MetadataEmbeddingActor** | — | 🔴 무방비 |
| **TitleGenerationActor** | — | 🟡 무방비 (부작용 낮음) |
| Storage / 마이그레이션 | — | 🔴 무방비 (확인 필요) |
| AuthenticationMiddleware / OAuth | — | 🔴 무방비 |

> 점검 때마다 실측으로 갱신한다.

## 위험 가중치

| 가중치 | 해당 조건 | 예 |
|-------|---------|-----|
| ×3 | 데이터를 쓰거나 지운다 | Store, Delete, GraphSync, 마이그레이션 |
| ×3 | 외부 계약이다 (LLM 클라이언트·HTTP 클라이언트가 소비) | MCP 도구, OAuth 엔드포인트 |
| ×2 | 비동기 상태·타이머를 가진다 | 세션 액터, ReceiveTimeout, PipeTo |
| ×1 | 순수 변환·포맷팅 | 제목 생성, 결과 포맷터 |

## 좋은 테스트 기준

- 액터: `ExpectMsg<T>` / `Within` 사용, `Thread.Sleep` 대기 금지
- 외부 의존(LLM·웹·실 DB 외 서비스)은 Moq로 대체하거나 `Trait`로 분리해 기본 실행에서 제외
- DB 테스트는 Testcontainers로 격리, 테스트 간 데이터 공유 금지
