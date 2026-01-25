이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.
프로젝트 위치및 설명,로컬 테스트방법 을 참고해 개선지침을 수행해주세요

# 개선지침
이 프로젝트는 AI를 활용하기위해

LLM과 LLM-EX 두가지 설정된 모델을 이용합니다.  유사하지만 LLM은 저용량모델, LLM-EX는
고급모델을 사용하며 전략에 맞게 활용합니다.
다음 기능영역 이 LLM을 이용하며 LLM-EX를 이용할수 있게 코드개선해

## 업그레이드가 필요한 기능영역
- ui/askbot
  - 여기서 LLM이 사용되는 전체
- ui/askbot/share
  - 메모리저장기능이 있습니다.

# 참고

## 프로젝트 위치및 설명
- prompt/kr/agent.md 파일을 참고합니다.

## 로컬 테스트방법
- 빌드및 실행을 수행하지 않습니다.테스트는 직접예정으로 피드백에따라 수정합니다.

---
# 완료 (v48)

## 구현 내용

### 1단계: 응답 생성 LLM-EX 지원
1. **UserChatRequest** - `UseExtendedModel` 플래그 추가
2. **ChatBotActor** - `ILlmExService` 주입 지원, `CompleteWithLlmAsync` 헬퍼 메서드로 LLM/LLM-EX 선택
3. **AskBotController** - `ILlmExService` 주입, `StreamingChatBotActor`에 전달
4. **SaveMemoryRequest** - `UseExtendedModel` 플래그 추가
5. **ui/askbot** - 헤더에 LLM-EX 토글 스위치 추가
6. **ui/askbot/share** - 메모리 저장 시 LLM-EX 분석 옵션 추가

### 2단계: 추론 과정 LLM-EX 지원 (피드백 반영)
7. **SearchMemoryActor** - `ILlmExService` 주입 지원
   - `DetermineIfSearchNeeded`: 메모리 검색 필요 여부 판단
   - `TransformQuery`: 쿼리 변환
   - `ExtractKeywords`: 키워드 추출
8. **DecisionActor** - `ILlmExService` 주입 지원
   - `EvaluateRelevance`: 검색 결과 관련성 평가
9. **메시지 타입에 UseExtendedModel 플래그 추가**:
   - `SearchMemoryRequest`
   - `AnalyzeQueryTypeRequest`
   - `MultiTopicSearchRequest`
   - `EvaluateRelevanceRequest`
10. **ChatBotActor** - 하위 액터 요청 시 `UseExtendedModel` 전달

## 수정된 파일
- src/Memorizer/Actors/ChatBotMessages.cs
- src/Memorizer/Actors/ChatBotActor.cs
- src/Memorizer/Actors/SearchMemoryActor.cs
- src/Memorizer/Actors/DecisionActor.cs
- src/Memorizer/Controllers/AskBotController.cs
- src/Memorizer/Views/AskBot/Index.cshtml
- src/Memorizer/Views/AskBot/Share.cshtml
- prompt/kr/agent.md