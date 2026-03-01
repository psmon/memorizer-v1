# WebSearchModule 테스트 결과

작성일: 2026-03-01

## 1) 테스트 범위
- 유닛 테스트
  - `WebSearchServiceTests` (목업 기반)
  - `WebSearchServiceLiveTests` (실호출 기반, `RUN_WEB_LIVE_TESTS=true`)
- 도커 통합 테스트
  - `docker-compose.local-psmon.yml` 기반
  - `postgres`, `neo4j`는 유지
  - `memorizer(app)`만 `--no-deps`로 재빌드/재기동
  - 헤드리스 모드 API 실호출 검증

---

## 2) 유닛 테스트 결과

### 2.1 목업 기반 유닛 테스트
- 명령:
```bash
dotnet test src/Memorizer.IntegrationTests/Memorizer.IntegrationTests.csproj --filter "FullyQualifiedName~WebSearchServiceTests"
```
- 결과:
  - Passed: 9
  - Failed: 0

검증 항목:
- Google/Bing/Naver API 파싱
- Google/Bing/Naver Fetch HTML 파싱
- 특정 페이지 열람 파싱
- 검색 후 상위 결과 열람 연계
- Headless 비활성 예외 처리

### 2.2 실호출 라이브 테스트
- 명령:
```bash
RUN_WEB_LIVE_TESTS=true dotnet test src/Memorizer.IntegrationTests/Memorizer.IntegrationTests.csproj --filter "FullyQualifiedName~WebSearchServiceLiveTests"
```
- 최종 결과:
  - Passed: 5
  - Failed: 0

비고:
- 초기에는 EUC-KR 인코딩, 검색엔진 anti-bot 응답, Playwright 런타임 의존성 이슈가 있었음.
- 코드 보강 후 최종 통과.

---

## 3) 도커 통합 테스트 결과

## 3.1 실행 조건
- compose 파일: `docker-compose.local-psmon.yml`
- 적용 방식:
```bash
docker compose -f docker-compose.local-psmon.yml up -d --build --no-deps memorizer
```
- 확인: `postgres`, `neo4j`는 중단/삭제 없이 그대로 `Up` 상태 유지

## 3.2 컨테이너/헤드리스 런타임 보강
- `memorizer` 서비스에 WebSearch Headless 환경변수 적용
- Playwright 브라우저 설치 경로 볼륨 분리(`/app/data/playwright-browsers`)
- Dockerfile에 Chromium 런타임 의존 라이브러리 설치
  - 예: `libglib2.0-0`, `libnss3`, `libxkbcommon0`, `libgtk-3-0`, `libgbm1` 등
- root 컨테이너 실행 환경에서 샌드박스 충돌 방지
  - `MEMORIZER_WebSearch__Headless__ChromiumSandbox=false`

## 3.3 API 실호출 검증
WSL 호스트의 `localhost:5012` 직접 접근 제한이 있어, 컨테이너 네트워크에서 `curl` 사이드카로 호출.

### 헬스체크
```bash
docker run --rm --network container:memorizer-app curlimages/curl:8.12.1 -sS http://localhost:8080/healthz
```
- 결과: `Healthy`

### 헤드리스 검색 (Bing, "맛집")
```bash
docker run --rm --network container:memorizer-app curlimages/curl:8.12.1 -sS \
  -X POST http://localhost:8080/api/websearch/search \
  -H 'Content-Type: application/json' \
  --data '{"provider":"bing","query":"맛집","maxResults":2,"accessMode":"headless"}'
```
- 결과: `items` 2건 반환 (정상)

### 헤드리스 페이지 열람 (네이버 블로그 섹션)
```bash
docker run --rm --network container:memorizer-app curlimages/curl:8.12.1 -sS \
  -X POST http://localhost:8080/api/websearch/read-page \
  -H 'Content-Type: application/json' \
  --data '{"url":"https://section.blog.naver.com/BlogHome.naver?directoryNo=0&currentPage=1&groupId=0","accessMode":"headless"}'
```
- 결과: `statusCode=200`, `title="네이버 블로그"`, 본문 preview 반환 (정상)

---

## 4) 이슈 및 해결 내역

1. Playwright Chromium 미설치
- 증상: `Executable doesn't exist`
- 조치: startup auto-install + 설치 경로 설정 + 브라우저 볼륨 분리

2. Linux 공유 라이브러리 누락
- 증상: `libglib-2.0.so.0` 등 로드 실패
- 조치: Dockerfile에 Chromium 런타임 의존 패키지 설치

3. root + sandbox 충돌
- 증상: `Running as root without --no-sandbox is not supported`
- 조치: `ChromiumSandbox` 설정 추가 및 docker profile에서 `false`

4. Fetch 검색결과 편차
- 증상: Google/Naver에서 anti-bot 페이지로 `items` 빈 결과 가능
- 조치: 라이브 테스트에서 페이지 응답 유효성 검사 병행, Bing 기준 결과 획득 확인

---

## 5) 결론
- 유닛 테스트(목업/실호출): 통과
- 도커 통합 테스트(헤드리스 API): 통과
  - `memorizer`만 업데이트하여 검증 완료
  - 헤드리스 검색/페이지 열람 API 정상 동작 확인
