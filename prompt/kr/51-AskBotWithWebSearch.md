이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.
프로젝트 위치및 설명,로컬 테스트방법 을 참고해 개선지침을 수행해주세요

# 추가하고자하는  기능
- ui/askbot : ask bot 
  - 현재 대화요청시 연관내용을 첨부하기위해 내부 메모리를 검색해 적합한 후보인지 참조하는데.. 연관 메모리가 없을시 폴백으로 웹탐색 지식을 활용
  - 웹탐색이 필요할시 헨드리스(src/Memorizer/Services/IWebSearchService.cs) 기반만 이용할것 
    - src/Memorizer/Services/IWebSearchService.cs : 이 모듈이 사전 구현및 테스트 완료되었습니다.

## 프로젝트 위치및 설명
- prompt/kr/agent.md 파일을 참고합니다.

## 로컬 테스트방법
- 빌드를 수행해 빌드오류를 수정합니다.
- 추가된 코드는 유닛테스트를 작성해 수행합니다. ( 기존 유닛테스트도 작동하고 보강 )
- src/Memorizer/appsettings.Sam.json : 이 환경을 실행시키고 런타임 오류를 개선합니다., 로컬 모드로 실행합니다.
- 개발 db인 postgres는 로컬 docker를 통해 이미 구동되어 있어서 db조회및 파악가능합니다. - 런타임오류 발생및 기능오류 제보기 read로 이용가능
- 정상적으로 구동되면 직접 테스트 예정으로 피드백대기합니다.
  - 공식배포는 닷넷도커를 통해 리눅스기반 배포되지만... 로컬테스트는 윈도우로 수행해 피드백예정입니다.

# 추가지침
- 웹검색 참조인경우.. 메모리지침 참고버튼이 생기듯이 참고한 페이지 URL표시하는기능추가
  - 해당기능을 구현하기위해.. 참조정보는 DB에 저장되 마이그레이션이 필요할수도 있음 

- ui/askbot , /ui/askbot/share/eYD0Ex , 공유전 페이지및 공유된 페이지에서 웹참조가 표시안되는듯


- 네이버 파싱은 해결된듯하고.. 검색전략에의해 3가지 네이버,빙,구글 3개를 검색해 그중 가장 관련성있는 검색결과를 채택(판단)
 네이버처럼 파싱에 문제가 있을수 있으니 txt 로깅 기록남기고 3개가 잘되나 진행예정.. 그전에 검색 대상 3개를 종합 검색해 채택하는 로직으로 개선



다음과 같이 두가지 토픽이 필요한경우.. 메모리에서 하나를 검색
하지만 나머지 주제는 메모리없어서 웹검색이 필요한 상황일텐데.. 현재 두가지이상 토픽이 필요한경우
복합 참조를 안함 플로우를 개선  
```
Processing your request...
Analyzing user query...
Analyzing query to determine search strategy...
Query analysis: 2 topic(s) identified
Reasoning: 사용자가 핵심 기술 결정과 바이브 코딩 회고 두 가지 주제를 언급하고, 이 둘에 대한 국내 커뮤니티 반응을 함께 해석해 분석하도록 요청했습니다.
Using multi-topic search strategy for topics: 핵심 기술 결정 Core Technology Decision, 바이브 코딩 회고 Vibe Coding Retrospective
Found 2 memories across 2 topics.
Topic '핵심 기술 결정 Core Technology Decision': 1 memory/memories found
Topic '바이브 코딩 회고 Vibe Coding Retrospective': 1 memory/memories found
Evaluating relevance of multi-topic search results...
Found 1 relevant memories.
Decision reasoning: 결과 2는 Vibe Coding의 핵심 개념과 아키텍처를 다루고 있어 “핵심 기술 결정”과 “바이브 코딩 회고”에 대한 이해를 돕는 자료가 될 수 있습니다. 국내 커뮤니티 반응 분석은 포함되어 있지 않지만, Vibe Coding에 관한 기본 정보를 제공하므로 부분적으로 유용합니다.
Generating response based on relevant memories...
Successfully generated response using 3 memory/memories.
```

다음은 진행 결과이고
as is : 두가지 토픽중 하나는 메모리 참조
to be : 두가지 토픽중 메모리참조도 하고, 메모리검색안된 나머지주제는 웹검색... 자료채택 flow를 개선(프롬프트및 채택 agent 개선)

```
리액티브 스트림 과 최근 기술동향을 비교해 활용사례
Processing your request...
Analyzing user query...
Analyzing query to determine search strategy...
Query analysis: 2 topic(s) identified
Reasoning: 사용자가 리액티브 스트림과 최신 기술 동향을 서로 비교하고 활용 사례를 찾으려는 두 가지 주제를 요청했습니다.
Using multi-topic search strategy for topics: 리액티브 스트림 Reactive Streams, 최근 기술동향 Recent Technology Trends
Found 2 memories across 2 topics.
Topic '리액티브 스트림 Reactive Streams': 1 memory/memories found
Topic '최근 기술동향 Recent Technology Trends': 1 memory/memories found
Evaluating relevance of multi-topic search results...
Found 1 relevant memories.
Decision reasoning: Result 1은 리액티브 스트림에 대한 정의와 핵심 개념을 제공하므로 사용자의 질문과 직접적으로 관련이 있습니다.
Generating response based on relevant memories...
Successfully generated response using 3 memory/memories.
```

머메이드를 만들때 다음과 같이 오류빈도가 높은데
메머이드 작성시 실수안하도록 프롬프트개선
```
Parse error on line 3:
...bscriber -->|request(n)| PublisherPubli
-----------------------^
Expecting 'SQE', 'DOUBLECIRCLEEND', 'PE', '-)', 'STADIUMEND', 'SUBROUTINEEND', 'PIPE', 'CYLINDEREND', 'DIAMOND_STOP', 'TAGEND', 'TRAPEND', 'INVTRAPEND', 'UNICODE_TEXT', 'TEXT', 'TAGSTART', got 'PS'

graph LR
  Publisher -->|data| Subscriber
  Subscriber -->|request(n)| Publisher
  Publisher --(backpressure)--> Subscriber
```