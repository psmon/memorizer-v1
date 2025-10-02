이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능합니다.

ui/askbot 에는 대화형 챗봇이 이미 구현되어 있으며 다음 지침에의해 
대화내용을 숏링크로 공유하는 기능을 추가하고자합니다.

# 지침
- 연결됨/접속끊김 상태 UI가 New Session을 가립니다. Session ID를 보여주는 영역에서 심플하게 문자로 표시합니다.
  - 연결됨/접속끊김 상태는 UI에서 쉽게 구분할수 있도록 색상또한 다르게 표시합니다.
- 공유기능은 New Session 버튼 옆에 배치합니다. 
- 공유버튼 클릭시 현재 Session ID의 모든대화기록을 숏링크로 변환해 클립보드에 복사합니다.
  - 숏링크는 ui/askbot/share/{숏링크} 형태로 구성됩니다.
  - 숏링크는 6자리 영문대소문자+숫자 조합으로 구성됩니다.
  - 숏링크는 Postgres DB에 저장되며, Session ID와 매핑됩니다.
  - 쇼링크를 클릭시 해당 Session ID의 모든대화기록을 불러와 챗봇이 대화내용을 모두 보여줍니다.
    - view화면은 챗봇이 진행될 필요는 없으며 markdown을 지원하면서 대화내용을 모두 보여줍니다.


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
- 액터모델 유닛테스트 방법은 다음을 참고, 액터모델을 생성하면 해당액터에대한 유닛테스트를 작성또는 업데이트합니다. 
  - src/Memorizer.IntegrationTests/Actors 위치에 액터모델 테스트를 작성합니다.
- API 테스트가 필요할시 닷넷에서 이용할수 있는 Client를 이용해 테스트합니다. SSE가 필요하거나 웹소켓이 필요한경우 적절한 클라이언트 모듈을 이용합니다.  

## 테스트시 API인증방법
- src/Memorizer/Controllers/AuthController.cs 코드확인후.. api인증을 통과한후 등록 API를 통해 진행..
- 참고로 뷰는 미인증, 메모리등록은 인증된 API로만 가능 로긴시  admin/admin123 로 성공한후 반환된  쿠키값을 사용
- src/Memorizer/Controllers/MemoryController.cs 메모리 등록은 CreateMemory 사용 ... db를 조작하지 말고 제공되는 API를 파악해 활용할것


### 추가지침
- 매번 기록할거 아님.. 쉐어할때만 기록할거임 .. 그래서 askbot_share_links 에 컨텐츠 가 쉐어할때 저장되면됨.. 그리고 쉐어하기 누르면 팝업으로 떠서 링크도 볼수 있게.. 복사하기 버튼이 함께 있으면됨 
- 챗봇의 대화 세션은 브라우저 새로고침해도 동일하게 유지되지만, 대화기록을 복원하지 못함 액터모델이 세션의 대화내용을 기록하고 있기때문에 새로고침이 될때 이전 대화내용을 세션id기반으로 복원해죠
- 챗봇이 생각하는 진행 애니메이션을 개선 ... 이 물결 애니메이션 형태로