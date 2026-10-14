---
date: 2026-10-14T14:55:00+09:00
agent: tamer
type: recruit
mode: recruit
trigger: "제안한 전문가 모두 영입해죠"
---

# 초기 전문가 3명 영입 — mcp-tool-inspector, test-sentinel, akka-net-specialist

## 실행 요약

init 직후 온보딩(Case 2-A)에서 제안한 3명을 사용자가 일괄 수락했다.
영입 워크플로우(5-파일 패턴)에 따라 원천을 분석하고 에이전트·지식을 생성했다.

원천 분석:
- **mcp-tool-inspector** — `.claude/skills/mcp-tools/SKILL.md`, `src/Memorizer/Tools/MemoryTools.cs`(도구 8종: Store, Search, Get, GetMany, Delete, CreateRelationship, SearchGraph, SearchGraphByCypher)
- **test-sentinel** — `src/Memorizer.IntegrationTests` 실측(테스트 파일 15개, `[Fact]` 63 / Skip 3 / LiveWeb Trait 5), 스택 xUnit·Moq·Akka.TestKit·Testcontainers
- **akka-net-specialist** — 영입 카탈로그(.NET 특화), `.claude/skills/actor-model/SKILL.md`, `src/Memorizer/Actors/` 액터 8종

## 결과

| # | 파일 | 상태 |
|---|------|------|
| 1 | `harness/agents/{mcp-tool-inspector,test-sentinel,akka-net-specialist}.md` | 신규 |
| 3 | `harness/knowledge/{mcp-tool-contract,test-landscape,actor-design-principles}.md` | 신규 |
| 4 | `harness/harness.config.json` | agents 4명, v1.1.0 |
| 5 | `harness/docs/v1.1.0.md` | 신규 |

- 타입: 3명 모두 specialist (sage/keeper 확장 필드 추가 없음)
- 트리거 등록: 프로젝트 전용 판정 → harness frontmatter 자동 발견 (표준 SKILL.md 미수정)
- 영입 중 발견: MemoryTools(MCP)·GraphSync/GraphRelationship/MetadataEmbedding 액터·인증 미들웨어에 대응 테스트 없음 → `test-landscape.md`에 무방비 구역으로 기록

## 평가

| 축 | 등급 | 근거 |
|----|------|------|
| 워크플로우 개선도 | B | 점검 주체 3명과 판단 기준이 생겼다. 다만 엔진이 없어 "전체 점검해" 일괄 실행은 아직 불가하고 개별 트리거로만 동작한다 |
| Claude 스킬 활용도 | 4/5 | 기존 프로젝트 스킬 `mcp-tools`·`actor-model`·`project-convention`을 툴로 연결하고, 지식은 knowledge/로 분리했다. 나머지 스킬(graph-db, embedding-service, llm-api)은 아직 연동되지 않았다 |
| 하네스 성숙도 | L2 | knowledge 3 / agents 3+tamer / engine 0. 물길이 없다 |

## 다음 단계 제안

- `full-review` 엔진 정의: 3명 순차 실행 → 심각도별 집계 ("전체 점검해" 활성화)
- `targeted-review` 엔진 정의: `git diff` 기반 영향 에이전트만 투입 ("변경 점검해" 활성화)
- 첫 수행: "MCP 도구 점검해"로 mcp-tool-inspector 실전 투입 → 기준(D1~D5) 보정
