이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.

프로젝트 위치및 설명,로컬 테스트방법 을 참고해 지침을 수행해주세요

# 지침 - 메모리 버전 이력 관리

## 기능 개요
메모리 수정시 이전 버전을 보관하고 이력을 관리할 수 있는 기능을 구현합니다.
실수로 중요한 내용이 삭제되거나 변경된 경우 이전 버전으로 복원할 수 있습니다.

## 구현 요구사항
- 메모리 수정시 이전 버전을 별도 테이블에 저장합니다.
  - memory_history 테이블 또는 기존 구조 활용
  - 버전 번호, 수정일시, 수정자 정보 저장
  - 최대 10개 버전 유지 (설정으로 조절 가능)
- MemoryVersionActor 액터 생성
  - 버전 저장 및 조회 처리
  - 비동기로 처리하여 수정 성능에 영향 없음
- API 엔드포인트
  - GET /api/memory/{id}/history - 버전 이력 조회
  - GET /api/memory/{id}/history/{version} - 특정 버전 조회
  - POST /api/memory/{id}/restore/{version} - 특정 버전으로 복원
- UI 개선
  - ui/view/{id} 페이지에 "버전 이력" 버튼 추가
  - 버전 목록 팝업: 각 버전의 수정일시, 변경 요약 표시
  - 버전 비교 보기: 현재 버전과 선택한 버전의 diff 표시
  - "이 버전으로 복원" 버튼

## 프로젝트 위치및 설명
- src/Memorizer/ 하위디렉토리에 있습니다.
- src/Memorizer/Controllers - API컨트롤러가 있습니다.
- src/Memorizer/Services - 검색로직을 포함 서비스로직이 있습니다.
- src/Memorizer/Views - UI관련 뷰파일이 있습니다.
- src/Memorizer/Actors - 액터모델이 있습니다.
- src/Memorizer.IntegrationTests - 통합테스트 프로젝트가 있습니다.

## 로컬 테스트방법
- 빌드오류만 해결합니다.
- 빌드오류 잡고나서 테스트는 직접예정으로 피드백에따라 수정합니다.
