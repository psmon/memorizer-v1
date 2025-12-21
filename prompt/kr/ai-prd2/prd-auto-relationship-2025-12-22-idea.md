이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.

프로젝트 위치및 설명,로컬 테스트방법 을 참고해 지침을 수행해주세요

# 지침: 메모리 자동 관계 발견 (Auto-Relationship Discovery)

## 개요
- 현재 메모리 간의 관계(RELATES_TO)는 수동으로만 생성됩니다.
- 기존에 저장된 메모리들 중에서 의미적으로 관련된 메모리들을 자동으로 발견하고 Neo4j에 관계를 생성하는 기능을 구현합니다.
- 이 기능은 기존의 벡터 검색, LLM, 액터모델, 그래프 동기화 기능을 활용합니다.

## 상세 요구사항

### 1. RelationshipDiscoveryActor 액터모델 구현
- 새로운 액터모델 `RelationshipDiscoveryActor`를 구현합니다.
- 이 액터는 백그라운드에서 주기적으로 또는 요청에 따라 실행됩니다.
- 기존 MetadataEmbeddingActor, GraphsyncActor 패턴을 참고합니다.

### 2. 관계 발견 로직
- 대상 메모리 선정:
  - 아직 관계가 없거나 적은 메모리를 우선 처리합니다.
  - 최근 생성된 메모리를 우선 처리합니다.
- 유사 메모리 검색:
  - 벡터 검색을 이용해 대상 메모리와 유사도 0.7 이상인 메모리를 최대 5개 검색합니다.
  - 이미 관계가 있는 메모리는 제외합니다.
- LLM 기반 관계 유형 판단:
  - 발견된 유사 메모리들과의 관계 유형을 LLM을 통해 판단합니다.
  - 관계 유형: extends, supports, related-to, example-of, explains 중 선택
  - 관계가 없다고 판단되면 연결하지 않습니다.
- 관계 생성:
  - 판단된 관계를 Neo4j에 저장합니다.
  - 기존 GraphsyncActor의 관계 생성 로직을 참고합니다.

### 3. API 엔드포인트
- `POST /api/memory/discover-relationships` - 수동으로 관계 발견 실행
  - 선택적 파라미터: memoryId (특정 메모리에 대해서만 실행)
  - 인증 필요
- `GET /api/memory/{id}/suggested-relationships` - 특정 메모리의 추천 관계 조회
  - 아직 생성되지 않은 추천 관계 목록 반환

### 4. UI 연동
- ui/view/{id} 상세 페이지에 "관계 자동 발견" 버튼 추가
  - 인증된 사용자만 표시
  - 클릭 시 해당 메모리에 대한 관계 발견 실행
  - 결과를 팝업으로 표시 (발견된 관계 수, 유형 등)

### 5. 배치 처리 (선택사항)
- 시스템 시작 시 또는 설정된 주기로 전체 메모리에 대해 관계 발견 실행
- 환경변수 `MEMORIZER_AutoRelationship__Enabled` (true/false)
- 환경변수 `MEMORIZER_AutoRelationship__IntervalHours` (기본값: 24)

## 프로젝트 위치및 설명
- src/Memorizer/ 하위디렉토리에 있습니다.
- src/Memorizer/Controllers - API컨트롤러가 있습니다.
- src/Memorizer/Services - 검색로직을 포함 서비스로직이 있습니다.
- src/Memorizer/Services/ILlmService.cs - LLM및 임베딩을 이용할수 있습니다.
- src/Memorizer/Views - UI관련 뷰파일이 있습니다.
- src/Memorizer/Actors - 액터모델이 있으며, 액터모델이 필요시 참고합니다.
  - ChatBotActor.cs - SSE를 엣지로 사용자와 연결해 대화요청을 처리하는 액터모델입니다.
  - SearchMemoryActor.cs - 메모리 검색을 담당하는 액터모델입니다.
  - DecisionActor.cs - 검색된 메모리와 요청내용이 관련성이 있는지 판단하는 액터모델입니다.
  - MetadataEmbeddingActor.cs - 메타데이터 임베딩을 담당하는 액터모델입니다.
  - GraphsyncActor.cs - Neo4j를 이용해 Postgres데이터를 그래프 모델로 싱크하는 액터모델입니다.
- src/Memorizer.IntegrationTests - 통합테스트 프로젝트가 있습니다.
- PageUrl : 다음과 같은 페이지 url을 가지고 있습니다.
    - ui/blog
        - ViewMore : 팝업버튼을 통해 컨텐츠보기
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238 : id별로 컨텐츠보기
    - ui/askbot - 챗봇 페이지
    - ui/askbot/share/{숏링크} - 공유된 챗봇 대화내용 보기 페이지
- src/Memorizer.IntegrationTests/Actors : 기존 유닛테스트가 있습니다. 유닛테스트 방법을 참고

## 로컬 테스트방법
- docker-compose.local-psmon.yml 를 통해 로컬에서 테스트 가능합니다. 도커구동 문제시 이파일을 사용할것..
- 기본구성이 구동되어 있을수 있으며~ down 명령으로 전체를 내리지말고 memorizer-app 만 재빌드후 재구동할것
  - postgmem-postgres 는 잘작동하는 DB셋팅및 샘플데이터가 포함된 데이터베이스가 있음으로 이 요소는 특히나 down 하지말것
- 이 어플리케이션은 memorizer-app 를 통해 빌드및 작동됩니다. 재빌드/재구동이 필요할시, dotnet cli없이 도커로만 이용해주세요
- 액터모델 유닛테스트 방법은 다음을 참고, 액터모델을 생성하면 해당액터에대한 유닛테스트를 작성또는 업데이트합니다.
  - src/Memorizer.IntegrationTests/Actors 위치에 액터모델 테스트를 작성합니다.
- API 테스트가 필요할시 닷넷에서 이용할수 있는 Client를 이용해 테스트합니다.

## 테스트시 API인증방법
- src/Memorizer/Controllers/AuthController.cs 코드확인후.. api인증을 통과한후 등록 API를 통해 진행..
- 참고로 뷰는 미인증, 메모리등록은 인증된 API로만 가능 로긴시 admin/admin123 로 성공한후 반환된 쿠키값을 사용
- src/Memorizer/Controllers/MemoryController.cs 메모리 등록은 CreateMemory 사용

## LLM 프롬프트 예시
관계 유형 판단을 위한 프롬프트:
```
당신은 두 문서 간의 관계를 판단하는 전문가입니다.

[문서 A]
제목: {titleA}
내용: {summaryA}

[문서 B]
제목: {titleB}
내용: {summaryB}

두 문서의 관계를 다음 중 하나로 판단해주세요:
- extends: 문서 B가 문서 A의 개념을 확장/발전시킨 경우
- supports: 문서 B가 문서 A의 주장을 뒷받침하는 경우
- related-to: 두 문서가 관련된 주제를 다루는 경우
- example-of: 문서 B가 문서 A에서 설명한 개념의 예시인 경우
- explains: 문서 B가 문서 A의 내용을 설명하는 경우
- none: 의미있는 관계가 없는 경우

JSON 형식으로 응답:
{"relationship": "관계유형", "confidence": 0.0~1.0, "reason": "판단 이유"}
```
