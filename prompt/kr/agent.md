
# Memorizer 프로젝트 구조 안내

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
    - ui/sharelist - 공유된 챗봇 대화 목록 페이지
    - ui/architecture - 메모리 아키텍처 생성 페이지
    - ui/architecture/share/{숏링크} - 공유된 아키텍처 보기 페이지
    - ui/architecture/shares - 공유된 아키텍처 목록 페이지
    - ui/prd - PRD Maker 페이지 (이벤트 스토밍 + 예제 맵핑)
    - ui/prd/share/{숏링크} - 공유된 PRD 분석 보기 페이지
    - ui/prd/shares - 공유된 PRD 분석 목록 페이지
- src/Memorizer/Services/ILlmExService.cs - 심층분석용 LLM-EX 서비스 인터페이스
- src/Memorizer/Services/CustomLlmExService.cs - LLM-EX Custom 구현체 (LM Studio 호환)
- src/Memorizer/Services/OllamaLlmExService.cs - LLM-EX Ollama 구현체
- src/Memorizer/Services/OpenAILlmExService.cs - LLM-EX OpenAI 구현체
- src/Memorizer/Settings/LlmExSettings.cs - LLM-EX 설정 (Type으로 Custom/Ollama/OpenAI 선택)
- src/Memorizer/Services/PrdMakerPrompts.cs - PRD Maker 프롬프트 (이벤트스토밍, 예제맵핑)
- src/Memorizer/Views/ShapeUpView/ - Shape Up 화이트보드 뷰 (Fabric.js 기반 드로잉)
- src/Memorizer/Controllers/ShapeUpController.cs - Shape Up 보드 컨트롤러 (공유, 저장, AI 생성)
- docker-compose.md - 환경별 docker-compose 설정 가이드
- src/Memorizer.IntegrationTests/Actors : 기존 유닛테스트가 있습니다. 유닛테스트 방법을 참고
- PageUrl (추가):
    - ui/shapeup - Shape Up 화이트보드 페이지
    - ui/shapeup/share/{숏링크} - 공유된 Shape Up 보드 보기 페이지
    - ui/shapeup/shares - 공유된 Shape Up 보드 목록 페이지


마지막 자동수정일시분 : 2026-01-11 00:00:00
마지막 버전 반영 : 42