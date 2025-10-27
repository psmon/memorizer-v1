이 프로젝트는 RAG기법을 활용한 메모리 저징/검색 기능이 구현되어있으며 MCP를 이용해 활용할수 있습니다.
추가로 액터 모델(Actor Model)을 활용하여 MCP가 없어도 작동하는 독립형 챗봇 에이전트를 구현되어있습니다.

다음 문서 목적을 달성하는 기술문서를 작성하고 싶습니다.

# 문서 목적
- 전체 아키텍처 및 구성요소 설명
  - 어플리케이션 레이아웃 및 아키텍처 설명 
- RAG기반 메모리 검색/저장 기능을 MCP를 통해 활용하는 방법
  - 벡터와 그래프 DB를 활용한 메모리 검색/저장 기능 설명
  - 활용된 프롬프트를 한글화로 설명
- SSE와 액터모델을 연동해 UI에서 중간 스트림처리 실시간 응답이 가능한 챗봇 에이전트 구현 및 활용 방법
  - SSE기반 실시간 응답 구현방법 설명
  - 활용된 프롬프트를 한글화로 설명
  - 챗봇 UI 구현방법 설명
  - 챗봇 API 구현방법 설명
  - 챗봇 컨트롤러 구현방법 설명
  - 챗봇 뷰 구현방법 설명
  - 챗봇 액터 구현방법 설명
  - 챗봇 액터간 통신방식 설명
  - 챗봇 액터와 MCP 연동방법 설명
  - 챗봇 액터와 메모리 검색/저장 액터 연동방법 설명
  - 챗봇 액터와 그래프 DB 연동 액터 연동방법 설명
  - 챗봇 액터와 의사결정 액터 연동방법 설명 
- 액터 모델 기반 챗봇 에이전트 구현 및 활용 방법
  - 에이전트가 협업하기 위해 사용된 프롬프트(한글화된 프롬프트로)를 포함한 액터간 통신방식 설명
  - 액터 모델 기반 챗봇 에이전트 아키텍처 설명
- 추후 개선과제(기능,기술)와 로드맵 제안 

## 문서작성 공통지침

- 핵심 코드 설명 및 참고 위치 안내하기
  - https://github.com/psmon/memorizer-v1 이 이 프로젝트와 연결된 public repo입니다. 이 기준으로 참고위치를 작성해주세요.
- 문서양식은 markdown으로 작성 , 시각화가 필요한 경우 mermaid 다이어그램을 적극 활용할것


# 문서화를 위한 핵심코드 참고위치
- src/Memorizer/Tools/MemoryTools.cs : 메모리 검색 및 저장 기능의 MCP 인터페이스
- src/Memorizer/Prompts/PromptTemplates.cs : 활용된 프롬프트
- src/Memorizer/Actors/ChatBotActor.cs : 액터 모델 기반 챗봇
- src/Memorizer/Actors/SearchMemoryActor.cs - 메모리 검색담당 액터
- src/Memorizer/Actors/DecisionActor.cs - 의사결정 담당 액터
- src/Memorizer/Controllers/ChatBotController.cs : 챗봇 API 컨트롤러
- src/Memorizer/Views/AskBot/Index.cshtml : 챗봇 UI 뷰(SSE기반 실시간 응답)
- src/Memorizer/Actors/GraphRelationshipActor.cs : 그래프 DB 연동 액터

# 생성위치 및 양식 
문서는 한글로 작성하고 다음위치에, 문서를 큰덩어리 하나로 만들지말고 파트별 구분해서 저장
- docs/ko_content/actor-agent/
