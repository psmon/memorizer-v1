# 이력 분석 백로그 (2026-10-14)

> 출처: [tamer 로그](../logs/tamer/2026-10-14-15-15-absorb-prompt-history.md). 모든 항목은 현행 코드로 검증했다(✅).
> 실행할 때는 하네스 트리거로 요청한다. 끝난 항목은 체크하고, 해당 knowledge 레지스트리를 회귀 가드로 옮긴다.

## Critical 결함: 수정 후보

- [ ] **K2**: `POST /api/graph/sync`를 인증 없이 호출할 수 있고, FullSync 폴백이 그래프 전체를 삭제한다. "MCP 도구 점검해" 후 수정한다
- [ ] **K4**: ApiKey가 없으면 `default-api-key`로 폴백한다. 설정이 없을 때 기동을 거부하도록 바꾼다
- [ ] **AK1**: ChatBotActor가 `Task.Run` 안에서 상태를 바꾼다. 스냅샷을 넘기고 결과를 메시지로 받는 구조로 바꾼다. "액터 점검해" 후 수정하고, test-sentinel이 회귀 테스트를 짝으로 만든다

## 테스트 신뢰도: 우선 정비

- [ ] **T1·T2·T3**: 라이브·LLM 의존 테스트를 Trait와 env 게이트로 격리하고 가짜 초록을 없앤다
- [ ] **T4**: 세션 타임아웃 테스트를 `ExpectTerminated`로 바꾼다

## 하네스 구조

- [ ] `full-review` 엔진: 3인을 순차 실행하고 결과를 심각도별로 집계한다. 마지막에 교차 확인 단계를 둔다(docs/02 방법론)
- [ ] `targeted-review` 엔진: `git diff` 기준으로 관련 전문가만 투입한다. test-sentinel의 테스트 동반 갱신 확인을 항상 포함한다
- [ ] `build-verify` 엔진: 검증 사다리(build → mock → 라이브 → docker `--no-deps`)를 따른다

## 영입 후보 (한 번에 1~2명)

- [ ] **web-ingress-guard**: `/api/llm` CORS와 rate limit, SSRF, wireframe XSS, 이미지 경로
- [ ] **graph-sync-specialist**: PG↔Neo4j 정합성(K9, AK6), NL→Cypher 품질
- [ ] prompt-curator, frontend-view-inspector, llm-provider-specialist(랜덤 벡터 폴백), storage-migration-keeper
