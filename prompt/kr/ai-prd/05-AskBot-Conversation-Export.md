이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.

프로젝트 위치및 설명,로컬 테스트방법 을 참고해 지침을 수행해주세요

# 지침 - AskBot 대화 내보내기 기능

## 기능 개요
AskBot의 대화 내용을 다양한 포맷으로 내보내기(Export)할 수 있는 기능을 추가합니다.
현재 공유 기능(숏링크)은 있지만, 파일로 다운로드하는 기능은 없습니다.

## 구현 요구사항
- 대화 내보내기 버튼을 공유 버튼 옆에 추가합니다.
- 지원 포맷
  - Markdown (.md): 대화 내용을 마크다운 형식으로 저장
  - PDF: 대화 내용을 PDF로 변환하여 저장
  - JSON: 대화 메타데이터 포함 원본 데이터 저장
- 내보내기 옵션
  - 파일명 지정 가능 (기본값: 날짜_시간_세션ID)
  - 이미지 포함 여부 선택 (멀티모달 대화시)
- 서버사이드 처리
  - ExportConversationActor 액터 생성
  - PDF 생성을 위한 라이브러리 활용 (예: DinkToPdf 또는 QuestPDF)
- API 엔드포인트
  - POST /api/askbot/export/{sessionId} - 대화 내보내기
  - Body: { format: "md" | "pdf" | "json", includeImages: boolean }

## 프로젝트 위치및 설명
- src/Memorizer/ 하위디렉토리에 있습니다.
- src/Memorizer/Controllers - API컨트롤러가 있습니다.
- src/Memorizer/Views - UI관련 뷰파일이 있습니다.
  - ui/askbot 페이지 수정 필요
- src/Memorizer/Actors - 액터모델이 있습니다.
  - ChatBotActor.cs - 대화 데이터 조회 참고
- src/Memorizer.IntegrationTests - 통합테스트 프로젝트가 있습니다.

## 로컬 테스트방법
- 빌드오류만 해결합니다.
- 빌드오류 잡고나서 테스트는 직접예정으로 피드백에따라 수정합니다.
