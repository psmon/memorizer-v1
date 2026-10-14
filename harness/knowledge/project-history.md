# 하네스 도입 전 이력 요약 (Project History Digest)

> 사용 에이전트: [tamer](../agents/tamer.md) (모든 전문가가 참고)
> 원천: `prompt/` 전체 (kr 01~53, docs/, worst-case/, skill-maker/, ai-prd*) — 2026-10-14 분석
> **`prompt/`는 2026-10-14 저장소에서 삭제되었다 (사용자가 별도 아카이브 보관).** 이후 요청·개선은 하네스로 진행한다. 이 문서가 저장소에 남은 유일한 이력 요약본이다. 본문의 `prompt/...` 표기는 아카이브 내 경로다.
> 모든 교훈은 현행 코드와 대조해 **✅ 현행 유효 / 🔒 이미 반영 / ❌ 폐기**로 판정했고, ✅·🔒만 각 전문가 지식에 도입했다.

## 1. 타임라인 (53개 지시 → 7개 시대)

| 시대 | 지시 | 내용 |
|------|------|------|
| 기반 확장 | 01, 10, 11 | OpenAI/Custom LLM·임베딩 provider, 768차원 전환 |
| Graph | 02~07, 14 | Neo4j 도입, NL→Cypher, Word 노드, `SearchGraphByCypher` |
| 인증·뷰 | 05, 08, 09, 12, 13, 16, 19 | 관리자 세션/ApiKey, 블로그·Mermaid·메뉴, PromptTemplates 통합, 설치 스크립트 |
| AskBot 액터 | worst-case/15, 15.01~15.03, 17, 32, 48, 51 | 세션 액터+SSE, 대화 맥락, multi-QA, LLM-EX, 웹검색 폴백 |
| 공유·재기억 | 20~25, 30 | 숏링크 공유, ReMemorizer, 이미지 저장 |
| 멀티모달·LLM API·OAuth | 28, 29, 31, 33, 34, 38 | qwen3-vl, 공개 `/api/llm`, MCP OAuth2 |
| 기획 도구 | 35~47, 49, 50, 52, 53 | Memory Architecture, PRD Maker, ShapeUp 화이트보드, SkillMaker, AI Tech News |

테스트 정비: 18, 26 (두 번 모두 "대부분 실패" 상태에서 복구)

## 2. 반복 실패 지대 — 근본 원인 잔존 여부

| 지대 | 반복 | 잔존 원인 (✅) | 담당 |
|------|------|--------------|------|
| Graph 동기화 | 02→02FIX→03→04→06→07 | Delete/Update 미동기화, 액터/폴백 FullSync 의미 불일치, `/api/graph/sync` 무인증 | mcp-tool-inspector(K2,K9), akka(AK6) |
| AskBot 비동기 | worst→15.01→15.02→15.03→17 | Task.Run 상태 변경, 실패 무응답, 대기 중 요청 유실, 싱글턴 직렬화, 세션 누수 | akka(AK1~AK5) |
| 테스트 부패 | 18→26 | 기능 개발 시 테스트 미갱신, 가짜 초록, 외부 의존 미격리 | test-sentinel(T1~T6) |
| 메모리 조각 선택(RAG) | 37→39→40→43→45 | 규칙 3회 교체, PrdMaker/ShapeUp에 복제, 테스트 없음 | (후보: prompt-curator) |
| ShapeUp 캔버스 UI | 41~47 | 자체 캔버스 JS, UI 테스트 없음 | (후보: frontend-view-inspector) |
| Mermaid 렌더링 | 36, 37, 51, 53 | 프롬프트 규칙으로만 대응 (`ChatBotActor.cs:46-58`) | (후보: frontend-view-inspector) |
| LLM 공급자 | 01, 10, 11, 33, 38 | 추상화 3벌(ILlmService/ILlmExService/IMultiModalService), 임베딩 오류 시 **랜덤 벡터 폴백** — Custom은 2026-10-14 제거(🔒 예외 전파), **Ollama·OpenAI 임베딩에는 잔존**(`OllamaEmbeddingService.cs`, `OpenAIEmbeddingService.cs`), 알 수 없는 Type은 조용히 Custom | (후보: llm-provider-specialist) |

교훈: **worst-case/15** — 플로팅 UI+에이전트+액터+SSE를 한 프롬프트로 요청 → 폐기 → 3단계로 분할해 성공.
→ 하네스에서도 큰 요청은 엔진 단계로 쪼갠다.

## 3. 개발자의 요청 스타일 (트리거·엔진 설계용)

- **고정 머리말**: "이 프로젝트는 dotnet 9.0…" → `# 지침` → `## 프로젝트 위치및 설명`(후기엔 agent.md 참조) → `## 로컬 테스트방법` → `## 테스트시 API인증방법`
- **후속 수정**: 같은 파일에 `추가개선`/`추가fix`/`N회차`/`as is / to be`를 덧붙이거나 `-fix`, `-fix2` 접미 새 파일
- **증상 그대로 붙여넣기**: 콘솔 오류, Akka 로그·스택트레이스, 문제 숏링크 ID, 추론 과정 출력 → "원인분석해", "확인후 개선"
- **자주 쓰는 표현**: "기존 기능 참고/그대로 활용", "인증없이", "LLM-EX 이용", "한국어로 답할 것", "스트리밍(SSE)", "메모리 조각", "목적달성을 위한 프롬프트 설계"
- **구어체 축약**: "~해죠" 등. 트리거 매칭은 어미보다 핵심 명사("점검", "fix", "개선")로 한다

## 4. 개발자의 검증 규칙 (모든 수행부에 적용)

| 규칙 | 출처 |
|------|------|
| 빌드 오류 0 → 유닛테스트 → 로컬 실행(포트 5012 / Sam 환경) → 런타임 오류 개선 | 49~53 |
| docker는 **앱 컨테이너만 재빌드**(`--no-deps`), **postgres/neo4j는 down 금지** | 11~25, docs/05 |
| DB는 **읽기로만 확인**, 스키마 변경·직접 조작 금지 — 변경은 API로 | 반복 |
| 테스트 수리 시 **원본 코드 수정 금지** | 18, 26 |
| LLM 응답은 assert 말고 콘솔로 사람이 확인 | 15.01, 18 |
| 최종 확인은 개발자가 직접 테스트하고 피드백 | 공통 |

## 5. 방법론 → 하네스 대응

| 하네스 이전 방식 | 하네스 대응 | 상태 |
|-----------------|-----------|------|
| 번호 지시 → 구현 → `agent.md` 지도 갱신 → 스킬 갱신 (skill-maker 루프) | tamer "하네스를 업데이트해" + 버전 히스토리 | 대체 |
| `translate.md` (kr→en, agent.md, VersionPrefix 일괄) | tamer 릴리즈(Mode E 버전 관리) | 대체 (`agent.md` 마지막 반영 지시 = 53) |
| skill-maker "스킬 선택 플로우차트" | 엔진 라우팅 | 대체 |
| docs/05 검증 사다리 (mock → env 게이트 라이브 → docker `--no-deps`) | 향후 `build-verify` 엔진 단계 | **채택 예정** |
| docs/02 "Agent X가 Agent Y를 검증", docs/03 교차 모델 검증 | 향후 엔진 마지막 교차 확인 단계 | **채택 예정** |
| docs/02 `/log_error` (트리거 프롬프트 원문 + 원인 분류) | 로그 템플릿 확장 후보 (스키마 변경 = Minor) | 검토 |
| docs/04 스킬 설계 원칙 (description = 무엇+언제, 성공 기준 정량화) | mcp-tool-inspector D1, tamer 스킬 평가 | 채택 |

## 6. ai-prd 아이디어 구현 현황 (2026-10-14)

구현됨 0 / 부분 3 / 미구현 8

- **부분**: 03 관련 메모리 추천(News 그래프 기반만, `NewsController.cs:155`), 08 그래프 시각화 개선(vis-network 필터·더블클릭), ai-prd2 자동 관계 발견(벡터 기반 제안 + 일괄 API, 주기 실행 액터 없음 — GraphRelationshipActor 고아)
- **미구현**: 01 자동 태깅, 02 요약, 04 음성 입력, 05 대화 내보내기, 06 버전 이력, 07 스마트 필터, 09 Webhook, 10 프롬프트 템플릿

## 7. 영입 후보 (현 3인이 커버하지 못하는 영역)

| 후보 | 근거 (지시 수) | 핵심 쟁점 (✅ 검증) |
|------|-------------|-----------------|
| **web-ingress-guard** (보안) | 01, 05, 06, 29~31, 34, 40 | `/api/llm` 익명·CORS `*`·rate limit 없음(`Program.cs:66-73`), 웹 fetch SSRF 방어 없음(`WebSearchService.cs:89`), 미사용 `generate-wireframe`가 LLM HTML을 wwwroot에 기록(`PrdMakerController.cs:674`), 공유 이미지 경로 검증 없음(`AskBotController.cs:687`), 503에 `ex.Message` 노출(`LLMController.cs:102`) |
| **graph-sync-specialist** | 7 (02~07, 14) | PG↔Neo4j 정합성, NL→Cypher 품질 |
| **prompt-curator** | 12 | 프롬프트가 PromptTemplates + 액터 내부 상수로 분산, LLM JSON 파싱 폴백, 메모리 조각 선택 규칙 |
| **frontend-view-inspector** | 약 15 | Mermaid 4회, ShapeUp 캔버스 7회, SSE 뷰 |
| **llm-provider-specialist** | 6 | 3벌 추상화, 랜덤 벡터 폴백(Custom은 해소), 조용한 Custom 폴백 |

> 웹검색: 2026-10-14 네이버 Headless 고정 → SearXNG(구글·네이버·빙 등 메타검색) + ProviderChain 폴백으로 교체. Bing API는 2025-08 종료, Google CSE는 신규 불가(2027-01 종료) — API 모드에 의존하지 말 것.
| storage-migration-keeper | 4~11 | SchemaMigrator, 마이그레이션 016~022, Storage 테스트 0 |

> 영입은 한 번에 1~2명. 보안(web-ingress-guard)과 그래프(graph-sync-specialist)가 Critical 결함을 품고 있어 우선순위가 높다.
