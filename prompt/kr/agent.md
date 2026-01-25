
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

## 메모리 검색 기능 (v43 추가, v44-v45 업그레이드)
- PRD Maker 예제 맵핑 토론: 이벤트 스토밍 결과에서 3개 키워드 추출 → 각 키워드별 메모리 검색 3개씩 (0.3 이상 유사도, 최대 9개) → LLM 배치 평가로 상위 3개 선택 → 최대 3개 메모리가 토론에 참여
- ShapeUp Free Board: 프롬프트에서 3개 키워드 추출 → 각 키워드별 메모리 검색 3개씩 → LLM 배치 평가로 상위 3개 선택 → 보드 생성에 참고
- 진행과정 표시: phase SSE 이벤트로 "키워드 추출 중", "메모리 검색 중", "최적 3개 선택 중", "생성 중" 단계 표시
- 토스트 알림: memory_found SSE 이벤트로 "메모리 조각 N개 검색, M개 채택" 메시지 토스트로 표시
- PRD 와이어프레임 ShapeUp 연동: PRD 페이지에서 ShapeUp Free Board로 자동 프롬프트 전달
- 프롬프트 요약: 2000글자 초과 시 자동 요약 후 생성 (핵심 기능, 화면 구성요소, 사용자 흐름 유지)
- 메모리 검색 옵션: 체크박스로 메모리 검색 활성화/비활성화 선택

## SVG BOX 기능 (v44 추가)
- ShapeUp에서 SVG 벡터 이미지 드로잉 지원 (투명 배경, 테두리 색상/두께 조절)
- SVG 아이콘 라이브러리: 화살표, 사용자, 클라우드, 서버, 데이터베이스 등 20종
- SVG Content Editor 모달: 템플릿 선택, SVG path 직접 입력, 실시간 미리보기
- PRD 와이어프레임 ShapeUp 연동: PRD 페이지에서 "ShapeUp으로 와이어프레임 생성" 버튼 클릭 시 ShapeUp Free Board 자동 프롬프트 설정
- JSON 파싱 개선: LLM 응답에서 주석 제거, 다중 추출 전략 (코드블록, 균형 중괄호 등)

## 주요 파일 (v44 관련)
- src/Memorizer/wwwroot/js/shapeup.js - SVG BOX 드로잉, PRD 와이어프레임 모드 처리
- src/Memorizer/wwwroot/js/shapeup-templates.js - JSON 파싱 개선 (extractJsonFromContent, removeJsonComments)
- src/Memorizer/Views/ShapeUpView/_Modals.cshtml - SVG Content Editor 모달
- src/Memorizer/Views/ShapeUpView/_ToolPanel.cshtml - SVG 아이콘 패널
- src/Memorizer/Services/ShapeUpPrompts.cs - SVG path 레퍼런스, 배치 메모리 평가 프롬프트
- src/Memorizer/Services/PrdMakerPrompts.cs - 배치 메모리 평가 프롬프트

## ShapeUp 공유 페이지 개선 (v46 추가)
- 공유 뷰 페이지에서 Edit 버튼 클릭 시 편집 페이지로 이동 및 캔버스 데이터 로드
- 프롬프트 펼치기 UI: 하단에 접기/펼치기 형태로 원본 프롬프트 표시, 복사 기능 제공
- SVG Icons, Board Templates 패널 자동 닫힘 제거 (기본값은 접힘 유지)
- 편집 도구 순서 재배치: Drawing Tools → Properties → SVG Icons → Board Templates
- Ctrl+C/Ctrl+V 복사/붙여넣기 지원 (그룹 유지, 우측 오프셋으로 겹침 방지)
- 편집 도구 패널 독립 스크롤 (캔버스 영역과 높이 일치, overscroll-behavior: contain)

## 주요 파일 (v46 관련)
- src/Memorizer/Views/ShapeUpView/Share.cshtml - Edit 버튼, 프롬프트 펼치기 UI
- src/Memorizer/Views/ShapeUpView/_ToolPanel.cshtml - 편집 도구 순서 재배치, Board Templates 접기/펼치기
- src/Memorizer/wwwroot/js/shapeup.js - checkEditMode(), copySelected(), pasteSelected(), toggleBoardTemplatesPanel()
- src/Memorizer/wwwroot/css/shapeup.css - 편집 도구 패널 독립 스크롤 스타일

## ShapeUp 공유 UX 개선 (v47 추가)
- AI 생성 없이 Share 시 LLM을 통한 제목/설명 자동 생성 (POST /api/shapeup/generate-metadata)
- 캔버스 텍스트 추출 → LLM이 분석하여 title(30자), description(100자) 생성
- 공유 페이지 Pan(이동) 기능: 기본 활성화, 왼쪽 마우스 드래그로 캔버스 이동 가능
- 공유 페이지 손바닥(Pan) 버튼 추가로 이동 모드 토글 가능
- 마우스 휠로 확대/축소 기능 (container 레벨 이벤트 바인딩으로 브라우저 스크롤 방지)
- CSS: touch-action: none, overscroll-behavior: contain 적용

## 주요 파일 (v47 관련)
- src/Memorizer/Controllers/ShapeUpController.cs - GenerateMetadata API, GenerateMetadataRequest/Response 클래스
- src/Memorizer/Views/ShapeUpView/Share.cshtml - Pan 버튼, container 레벨 드래그/휠 이벤트, 마우스 휠 줌
- src/Memorizer/wwwroot/js/shapeup-templates.js - extractCanvasTextForLLM(), shareBoard() LLM 메타데이터 생성

## AskBot LLM-EX 업그레이드 (v48 추가)
- AskBot에서 LLM-EX(고급 모델) 선택 옵션 추가
- ui/askbot 헤더에 LLM-EX 토글 스위치 추가
- ui/askbot/share 메모리 저장 시 LLM-EX 분석 옵션 추가
- UserChatRequest에 UseExtendedModel 플래그 추가
- ChatBotActor에 ILlmExService 지원 추가, 요청별로 LLM/LLM-EX 선택 가능
- SaveMemoryRequest에 UseExtendedModel 플래그 추가
- **추론 과정 LLM-EX 지원**: SearchMemoryActor, DecisionActor에도 ILlmExService 지원 추가
  - 메모리 검색 필요 여부 판단, 쿼리 변환, 키워드 추출, 관련성 평가 등 모든 추론 과정에서 LLM-EX 사용 가능
  - SearchMemoryRequest, AnalyzeQueryTypeRequest, MultiTopicSearchRequest, EvaluateRelevanceRequest에 UseExtendedModel 플래그 추가

## 주요 파일 (v48 관련)
- src/Memorizer/Actors/ChatBotMessages.cs - UserChatRequest, SearchMemoryRequest, AnalyzeQueryTypeRequest, MultiTopicSearchRequest, EvaluateRelevanceRequest에 UseExtendedModel 속성 추가
- src/Memorizer/Actors/ChatBotActor.cs - ILlmExService 주입, CompleteWithLlmAsync 헬퍼 메서드, 하위 액터 요청에 UseExtendedModel 전달
- src/Memorizer/Actors/SearchMemoryActor.cs - ILlmExService 주입, CompleteWithLlmAsync 헬퍼 메서드, 모든 LLM 호출에서 UseExtendedModel 지원
- src/Memorizer/Actors/DecisionActor.cs - ILlmExService 주입, CompleteWithLlmAsync 헬퍼 메서드, 관련성 평가에서 UseExtendedModel 지원
- src/Memorizer/Controllers/AskBotController.cs - ILlmExService 주입, StreamingChatBotActor에 전달, SaveMemoryRequest에 UseExtendedModel
- src/Memorizer/Views/AskBot/Index.cshtml - LLM-EX 토글 스위치 UI
- src/Memorizer/Views/AskBot/Share.cshtml - 메모리 저장 시 LLM-EX 옵션


마지막 자동수정일시분 : 2026-01-25 00:00:00
마지막 버전 반영 : 48