---
date: 2026-10-14T15:15:00+09:00
agent: tamer
type: improvement
mode: log-eval
trigger: "하네스를 업데이트해"
---

# 하네스 도입 전 이력(prompt/) 분석과 전문가·지식 갱신

## 실행 요약

사용자 요청: "과거 prompt 하위분석해 전문가 업데이트및 지식을 업데이트". 후속 지시는 "과거내용일수도 있으니 작성코드를 비교검증해 지식을 선별도입할것"이었다.
tamer의 업데이트 절차로 처리했다.

1. `prompt/` 144개 파일을 세 범위로 나눠 분석했다: kr 01~26과 worst-case, kr 27~53, docs·agent.md·skill-maker·ai-prd. kr이 원본이고 en은 번역이다
2. 모든 교훈을 현행 코드와 대조해 ✅ 현행 유효 / 🔒 이미 반영 / ❌ 폐기로 판정했다
3. 정원지기가 핵심 주장을 직접 재검증했다. 대상은 Cypher 부분 문자열 오탐, `/api/graph/sync` 공개, `default-api-key` 폴백, Task.Run 내부 상태 변경, `SessionActors` 미정리, 랜덤 임베딩 폴백, Testcontainers 미사용, LAN LLM 테스트, `Assert.True(true)`, CORS `AllowAnyOrigin`, 503 `ex.Message` 노출이다
4. ✅와 🔒만 knowledge와 에이전트 절차에 도입하고, ❌는 "폐기 이력"으로 따로 표시했다

## 결과

- 신규 `knowledge/project-history.md`: `prompt/` 아카이브 이후 남는 유일한 이력 요약
- 3개 knowledge에 **알려진 결함 레지스트리**를 도입했다: MCP K1~K9, 테스트 T1~T6, 액터 AK1~AK7
- 3개 전문가에 **Step 0(레지스트리 확인)** 을 추가하고 이력 기반 점검 항목을 보강했다
- tamer에 참조 지식과 선별 도입 원칙을 넣었다
- 신규 `todo/2026-10-14-history-backlog.md`: Critical 3건, 테스트 정비, 엔진 3종, 영입 후보
- v1.1.0 → **v1.1.1** (Patch: 지식 보강과 기존 에이전트 수정)

도입 규모: 결함 레지스트리 22건(K9 + T6 + AK7), 회귀 가드 8건(MCP 3 + 액터 5), 테스트 모범 사례 2건. ❌ 폐기는 5건이며 `docs/v1.1.1.md`에 기록했다

## 평가

| 축 | 등급 | 근거 |
|----|------|------|
| 워크플로우 개선도 | A | 53회 지시에 흩어져 있던 교훈이 전문가별 레지스트리로 모였다. 이제 점검이 "알려진 결함 확인"부터 시작하고, 낡은 이력(❌ 5건)은 판단 근거에서 빠졌다 |
| Claude 스킬 활용도 | 4/5 | 기존 스킬(mcp-tools, actor-model, project-convention)과 지식의 분리를 유지했다. 이력의 방법론(skill-maker 루프, translate)이 tamer 절차로 대체된다는 점을 명시했다. graph-db, llm-api 스킬과 연결할 전문가는 아직 없다 |
| 하네스 성숙도 | L2 | knowledge 4 / agents 3+tamer / engine 0. 지식은 두꺼워졌지만 물길(엔진)이 여전히 없다 |

## 다음 단계 제안

- `todo/2026-10-14-history-backlog.md`로 승격했다
- 최우선: `full-review` 엔진 정의. 그 다음 Critical 결함(K2, K4, AK1)을 해당 전문가 트리거로 점검하고 수정한다
- `prompt/` 아카이브 완료(사용자 보관, 저장소에서 삭제). knowledge의 `prompt/...` 출처는 아카이브 내 경로로 유지한다
