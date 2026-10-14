# MCP 도구 계약 기준 (MCP Tool Contract)

> 사용 에이전트: [mcp-tool-inspector](../agents/mcp-tool-inspector.md)
> 원천: `.claude/skills/mcp-tools/SKILL.md` "Description 작성 베스트 프랙티스"·"주의사항", MCP 도구 설계 관행,
> 하네스 도입 전 이력 `prompt/kr/02~07, 14, 31, 34`, `prompt/docs/01, 05-Research` — **2026-10-14 현행 코드 대조 후 선별 도입** ([project-history](project-history.md))

MCP 도구의 1차 소비자는 LLM이다. 이 문서는 **"LLM이 이 도구를 정확히 한 번에 올바르게 부를 수 있는가"** 를 판단하는 기준이다.

## 현재 도구 인벤토리 (2026-10-14 기준, `MemoryTools.cs`)

| 도구 | 성격 | 주의 |
|------|------|------|
| Store | 쓰기 | `title` 필수(Memory.cs에서 검증), `confidence` 0.0~1.0 **범위 검증 없음**, `relatedTo`+`relationshipType` 조합 |
| Search | 읽기 | 기본 `limit=10`, `minSimilarity=0.7` |
| Get / GetMany | 읽기 | Guid 형식 오류 처리 |
| Delete | **파괴적** | 되돌릴 수 없음 명시 필요. Neo4j 노드는 함께 지워지지 않음 (graph 정합성 공백) |
| CreateRelationship | 쓰기 | 허용 관계 타입 목록 노출 필요 |
| SearchGraph | 읽기(LLM→Cypher) | 생성된 Cypher는 키워드 검사 없이 read 트랜잭션에만 의존 |
| SearchGraphByCypher | 읽기(원문 Cypher) | **읽기 전용 + LIMIT 강제** 필수 — 아래 알려진 결함 참조 |

> 인벤토리는 점검 때마다 코드 기준으로 갱신한다. 이 표가 코드와 다르면 표를 고친다.

## Description 기준 (D1~D5)

| # | 기준 | 위반 예 |
|---|------|--------|
| D1 | **무엇 + 언제 쓰는가**가 첫 문장에 있다 | "Searches memories." (무엇만 있고 언제가 없음) |
| D2 | 모든 파라미터에 Description이 있고, 기본값이 드러난다 | `limit`에 기본값 10 언급 없음 |
| D3 | 문자열 enum의 **허용값 목록**이 명시된다 | `type`에 "e.g."만 있고 전체 목록 없음 |
| D4 | 형제 도구와의 **구분**이 드러난다 | Search vs SearchGraph 선택 기준 부재 |
| D5 | 부작용(쓰기/삭제/외부호출)이 명시된다 | Delete에 비가역성 언급 없음 |
| D6 | **Description 안의 예시가 실제로 통과한다** | 예시 Cypher가 도구 자신의 검증에 막힘 (아래 K1) |

## 반환 포맷 기준 (R1~R3)

- **R1** 성공 응답은 도구 간 동일한 골격(헤더 → 항목 → 후속 제안)을 따른다
- **R2** 실패 응답은 `Error:` 접두로 시작하고, 사용자가 고칠 수 있는 원인만 담는다 (스택·내부 쿼리·커넥션 정보·`ex.Message` 원문 금지)
- **R3** 결과 0건은 실패가 아니다 — "0건 + 다음 행동 제안"으로 응답한다

## 안전장치 체크리스트

- [ ] Cypher 원문 실행 시 쓰기 절 차단 — **토큰(단어 경계) 단위**로 검사. 부분 문자열 검사는 오탐(`createdAt`⊃CREATE, `OFFSET`⊃SET)
- [ ] `CALL` 프로시저(`CALL dbms.*`, `apoc.*` 쓰기 계열) 차단
- [ ] Cypher 결과에 LIMIT 강제 + 상한 클램프 (사용자 LIMIT이 크면 상한으로)
- [ ] 키워드 검사가 MCP 도구뿐 아니라 **같은 Cypher를 받는 HTTP 경로**(`/api/graph/search/cypher`, NL→Cypher)에도 적용
- [ ] 모든 비동기 메서드에 `CancellationToken` 전파
- [ ] ApiKey / OAuth 토큰 검증이 MCP 엔드포인트 전체에 적용, 미설정 시 **기동 거부**(기본 키 폴백 금지)
- [ ] 텔레메트리 태그·로그에 비밀값(잘못 입력된 키 포함)과 메모리 본문 전체를 싣지 않음
- [ ] 인증 우회 공개 경로 목록이 **한 곳**에서 관리됨 (현재 미들웨어에 2벌)

## 인증 모델 (현행, 2026-10-14)

- 우선순위: **ApiKey(헤더 `X-API-Key`, 쿼리 `?apikey=`) → Bearer(JWT)** — `AuthenticationMiddleware.cs:40-88`
  - ❌ 폐기된 이력: "OAuth 사용 시 ApiKey 무시" (prompt 34) — 현재 코드와 다름
- OAuth: `/oauth/token` client_credentials, JWT 만료 기본 24h (`OAuthController.cs`, `OAuthTokenService.cs`)
- 관리자: 세션 쿠키. 비로그인 사용자의 Edit/Delete API 차단은 반영됨 (`AuthenticationMiddleware.cs:266-280`)
- `/api/askbot/*`는 미들웨어 공개 경로 → **쓰기 엔드포인트는 컨트롤러가 직접 인증 확인**해야 한다 (`AskBotController.cs:965-968` 패턴)

## 알려진 결함 레지스트리 (2026-10-14 코드 검증, ✅ 현행 유효)

점검 시 **먼저 이 목록의 해소 여부를 확인**하고, 해소되면 🔒 회귀 가드로 옮긴다.

| ID | 결함 | 심각도 | 근거 | 출처 |
|----|------|-------|------|------|
| K1 | Cypher 쓰기 검사가 부분 문자열 매칭 → Description 예시 `ORDER BY m.createdAt`가 스스로 거부됨. `CALL` 미차단 | Moderate | `MemoryTools.cs:735-744` (예시 :710) | prompt 14, docs/05-Research §7 |
| K2 | ~~`POST /api/graph/sync` 무인증~~ (v1.4.8 해소: 로그인 필요) + FullSync 폴백 경로가 `MATCH (n) DETACH DELETE n` 실행 | Moderate (인증 후 남은 위험) | `AuthenticationMiddleware.cs:170,195`, `GraphSyncService.cs:86-90` | prompt 04, 06 |
| K3 | `/api/graph/search/cypher` 무인증 POST, 키워드 검사 없음(read 세션에만 의존) | Moderate | `AuthenticationMiddleware.cs:169`, `GraphController.cs:339-351`, `GraphRepository.cs:75-91` | prompt 06 |
| K4 | ApiKey 미설정 시 `"default-api-key"` 폴백, `==` 비교 | **Critical** | `AuthenticationService.cs:38-39` | prompt 05 |
| K5 | 잘못된 ApiKey 값을 평문 로그 | Moderate | `AuthenticationMiddleware.cs:55` | prompt 34 |
| K6 | 예시 OAuth SecretKey/ClientSecret이 `Enabled:true`로 커밋, 기동 시 차단 없음 | Moderate | `appsettings.json:59-67` | prompt 34 |
| K7 | OAuth PKCE `plain` 허용, redirect_uri 없으면 인가코드를 응답 본문으로 반환 | Moderate | `OAuthTokenService.cs:273`, `OAuthController.cs:215` | prompt 34 |
| K8 | Store에 `confidence` 범위 검증·try/catch 없음 | Info | `MemoryTools.cs:30-59` | prompt 02, 11 |
| K9 | Delete/Update가 Neo4j에 반영되지 않음 (추가만 동기화) | Moderate | `Memory.cs` 저장소 Delete/Update 경로에 graph 호출 없음 | prompt 02~04 반복 |

> 범위 밖이지만 함께 발견된 웹 진입점 결함(`/api/llm` 익명·CORS `*`·rate limit 없음, SSRF, wireframe XSS, 이미지 경로 검증)은
> [project-history](project-history.md) "영입 후보 — web-ingress-guard"에 기록. 전문가 영입 전까지는 이 검수관이 Info로만 언급한다.

## 회귀 가드 (🔒 이미 반영 — 다시 깨지지 않는지만 확인)

- 비인증 사용자 Edit/Delete 차단 (`AuthenticationMiddleware.cs:266-280`)
- OpenAI 구현체: ApiKey 비어 있으면 예외 (`OpenAILlmExService.cs:29`, `MultiModalOpenAIService.cs:26`)
- Store `title` 필수 검증 (`Memory.cs:192`)
- `/ui/tools/*` 쓰기와 `POST /api/graph/sync`는 로그인 필요 — `AuthenticationMiddleware.IsAdminOperation` (v1.4.8, `AuthenticationMiddlewareTests`). `"/ui/"`가 접두사 공개라 새 관리 POST를 `/ui/` 아래 만들 때 이 규칙에 포함되는지 확인
