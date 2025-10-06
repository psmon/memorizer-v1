이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능합니다.

prompt/kr/20-AskBotShare.md 에 의해 챗봇의 결과를 공유하는 기능을 이미 만들었습니다.
공유를 하면 askbot_share_links 테이블에 데이터가 저장됩니다.
다임 지침을 수행해 이 내용을 리스트업하고 클릭스 볼수 있는 기능을 추가합니다.

## 지침
- ui/sharelist 페이지를 추가합니다.
  - AI-Knowledges 라는 메뉴가 추가되어 해당 메뉴버튼을 누르면 연결됩니다.
  - 이 페이지는 askbot_share_links 테이블의 모든 레코드를 리스트업합니다.
  - 하단에는 메모리의 지식을 이용 LLM이 새로운 지식으로 재생성하는 컨셉설명을 추가합니다.
  - 클릭을 하면 ui/askbot/share/{숏링크} 페이지로 새창으로 이동합니다.
  - 페이지네이션을 추가해 한페이지에 30개씩 보여줍니다.
  - 레코드를 표현할때 단순한 테이블형태가 아닌 카드형태로 표현합니다.
    - 각 레코드는 다음 정보를 포함합니다:
    - 숏링크 (클릭가능)
    - 생성일시
    - 세션ID
    - 대화요약 (summary 컬럼)    
    - 카드 디자인은 심플하고 깔끔하게 유지합니다.

## 프로젝트 위치및 설명
- src/Memorizer/ 하위디렉토리에 있습니다.
- src/Memorizer/Controllers - API컨트롤러가 있습니다.
- src/Memorizer/Services - 검색로직을 포함 서비스로직이 있습니다.
- src/Memorizer/Services/ILlmService.cs - LLM및 임베딩을 이용할수 있습니다.  
- src/Memorizer/Views - UI관련 뷰파일이 있습니다.
- src/Memorizer/Actors - 액터모델이 있으며, 액터모델이 필요시 참고합니다. 
  - ChatBotActor.cs - SSE를 엣지로 사용자와 연결해 대화요청을 처리하는 액터모델입니다. 검색,판단 액터를 이용해 응답을 생성합니다.
  - SearchMemoryActor.cs - 메모리 검색을 담당하는 액터모델입니다.
  - DecisionActor.cs - 검색된 메모리와 요청내용이 관련성이 있는지 판단하는 액터모델입니다.
  - MetadataEmbeddingActor.cs - 메타데이터 임베딩을 담당하는 액터모델입니다. 메모리 등록시 메타데이터 임베딩을 비동기로 처리합니다. 이 과정에서 임베딩을 이용 벡터화합니다.
  - GraphsyncActor.cs - Neo4j를 이용해 ,Postgres데이터를 그래프로 모델로 싱크하는 액터모델입니다.
- src/Memorizer.IntegrationTests - 통합테스트 프로젝트가 있습니다.
- PageUrl : 다음과 같은 페이지 url을 가지고 있습니다.
    - ui/blog
        - ViewMore : 팝업버튼을 통해 컨텐츠보기
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238 : id별로 컨텐츠보기
    - ui/askbot - 챗봇 페이지
- src/Memorizer.IntegrationTests/Actors : 기존 유닛테스트가 있습니다. 유닛테스트 방법을 참고
  
## 로컬 테스트방법
- docker-compose.local-psmon.yml 를 통해 로컬에서 테스트 가능합니다. 도커구동 문제시 이파일을 사용할것..
- 기본구성이 구동되어 있을수 있으며~ down 명령으로 전체를 내리지말고 memorizer-app 만 재빌드후 재구동할것
  - postgmem-postgres 는 잘작동하는 DB셋팅및 샘플데이터가 포함된 데이터베이스가 있음으로 이 요소는 특히나 down 하지말것, 운영DB인것처럼 조심해서 다룰것  
- 이 어플리케이션은 memorizer-app 를 통해 빌드및 작동됩니다. 재빌드/재구동이 필요할시, dotnet cli없이 도커로만 이용해주세요
- 테스트가 개선되는동안 db스키마 변경은 허용하지 않으며, READ만 허용하며 postgres,neo4j는 도커기반 잘작동하며 이것을 중단하거나 내리지 않습니다.

## 테스트시 API인증방법
- src/Memorizer/Controllers/AuthController.cs 코드확인후.. api인증을 통과한후 등록 API를 통해 진행..
- 참고로 뷰는 미인증, 메모리등록은 인증된 API로만 가능 로긴시  admin/admin123 로 성공한후 반환된  쿠키값을 사용
