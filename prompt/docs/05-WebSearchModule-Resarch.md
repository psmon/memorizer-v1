# Web Search Module 조사 결과 (도입 전 기술 검토)

## 1) 목적과 범위
- 현재 상태: 내부 메모리 검색(벡터/그래프)은 존재, 외부 웹 검색 모듈은 부재
- 목표: 구현 전 채택 기술을 결정하고, 법적/운영 리스크를 낮춘 설계 기준 수립
- 참고 프로젝트 문맥: `prompt/kr/agent.md` (AskBot + Actor 파이프라인)

본 문서는 아래 3가지를 비교한다.
- 웹 `fetch` 기반 검색/수집
- `headless` 브라우저 기반 검색/수집
- 주요 검색 사이트(API/서비스) 활용: 네이버, 구글, 빙, GitHub

---

## 2) 정책/컴플라이언스 원칙 (필수)

### robots.txt 및 이용약관(ToS) 우선 원칙
- 원칙 1: 검색엔진 결과 페이지(SERP) HTML 직접 스크래핑은 기본적으로 지양
- 원칙 2: robots.txt `Disallow` 경로는 수집 금지
- 원칙 3: 사이트별 ToS, API 약관, rate limit 준수
- 원칙 4: `User-Agent` 명시, 요청 빈도 제한, 재시도 백오프 적용
- 원칙 5: 로그인/결제/개인정보 페이지는 수집 제외(allowlist 기반)

### 실무 권장
- 검색은 "공식 API 우선", 페이지 본문 수집은 "robots 허용 URL만 fetch/headless"
- 수집 전 단계에서 `robots` 검사 실패 시 즉시 skip
- 출처 URL/수집시각/수집방법(API|fetch|headless)/정책판정 결과를 로그에 저장

---

## 3) 기술 옵션 비교

## A. 웹 fetch 기반 (HttpClient + HTML 파싱)

### 개요
- 검색 API에서 URL 후보를 받고, 대상 페이지를 서버에서 직접 HTTP 요청(fetch)
- 본문 추출(boilerplate 제거) 후 LLM 요약/재랭킹

### 장점
- 구현/운영 비용이 낮음
- 속도와 동시성 제어가 쉬움
- 인프라 리소스 사용량이 headless 대비 작음

### 단점
- JS 렌더링 의존 페이지에서 본문 추출 실패 가능
- anti-bot, WAF 정책에서 차단 가능
- 사이트별 HTML 구조 편차가 커 파싱 품질 편차 발생

### 적합도
- MVP 1순위 (기본 수집 채널)

---

## B. 헤드리스 기반 (Playwright 등)

### 개요
- 브라우저 엔진으로 렌더링 후 DOM/텍스트 추출
- JS 기반 SPA, 지연 로딩 콘텐츠 대응

### 장점
- fetch 실패 케이스 보완
- 동적 페이지 처리 성공률 높음

### 단점
- CPU/메모리 사용량 큼
- 운영 복잡도 증가(브라우저 바이너리, 타임아웃, 탐지 회피 이슈)
- robots/ToS 위반 리스크가 더 커질 수 있어 운영 정책 강제 필요

### 적합도
- 기본 채널이 아닌 "fallback 전용" 권장
- allowlist 도메인 + 제한된 쿼터로 운영

---

## C. 주요 검색 사이트 활용 (API 중심)

### 공통 원칙
- SERP HTML 크롤링 대신 공식 검색 API 사용
- 각 서비스의 인증/요금/쿼터/사용범위 약관 준수

### 네이버
- Open API 기반 검색(예: 웹문서/블로그/뉴스 등) 사용 검토
- 국내 콘텐츠 커버리지 강점

### 구글
- Programmable Search 계열 API 중심 검토
- 글로벌 문서 커버리지 강점

### 빙
- Bing Web Search API 계열 검토
- 상용 API 안정성과 문서화 장점

### GitHub
- REST/GraphQL Search API (repos/code/issues/discussions) 사용
- 개발/코드 관련 질의에서 고품질 소스 확보 가능

---

## 4) 권장 채택안 (결론)

## 최종 권장: "API 우선 + Fetch 기본 + Headless 제한적 Fallback"

- 1단계: 검색 결과 수집은 공식 API(네이버/구글/빙/GitHub) 우선
- 2단계: 결과 URL 본문은 robots 통과 시 fetch로 수집
- 3단계: fetch 실패/본문 부족 시에만 headless fallback
- 4단계: 내부 메모리 검색과 하이브리드 랭킹(내부 우선 + 외부 보강)

이 조합이 속도/품질/법적 리스크/운영비용의 균형이 가장 좋다.

---

## 5) Memorizer 적용 아키텍처 제안 (Actor 파이프라인 기준)

현재 파이프라인(요약):
- `ChatBotActor` -> `SearchMemoryActor` -> `DecisionActor`

확장 제안:
- `WebSearchActor`: 검색 API 호출, 후보 URL/스니펫 수집
- `WebFetchActor`: robots 검사 + 본문 fetch/정제
- `WebEvidenceRankActor`: 외부 근거 재랭킹/중복제거/신뢰도 점수화
- `SearchMemoryActor`와 병렬 또는 순차 결합 후 `DecisionActor`로 전달

메시지 계약(예시):
- `WebSearchRequest { Query, Locale, MaxResults, Providers }`
- `WebSearchResponse { Items[Title, Url, Snippet, Provider, Score] }`
- `WebFetchRequest { Url, QueryContext }`
- `WebFetchResponse { Url, Content, ExtractedAt, PolicyPassed, Method }`

SSE 표시(기존 phase 이벤트 확장):
- "외부 검색 중"
- "웹 문서 수집 중"
- "근거 재평가 중"

---

## 6) 정책 엔진 설계 포인트
- `robots` 검사 모듈을 fetch/headless 공통 선행 단계로 강제
- 도메인별 `crawl-delay`/rate-limit 정책 테이블 운영
- 차단/오류 누적 시 provider circuit breaker 적용
- 수집 본문 길이/언어/중복률 기준으로 품질 필터링

권장 기본값(초안):
- provider timeout: 5~8초
- fetch timeout: 8~12초
- headless timeout: 15~25초
- query당 외부 URL 수집 상한: 3~5개

---

## 7) 운영/보안 체크리스트
- API 키는 `appsettings` 평문 금지, 시크릿 저장소 사용
- 도메인 allowlist/denylist 적용
- 악성 URL 방지: `http/https` 외 스킴 차단, 내부망 주소 차단(SSRF 방어)
- 다운로드 크기 상한, content-type 검증
- 감사 로그: provider, URL, 정책판정, 응답코드, latency, token usage

---

## 8) 단계별 도입 계획 (현실적 순서)

1. API Provider 추상화 도입
- `IWebSearchProvider` 인터페이스 + Naver/Bing/GitHub 우선 구현
- Google은 비용/쿼터 정책 확인 후 추가

2. Fetch 파이프라인 도입
- robots 검사 -> fetch -> 본문 정제 -> 요약/랭킹

3. AskBot 통합
- `ChatBotActor`에서 외부검색 옵션 플래그 추가
- 내부 메모리 검색 실패/저신뢰 시 외부 검색 트리거

4. Headless fallback 최소 도입
- Playwright 기반, allowlist 도메인만 활성화

5. 관측성/품질 개선
- 성공률, 응답시간, 정책차단율, 사용자 클릭 피드백으로 튜닝

---

## 9) 기술 선택 요약표

| 항목 | Fetch 기반 | Headless 기반 | 검색사이트 API |
|---|---|---|---|
| 구현 난이도 | 낮음~중간 | 중간~높음 | 중간 |
| 운영 비용 | 낮음 | 높음 | 중간(요금/쿼터) |
| 속도 | 빠름 | 느림 | 빠름 |
| 동적 페이지 대응 | 낮음 | 높음 | 해당 없음(결과 제공) |
| 정책 리스크 | 중간 | 중간~높음 | 낮음(공식경로) |
| MVP 적합성 | 높음 | 낮음(보조) | 매우 높음 |

---

## 10) 최종 결정 제안
- 채택: `검색 API + fetch`를 기본 전략으로 도입
- 보류: `headless`는 2차 단계에서 제한적 fallback으로 도입
- 이유: 현재 Memorizer 구조(Actor + SSE + 메모리 검색)에 가장 적은 변경으로 확장 가능하며, robots/ToS 준수와 운영 안정성을 동시에 확보 가능

---

## 11) .NET 모듈 후보군 (기술 채택 핵심)

본 항목은 "유지보수성 + 공식성 + 운영안정성" 기준으로 선정한다.

### A. 즉시 채택 (권장)

1. `Microsoft.Extensions.Http.Resilience` (MS 공식)
- 용도: 외부 검색 API/fetch 호출의 retry, timeout, circuit-breaker, rate limiter
- 채택 이유: .NET 표준 `HttpClientFactory`와 결합이 좋고 운영 안정성 확보에 직접 기여

2. `Microsoft.Playwright` (MS 공식 OSS)
- 용도: JS 렌더링 페이지의 headless fallback 수집
- 채택 이유: .NET 공식 포트, 멀티 브라우저 지원, 생태계/문서 성숙
- 적용 원칙: fallback 전용, allowlist 도메인만 활성화

3. `AngleSharp` (OSS, .NET Foundation 지원)
- 용도: fetch 결과 HTML 파싱/DOM 질의(`querySelector`) 기반 본문 추출
- 채택 이유: HTML5 표준 DOM 지향, 빠른 파싱, 유지보수 활발

4. `Octokit` (GitHub 공식 .NET 클라이언트)
- 용도: GitHub 검색(repos/issues/code 메타) 수집
- 채택 이유: GitHub 공식 지원 SDK, 인증/레이트리밋 대응 구현 용이

### B. 조건부 채택 (보조)

1. `HtmlAgilityPack` (OSS)
- 용도: 비정형/깨진 HTML 파싱 보조
- 조건: AngleSharp 실패 케이스에서만 fallback parser로 사용

2. `SmartReader` (OSS)
- 용도: 기사형 페이지 본문 추출(Readability 계열)
- 조건: 블로그/뉴스형 URL에서만 선택 적용

3. `TurnerSoftware.RobotsExclusionTools` 또는 `InfinityCrawler` (OSS)
- 용도: robots/sitemap 해석 보조
- 조건: 최신 유지보수 빈도 점검 후 도입, 필요 시 RFC 9309 기준 최소 parser 내부 구현 병행

### C. 도입 제외/비권장

1. `Microsoft.Azure.CognitiveServices.Search.WebSearch`
- 사유: NuGet 상 deprecated/legacy 명시, 2025-08-11 기준 obsolete 안내

2. Bing Search SDK 의존 전략
- 사유: Bing Search/Bing Custom Search API는 2025-08-11 은퇴(신규 배포 불가/기존 리소스 비활성화)
- 결론: Bing은 "전용 SDK 채택"이 아닌 "대체 검색 제공자 REST 추상화"로 대응

---

## 12) 검색 제공자별 .NET 구현 전략 (SDK vs REST)

### 네이버
- 권장: 공식 Open API를 `HttpClient`로 직접 호출 (전용 .NET 공식 SDK 부재)
- 비고: 검색 API 일일 호출 한도/인증 헤더 규칙 준수

### 구글
- 권장: Programmable Search JSON API를 REST 우선으로 호출
- 보조: `google-api-dotnet-client`는 유지보수 모드이므로 신규 기능 의존도는 낮게

### 빙
- 권장: 신규 채택 제외 (은퇴 완료)
- 대응: provider 추상화에서 비활성 provider로 분리

### GitHub
- 권장: `Octokit` 우선 + 필요 시 REST/GraphQL 직접 호출 병행

---

## 13) 최종 모듈 채택안 (Memorizer 기준)

필수 채택:
- `Microsoft.Extensions.Http.Resilience`
- `AngleSharp`
- `Microsoft.Playwright` (fallback 전용)
- `Octokit`

선택 채택:
- `HtmlAgilityPack` (파싱 fallback)
- `SmartReader` (기사 본문 추출 특화)

제외:
- `Microsoft.Azure.CognitiveServices.Search.WebSearch` 및 Bing Search API 기반 신규 설계

### 도입 우선순위
1. Resilience + Provider 추상화 + REST(네이버/구글/GitHub)
2. AngleSharp 기반 fetch 본문 추출
3. SmartReader/HtmlAgilityPack 보강
4. Playwright fallback

---

## 14) 참고 근거 링크 (확인일: 2026-03-01)
- Microsoft Playwright for .NET: https://playwright.dev/dotnet/
- Playwright OSS 저장소: https://github.com/microsoft/playwright-dotnet
- Microsoft.Extensions.Http.Resilience 문서: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience
- AngleSharp 저장소: https://github.com/AngleSharp/AngleSharp
- AngleSharp NuGet: https://www.nuget.org/packages/AngleSharp
- HtmlAgilityPack NuGet: https://www.nuget.org/packages/HtmlAgilityPack
- SmartReader NuGet: https://www.nuget.org/packages/SmartReader
- Octokit 저장소: https://github.com/octokit/octokit.net
- GitHub REST Search 문서: https://docs.github.com/en/rest/search
- Naver Search API 문서: https://developers.naver.com/docs/serviceapi/search/
- Google API .NET Client(유지보수 모드 안내): https://github.com/googleapis/google-api-dotnet-client
- Bing Search API 은퇴 공지 (은퇴일 2025-08-11): https://learn.microsoft.com/en-us/javascript/api/overview/azure/cognitiveservices-websearch-readme?view=azure-node-latest
- Microsoft.Azure.CognitiveServices.Search.WebSearch (deprecated 표기): https://www.nuget.org/packages/Microsoft.Azure.CognitiveServices.Search.WebSearch/
