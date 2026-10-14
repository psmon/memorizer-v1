# MCP 도구 계약 기준 (MCP Tool Contract)

> 사용 에이전트: [mcp-tool-inspector](../agents/mcp-tool-inspector.md)
> 원천: `.claude/skills/mcp-tools/SKILL.md` "Description 작성 베스트 프랙티스"·"주의사항", MCP(Model Context Protocol) 도구 설계 관행

MCP 도구의 1차 소비자는 LLM이다. 이 문서는 **"LLM이 이 도구를 정확히 한 번에 올바르게 부를 수 있는가"** 를 판단하는 기준이다.

## 현재 도구 인벤토리 (2026-10-14 기준, `MemoryTools.cs`)

| 도구 | 성격 | 주의 |
|------|------|------|
| Store | 쓰기 | `title` 필수, `confidence` 0.0~1.0, `relatedTo`+`relationshipType` 조합 |
| Search | 읽기 | 기본 `limit=10`, `minSimilarity=0.7` |
| Get / GetMany | 읽기 | Guid 형식 오류 처리 |
| Delete | **파괴적** | 되돌릴 수 없음 명시 필요 |
| CreateRelationship | 쓰기 | 허용 관계 타입 목록 노출 필요 |
| SearchGraph | 읽기(LLM→Cypher) | 생성된 Cypher도 읽기 전용 검증 대상 |
| SearchGraphByCypher | 읽기(원문 Cypher) | **읽기 전용 + LIMIT 강제** 필수 |

> 인벤토리는 점검 때마다 코드 기준으로 갱신한다. 이 표가 코드와 다르면 표를 고친다.

## Description 기준 (D1~D5)

| # | 기준 | 위반 예 |
|---|------|--------|
| D1 | **언제 쓰는가**가 첫 문장에 있다 | "Searches memories." (무엇만 있고 언제가 없음) |
| D2 | 모든 파라미터에 Description이 있고, 기본값이 드러난다 | `limit`에 기본값 10 언급 없음 |
| D3 | 문자열 enum의 **허용값 목록**이 명시된다 | `type`에 "e.g."만 있고 전체 목록 없음 |
| D4 | 형제 도구와의 **구분**이 드러난다 | Search vs SearchGraph 선택 기준 부재 |
| D5 | 부작용(쓰기/삭제/외부호출)이 명시된다 | Delete에 비가역성 언급 없음 |

## 반환 포맷 기준 (R1~R3)

- **R1** 성공 응답은 도구 간 동일한 골격(헤더 → 항목 → 후속 제안)을 따른다
- **R2** 실패 응답은 `Error:` 접두로 시작하고, 사용자가 고칠 수 있는 원인만 담는다 (스택·내부 쿼리·커넥션 정보 금지)
- **R3** 결과 0건은 실패가 아니다 — "0건 + 다음 행동 제안"으로 응답한다

## 안전장치 체크리스트

- [ ] Cypher 원문 실행 시 `CREATE|MERGE|SET|DELETE|REMOVE|DROP|CALL dbms` 등 쓰기 절 차단
- [ ] Cypher 결과에 LIMIT 강제 (사용자 LIMIT이 더 크면 상한으로 클램프)
- [ ] 모든 비동기 메서드에 `CancellationToken` 전파
- [ ] ApiKey / OAuth 토큰 검증이 MCP 엔드포인트 전체에 적용
- [ ] 텔레메트리 태그·로그에 비밀값과 메모리 본문 전체를 싣지 않음
