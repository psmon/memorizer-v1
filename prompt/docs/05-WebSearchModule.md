# 웹 검색 모듈 검토

내부 메모리 검색기능은 있으나, 외부 웹검색모듈은 없는상태로
구현도입전 기술을 채택하려고합니다.
- 현재 구현 프로젝트 참고 : prompt/kr/agent.md

다음 문서에 조사 결과문서를 정리하고자함
- prompt/docs/05-WebSearchModule-Resarch.md

## 문서작성
크롤 수집 방지(robot.txt) 정책으 고려
- 웹 fetch 기반 검색
- 헤드리스 기반 검색 : 
- 주요검색 사이트 활용 (네이버,구글,빙,github )
- 추가로 웹검색을 잘하는 닷넷모듈중 적합한 후보군도 채택해줄래? 오픈소스또는 MS공식 오픈소스등 채택 - 기술채택으로 가장중요


## 문서기반 모듈작성

우선 다음 경로 하위에 웹탐색 검색 기능을 모듈만 만들어줄래?
- src/Memorizer/Services/....

### 다음을 지원하고 유닛테스트 만들고 잘작동하는지 검증
검색 지원 : 구글,빙,네이버

- 구글에서 "맛집" 검색후 결과 내용확인 가능
- 빙에서 "맛집" 검색후 결과 내용확인 가능
- 네이버에서 "맛집" 검색후 결과 내용확인 가능
- 특정 페이지 열람후 확인도 가능 : "https://section.blog.naver.com/BlogHome.naver?directoryNo=0&currentPage=1&groupId=0"

- Headless는 Chromium 을 닷넷어플리케이션이 시작할때 설치없을시 설치할수 있나? 
- 설치경로 환경설정 : 추후 도커의 별도 파일로 분리예정
- OS맞게 자동설치 고려해줄래? ( 윈도우 와 리눅스(주로우분투)를 구분 )


## test

- 도커 구동을 통한 테슽트 검즘 (크로미움자동설치 확인을위해 필요, 호출가능한 헤드리스 모드를 검증)
  - docker-compose.local-psmon.yml 이 파일에 적용하고
  - postgres,noo4j 는 이미 구동중이여서 내리거나 지우지말고 app만 업데이트
  - 도커로 구동후(리눅스 wsl내 작동됨)... 헤드리스모드로 검색결과 획득가능한지 API 테스트

```
• Ran docker compose -f docker-compose.local-psmon.yml up -d --build --no-deps memorizer
  └ #0 building with "default" instance using docker driver

    … +105 lines
     Container memorizer-app  Starting
     Container memorizer-app  Started

• Ran docker logs --tail 400 memorizer-app
  └ [2026-03-01 05:59:09.197] info: Startup-Debug[0]
          === APPLICATION STARTUP DEBUG ===
    … +126 lines
    [2026-03-01 05:59:10.144] info: Memorizer.Services.InitializationService[0]
          System memory already exists, skipping creation

• Ran sleep 8; docker logs --tail 200 memorizer-app
  └ [2026-03-01 05:59:09.197] info: Startup-Debug[0]
```

진행요원 : codex
