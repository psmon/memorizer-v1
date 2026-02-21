이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.
프로젝트 위치및 설명,로컬 테스트방법 을 참고해 개선지침을 수행해주세요

# 개선지침
- ClaudeCode 라는 대메뉴를 추가합니다. - 기존메뉴구성참고 Shape UP 다음에 (주의:Shape UP의 하위소속아닙니다.)
- 하위기능으로 Claude Skill 만들기 입니다.
  - 스킬 공유된 기능도 볼수있으며 SkillShared 도 추가됩니다.
- ClaudeCode : Claude Code를 활용하는 페이지설명
  - SkillCreate : 스킬생성연습 
  - SkillShared : 생성된 스킬 공유된것을 보는 페이지
- 스킬이해를 위한 참고 : 310c9d5e-dece-4709-a51c-e0e4404660d7 메모라이저 MCP에서 검색후, 연관자료까지 학습합니다.
- 해당기능 인증모드는 인증없이도 실행됩니다. - 기존 인증모드 참고
- SSE를 채택하고 액터모델에 에이전트 기능을 탑제 시도합니다. , LLM은 스트림방식을 적극활용하고  LLM-EX채택


## 스킬만들기 주요기능
- 만들고 싶은 역활이 무엇인가요? 대화형으로 시작합니다.
- 업무직군을 선택하는 버튼이 먼저 제공됩니다. "개발자","기획자","데브옵스","QA',"직접입력" 등 
- 직군을 선택하면 필요로 하는 하위 추천 스킬 제목을 선택제안을 합니다. LLM에의해 동제안 버튼을 생성 +직접입력
- 직군+하위 추천스킬 제목을 선택을 베이스로 이 스킬을 작성하기위한 보강자료를 만들기 위해 추가 질의가 진행됩니다. 
  - 이 과정에서 LLM이 적절한 질문을해 , 사용자에게 올바른 추가정보를 얻기위해 목적달성을 위해 반복합니다.
  - 이 모듈이 가장중요하며 Agent-Actor 방식을 이용합니다.
- 스킬을 작성하기 위한 모든정보를 획득했다고 판단되면 스킬 생성하기가 진행됩니다. - 진행상황표기
- 스킬이 모두 생성되면 대화창에 상세내용을 표현할 필요가 없으며 버튼(링크) 형태로 제공합니다.
- 버튼을 누르면 새로운 창형태로 뜨게됩니다.
- 이 스킬만들기의 목적달성을 위한 프롬프트를 생성합니다.
  - 스킬은 클로드 코드 스킬의 파일형태구조를 표현하고 , SKIIL.md 를 마크다운업 형태로 볼수 있어야 합니다.(복사하기기능지원)
  - 스킬은 공유하기 기능도 있어서 공유하기 누르면 숏링크로 접근해 볼수 있습니다. - 기존 공유하기 기능확인 
- 위 기능의 목적달성을 위해 LLM 기능과 목적달성을 위한 어플리케이션내 작동 프롬프트 설계를 합니다.


## 프로젝트 위치및 설명
- prompt/kr/agent.md 파일을 참고합니다.

## 로컬 테스트방법
- 빌드를 수행해 빌드오류를 수정합니다.
- 추가된 코드는 유닛테슽트를 작성해 수행합니다.
- src/Memorizer/appsettings.Sam.json : 이 환경을 실행시키고 런타임 오류를 개선합니다., 로컬 모드로 실행합니다.
- 개발 db인 postgres는 로컬 docker를 통해 이미 구동되어 있어서 db조회및 파악가능합니다. - 런타임오류 발생및 기능오류 제보기 read로 이용가능
- 정상적으로 구동되면 직접 테스트 예정으로 피드백대기합니다.

# 버그fix 
- 직군을 처음에 한번묻고.. 답변했는데 두번째 동일하게 물음.. 그리고 선택하면 진행됨
- '개발자' 직군에 적합한 스킬을 추천합니다. 원하는 스킬을 선택하거나 직접 입력해주세요. 진행되었는디 선택하면 미진행..다음서버오류발생
```
      => SpanId:e8d2710e2bd55568, TraceId:2ae948e27b32a2358c7ff4c28638784e, ParentId:0000000000000000 => ConnectionId:0HNJHC30ET34U => RequestPath:/api/claudecode/message RequestId:0HNJHC30ET34U:00000021
      Request finished HTTP/1.1 POST http://localhost:5013/api/claudecode/message - 202 - application/json;+charset=utf-8 1.7851ms
[2026-02-22 00:50:34.359] info: Akka.Actor.ActorSystem[0]
      [INFO][02-21-2026 15:50:34.359Z][Thread 0024][akka://Memorizer/user/skill-maker-54cb952b-362d-4991-950f-06af97e76d5c] Message [SkillMakerUserMessage] to [akka://Memorizer/
user/skill-maker-54cb952b-362d-4991-950f-06af97e76d5c#1310332110] was unhandled. [1] dead letters encountered. This logging can be turned off or adjusted with configuration sett
ings 'akka.log-dead-letters' and 'akka.log-dead-letters-during-shutdown'. Message content: SkillMakerUserMessage { SessionId = 54cb952b-362d-4991-950f-06af97e76d5c, Message = API 문서 자동화, MessageType = Category, Timestamp = 2026-02-21 오후 3:50:34 }
[2026-02-22 00:51:10.069] dbug: Microsoft.Extensions.Http.DefaultHttpClientFactory[10
```
- 직군선택 - 카테고리 - 상세정보 수집까지 된듯 하지만 스킬생성직전 어플리케이션 종료된듯(오류)
```
[2026-02-22 01:01:25.645] info: Akka.Actor.ActorSystem[0]
      [INFO][02-21-2026 16:01:25.645Z][Thread 0035][akka://Memorizer/user/skill-maker-543e8c6e-6df9-40a4-963c-9ba3a2df1abb] Enough info gathered for session 543e8c6e-6df9-40a4-963c-9ba3a2df1abb, starting skill generation
Unhandled exception. System.NotSupportedException: There is no active ActorContext, this is most likely due to use of async operations from within this actor.
   at Akka.Actor.ActorBase.get_Context()
   at Akka.Actor.UntypedActor.get_Context()
   at Memorizer.Actors.SkillMakerActor.GenerateFollowUpQuestion() in D:\Code\AI\memorizer-v1\src\Memorizer\Actors\SkillMakerActor.cs:line 352
   at System.Threading.Tasks.Task.<>c.<ThrowAsync>b__128_1(Object state)
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()
[2026-02-22 01:01:25.658] fail: Akka.Actor.ActorSystem[0]
      [ERROR][02-21-2026 16:01:25.646Z][Thread 0035][akka://Memorizer/user/skill-maker-543e8c6e-6df9-40a4-963c-9ba3a2df1abb] Error generating follow-up question for session 543e8c6e-6df9-40a4-963c-9ba3a2df1abb
      Cause: System.NotSupportedException: There is no active ActorContext, this is most likely due to use of async operations from within this actor.
         at Akka.Actor.ActorBase.get_Context()
         at Akka.Actor.UntypedActor.get_Context()
         at Memorizer.Actors.SkillMakerActor.GenerateFollowUpQuestion() in D:\Code\AI\memorizer-v1\src\Memorizer\Actors\SkillMakerActor.cs:line 329
      System.NotSupportedException: There is no active ActorContext, this is most likely due to use of async operations from within this actor.
         at Akka.Actor.ActorBase.get_Context()
         at Akka.Actor.UntypedActor.get_Context()
         at Memorizer.Actors.SkillMakerActor.GenerateFollowUpQuestion() in D:\Code\AI\memorizer-v1\src\Memorizer\Actors\SkillMakerActor.cs:line 329
[2026-02-22 01:01:31.310] dbug: Microsoft.Extensions.Http.DefaultHttpClientFactory[100]
      Starting HttpMessageHandler cleanup cycle with 1 items
[2026-02-22 01:01:31.310] dbug: Microsoft.Extensions.Http.DefaultHttpClientFactory[101]
      Ending HttpMessageHandler cleanup cycle after 0.0092ms - processed: 0 items - remaining: 1 items
프로세스가 종료 코드 -532,462,766(으)로 완료되었습니다.
```
- 잘수행되었음 추가로 제목은.. 완성된 스켈베이스로 공유하기하면 제목 자동생성해죠
- 이 스킬생성시 이 스킬 활용하는 프랙티스.. SKILL.md 를 복사해야하는 경로 가이드도 추가로 
- 스킬 실제 활용 가이드는 스킬이 할수 있는것에서 , /skill-myskill OOO를 수행해 주세요 와 같이 LLM이 개입해 추천생성해주는것으로 
- 공유시 활용가이드(프렉티스)도 표시될수있도록 포함시켜
- 스킬 추가정보획득시 유사한 질문을 하는것같음.. 유사중복질문 안하도록 개선