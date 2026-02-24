이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.
프로젝트 위치및 설명,로컬 테스트방법 을 참고해 개선지침을 수행해주세요


# 변경하고자하는 기능
- ClaudeCode : 
  - SkillCreate : 스킬생성연습  

# 개선지침
- 스킬 직무역할 - 적합한 스킬 추천후 추가 설문지(5개) 를 통해 스킬생성이 진행됩니다.
- 추가 설문을 받을때 다음 플랜디자인 원칙에 따라 추가질문을 획득해주세요 (중복질문은 방지)
  - prompt/docs/04-ClaudeCode-PlanningAndDesign.md
- 설문이 진행될때 , 스킬 학습용으로 왜 이러한 질문을 하는가에대한 인사이트와 예시를 추가합니다.


## 프로젝트 위치및 설명
- prompt/kr/agent.md 파일을 참고합니다.

## 로컬 테스트방법
- 빌드를 수행해 빌드오류를 수정합니다.
- 추가된 코드는 유닛테스트를 작성해 수행합니다.
- src/Memorizer/appsettings.Sam.json : 이 환경을 실행시키고 런타임 오류를 개선합니다., 로컬 모드로 실행합니다.
- 개발 db인 postgres는 로컬 docker를 통해 이미 구동되어 있어서 db조회및 파악가능합니다. - 런타임오류 발생및 기능오류 제보기 read로 이용가능
- 정상적으로 구동되면 직접 테스트 예정으로 피드백대기합니다.

## fix
스킬 사용가이드가 다음과같이 표현됨
```
SKILL.md 사용 가이드
아래 내용을 "복사하기" 후 다음 경로에 SKILL.md 파일로 저장하세요.

프로젝트 스킬	your-project/.claude/skills/SKILL.md	팀원과 공유됨 (Git)
개인 글로벌 스킬	~/.claude/skills/SKILL.md	모든 프로젝트에서 사용
저장 후 Claude Code에서 /skill 명령으로 스킬을 호출할 수 있습니다.
```
아래와같이 스킬명 디렉토리에 배치해야 올바른 구조
- your-project/.claude/skills/스킬명/SKILL.md
- ~/.claude/skills/스킬명/SKILL.md
스킬 사용 가이드를 수정해주세요

---

다음도 수정
as is:
-저장 후 Claude Code에서 /skill-스킬명 명령으로 스킬을 호출할 수 있습니다.
to be:
-저장 후 Claude Code에서 /스킬명 명령으로 스킬을 호출할 수 있습니다. -예시를 위한 한글을 영어로 변경해주세요
