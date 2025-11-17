
이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.

프로젝트 위치및 설명,로컬 테스트방법 을 참고해 지침을 수행해주세요

# 지침
- LLM API를 순수하게 이용할수 있는 LLMController.cs 컨트롤러를 구현합니다.
  - prompt/kr/10-CustomLLM-New.MD에 호출가능한 LLM API 스펙이 정의되어 있습니다. 이를 참고해 LLMController.cs 컨트롤러를 구현합니다.
    - API 활용 스펙을 동일하게 합니다.
    - CORS 정책없이 호출가능하도록 구현합니다.
    - Swagger 문서를 통해 구현기능을 이용할수 있도록합니다.
    - ChatCompletion에 완료및 스트리밍을 이용할수 있게 구현합니다.
      - 스트리밍 기능 사용시 SSE기반으로 구현합니다. 
- 기능 작성이 다되고나면 , 이 API를 활용할수 있도록 프론트를위한 가이드문서를 작성합니다. 
  - prompt/docs/01-LLM-API-Usage.md 문서명으로 작성합니다.
  - 문서에는 API 활용방법과 샘플코드가 포함되어야합니다.
  
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
- 빌드오류만 해결합니다.
- 빌드오류 잡고나서 테스트는 직접예정으로 피드백에따라 수정합니다.
