이 프로젝트는 dotnet 9.0을 사용하며 풀스택(API,UI) 으로 구성되어 있습니다.
PostgreSQL를 이용한 검색및 벡터검색이 가능하고 Neo4j를 활용 그래프 탐색도 가능하며
추가로 LLM을 활용한 ASK 대화형봇 기능도 구현되어 있으며, MCP 인터페이스로도 다양한 AI기능을 제공합니다.

프로젝트 위치및 설명,로컬 테스트방법 을 참고해 지침을 수행해주세요

# 지침
- 최근 prompt/kr/38-LLMEX-OpenAI.md 활동이 구현되었으며 , LLMEX를 추가로 사용하게 되었습니다.
- 기본 LLM의 경우 CUSTOM을 포함 Ollama, OpenAI등 호환을 지원합니다.
  - CUSTOM은 LM Studio가 제공하는 API스펙과 호환을 맞춘버전입니다. 
  - LLMEX 에서 OpenAI 사용시 API Key가 없다는 오류가 뜨는것같은데 기존 구현된 LLM 인터페이스및 구현체를 참고 설정에따라 지원되게 해주세요
  - LLMEX의 AI 모델은 환경변수를 통한 어플리케이션 로드시 하나만 선택되어 사용됩니다.
    - CUSTOM, OLLAMA, OPENAI 중 하나를 선택할수있도록합니다.
    - 환경변수 설정에따라 어플리케이션 시작시 LLM-EX 설정이 올바르게 로드되고 사용되는지 확인합니다.
- 환경변수 업데이트도 참고 이용 샘플 yml 을 정리해주세요

## 환경변수 업데이트
이 프로젝트는 다양한 LLM을 지원하며 다음 설정 샘플을 참고해 환경변수를 업데이트합니다.
누락된 설정이 있다고하면 함께 추가해주고 불필요한 설정은 제거합니다.  LLM과 관련된 설정이 중요하기 때문에 꼼꼼히 확인합니다.

```
- docker-compose.custom-sample.yml : CUSTOM LLM을 이용합니다.
  - docker-compose.local-psmon.yml : 개인 로컬 테스트용으로 CUSTOM LLM을 이용합니다.  
- docker-compose.ollama.yml : Ollama LLM을 이용합니다.
  - docker-compose.yml : 기본파일로 Ollama LLM을 이용합니다. 
- docker-compose.openai-sample.yml : OpenAI LLM을 이용합니다.
- docker-compose.server.yml : rancher 1.6버전 개인서버 배포로 docker-compose 2버전을 이용하는 서버용 파일입니다.  CUSTOM LLM을 이용합니다.
```
docker-compose.md 에 환경별 이용해야하는 yml 설명서를 작성또는 업데이트합니다.
  
  
## 프로젝트 위치및 설명
- prompt/kr/agent.md 파일을 참고합니다.
  
## 로컬 테스트방법
- 빌드오류만 해결합니다.
- 빌드오류 잡고나서 테스트는 직접예정으로 피드백에따라 수정합니다.
