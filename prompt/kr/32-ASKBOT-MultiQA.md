이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.

프로젝트 위치및 설명,로컬 테스트방법 을 참고해 지침을 수행해주세요

# 지침
- ASKBOT을 개선하려고합니다. ASKBOT의 기존기능은 prompt/kr/15.01-ASKBOT-ActorModel.md 의 요구사항이 구현되어 확인할수 있습니다.
- 질문에 유사문서 3개문서까지 찾고 참고 하는 기능이 있습니다. 검색-연관주제인지 추론과정
  - 질문의 유형에 필요한 유형의 문서종류가 하나인지? 두개인지? 를 LLM을 통해 먼저구분합니다. 목적에 맞는 프롬프트 작성할것 
    - 한개일때 : 기존과 동일하며 연관성높은  3개의 문서 참고
      - ex> A를 검색해 요약해주세요 해주세요 
    - 두개일때 : 필요한 문서 유형이 A,B 두가지 유형일때.. 연관성이 있는 A-1개 , B-1개를 두개 문서참고
      - ex> A와 B를 검색해 공통점을 찾아주세요
    - 세개이상일때 : A,B,C (C까지 최대3개제한).. 각각 연관성 있는 문서 1개씩 최대 3개문서참고
  - 연관성이 없는 경우 기존과 동일하게 검색문서(메모리)가 아닌 참고없이 응답 

  
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
