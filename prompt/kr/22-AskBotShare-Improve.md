
이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능합니다.

ui/askbot 에는 대화형 챗봇이 이미 구현되어 있으며, 대화내용 공유하기 기능도 있습니다.
다음 지침에 의해 공유하기및 공유된 컨텐츠를 보는 기능을 강화 하려고합니다.

# 지침
- 현재 챗봇대화 공유하기를 하면, 내부연관 자료일때 연관자료를 마지막에 표현합니다. 
  - 챗봇이용후 클릭을 하면 모달형태로 view를 할수있으며 공유하기를 누른후 공유된 자료에서는 연관문서가 표현되지 않습니다.
  - 공유하기를 해도 동일하게 연관문서가 표현되도록 개선하며 클릭시 모달형태로 view를 할수있도록 개선합니다.
    - askbot_share_links 테이블에 스키마가에 관련자료를 표현할수 있도록 컬럼을 추가합니다.  
    - 기존작성된 데이터는 없을수 있음으로 데이터없을시 예외가 생기지  않도록 합니다. 


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
    - ui/askbot/share/{숏링크} - 공유된 챗봇 대화내용 보기 페이지
- src/Memorizer.IntegrationTests/Actors : 기존 유닛테스트가 있습니다. 유닛테스트 방법을 참고
  
## 로컬 테스트방법
- docker-compose.local-psmon.yml 를 통해 로컬에서 테스트 가능합니다. 도커구동 문제시 이파일을 사용할것..
- 기본구성이 구동되어 있을수 있으며~ down 명령으로 전체를 내리지말고 memorizer-app 만 재빌드후 재구동할것
  - postgmem-postgres 는 잘작동하는 DB셋팅및 샘플데이터가 포함된 데이터베이스가 있음으로 이 요소는 특히나 down 하지말것, 운영DB인것처럼 조심해서 다룰것  
- 이 어플리케이션은 memorizer-app 를 통해 빌드및 작동됩니다. 재빌드/재구동이 필요할시, dotnet cli없이 도커로만 이용해주세요
- 액터모델 유닛테스트 방법은 다음을 참고, 액터모델을 생성하면 해당액터에대한 유닛테스트를 작성또는 업데이트합니다. 
  - src/Memorizer.IntegrationTests/Actors 위치에 액터모델 테스트를 작성합니다.
- API 테스트가 필요할시 닷넷에서 이용할수 있는 Client를 이용해 테스트합니다. SSE가 필요하거나 웹소켓이 필요한경우 적절한 클라이언트 모듈을 이용합니다.  

## 테스트시 API인증방법
- src/Memorizer/Controllers/AuthController.cs 코드확인후.. api인증을 통과한후 등록 API를 통해 진행..
- 참고로 뷰는 미인증, 메모리등록은 인증된 API로만 가능 로긴시  admin/admin123 로 성공한후 반환된  쿠키값을 사용
- src/Memorizer/Controllers/MemoryController.cs 메모리 등록은 CreateMemory 사용 ... db를 조작하지 말고 제공되는 API를 파악해 활용할것

