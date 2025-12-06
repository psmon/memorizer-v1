이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.

프로젝트 위치및 설명,로컬 테스트방법 을 참고해 지침을 수행해주세요

# 지침 - Webhook 알림 기능

## 기능 개요
메모리 생성/수정/삭제 등의 이벤트 발생시 외부 시스템에 알림을 보내는 Webhook 기능을 구현합니다.
Slack, Discord, Teams 등의 협업 도구와 연동하거나 자동화 파이프라인에 활용할 수 있습니다.

## 구현 요구사항
- Webhook 설정 관리
  - appsettings.json에 Webhook 설정 추가
  - 여러 개의 Webhook URL 등록 가능
  - 이벤트 유형별 필터링 가능
- 지원 이벤트
  - memory.created: 새 메모리 생성
  - memory.updated: 메모리 수정
  - memory.deleted: 메모리 삭제
  - relation.created: 관계 생성
- WebhookActor 액터 생성
  - 비동기로 Webhook 호출
  - 실패시 최대 3회 재시도 (지수 백오프)
  - 호출 이력 로깅
- Webhook 페이로드 형식
```json
{
  "event": "memory.created",
  "timestamp": "2024-01-01T12:00:00Z",
  "data": {
    "id": "uuid",
    "title": "메모리 제목",
    "type": "reference"
  }
}
```
- 관리 API (인증 필요)
  - GET /api/webhooks - 등록된 Webhook 목록
  - POST /api/webhooks - Webhook 등록
  - DELETE /api/webhooks/{id} - Webhook 삭제
  - POST /api/webhooks/{id}/test - 테스트 호출

## 프로젝트 위치및 설명
- src/Memorizer/ 하위디렉토리에 있습니다.
- src/Memorizer/Controllers - API컨트롤러가 있습니다.
- src/Memorizer/Services - 검색로직을 포함 서비스로직이 있습니다.
- src/Memorizer/Actors - 액터모델이 있습니다.
- src/Memorizer.IntegrationTests - 통합테스트 프로젝트가 있습니다.

## 로컬 테스트방법
- 빌드오류만 해결합니다.
- 빌드오류 잡고나서 테스트는 직접예정으로 피드백에따라 수정합니다.
