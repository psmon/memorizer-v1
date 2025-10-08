
이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능합니다.

다음 개선지침에 의한 기능보강을 하려고합니다.

# 개선지침
- ui 페이지에서는 비인증은 뷰어만가능, 로긴시 메모리 등록/수정/삭제가 가능합니다.
  - API권한 체크는 이미 구현되어 있지만 , UI에서 모두 표시가됩니다. 비인증일때 메모리 등록/수정/삭제 버튼이 보이지 않도록 개선합니다. 
- ui/askbot/share/{숏링크} 페이지에서는 공유된 챗봇 대화내용을 볼수 있습니다. 
  - 이 페이지는 인증없이 이미 볼수있으며 공유된 대화내용을 메모리에 등록할수 있는 기능을 추가합니다. 
    - 메모리 등록은 권한이 필요하므로 인증이 필요합니다. 
    - MemoryTools 의 Store 기능을 참고해 저장합니다. 
      - 저장을한후 참고문서가 있다고하면 relation으로 메모리간 연결할수 도 있습니다.
      - 이 기능을 수행하기위해 추가 LLM이 필요할수 있습니다. MCP팁을 참고해 프롬프트를 작성하고 LLM을 이용합니다.


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
    - ui : 컨텐츠 View를 포함 Edit,Delete 기능도 있으며 ,Vector Search도 가능합니다.
    - ui/blog
        - ViewMore : 팝업버튼을 통해 컨텐츠보기
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238 : id별로 컨텐츠보기
    - ui/askbot - 챗봇 페이지
    - ui/askbot/share/{숏링크} - 공유된 챗봇 대화내용 보기 페이지
    - ui/sharelist - 공유된 챗봇 대화내용 리스트 페이지
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

