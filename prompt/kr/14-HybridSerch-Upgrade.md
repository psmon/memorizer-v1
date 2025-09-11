이 프로젝트는 닷넷9 로 개발되었으며, MCP를 이용해 메모리를 검색/저장/수정/삭제 하는기능등을 제공하며
추가로 웹 뷰어를 통해 메로리를 살펴보거나 관리도 할수 있는 기능을 제공합니다.

다음 지침에 의해 MCP기능을 개선하려고 합니다.

## 개선지침 
- CreateGraphQueryPrompt 에는 자연어를 Cypher 쿼리로 변환하는데 도움되는 프롬프트가 포함되어 있습니다.
- MCP를 이용하는 SearchGraph 에서는 자연어를 Cypher 쿼리로 변환하는 기능을 제공합니다. 
  - LLM이 더 정확한 Cypher 쿼리를 생성할수 있도록 CreateGraphQueryPrompt 내용을 파악한후 MCP를 위한 Description 을 업데이트
- 추가로 LLM이 Cypher 쿼리를 직접호출할수 있도록 SearchGraphByCypher 기능을 추가
  - CreateGraphQueryPrompt 내용을 파악한후 SearchGraphByCypher 올바른 사용을 위한 MCP를 위한 Description 업데이트
- CreateGraphQueryPrompt 에 이용된 프롬프트는 수정하지 말아주세요 
- 프로젝트 위치및 설명을 참고하고, 코드개선이 완료되면 로컬 테스트방법을 숙지해 진행해주세요
- 개선완료후  MCP-GUIDE.MD  에 MemoryTools를 분석해 MCP기능을 활용하는방법과 활용프롬프트도 소개해주세요, 그리고 이 내용을 메모리에 저장

## 프로젝트 위치및 설명
- src/Memorizer/ 하위디렉토리에 있습니다.
- src/Memorizer/Views - UI관련 뷰파일이 있습니다.
- src/Memorizer/Tools/MemoryTools.cs Mcp기능은 여기 정의되어있으며 LLM을 위한 활용가이드는 Description 에 정의되어 있습니다. 
- PageUrl : 다음과 같은 페이지 url을 가지고 있습니다.
  - ui/blog
    - ViewMore : 팝업버튼을 통해 컨텐츠보기
  - ui/view/2b61629b-d5b4-41c2-8163-6670893df238 : id별로 컨텐츠보기


## 로컬 테스트방법
- docker-compose.local-psmon.yml 를 통해 로컬에서 테스트 가능합니다. 코드를 수정후 작동을 확인할때 이용합니다.
- 이 어플리케이션은 momorizer 를 통해 빌드및 작동됩니다. 재빌드/재구동이 필요할시, dotnet cli없이 도커컴포즈방식을 이용해주세요
- 테스트가 개선되는동안 db스키마 변경은 허용하지 않습니다.
  - postgres 스키마파악및 읽기만 활용합니다. postgres는 다시구동필요없으며 개선되면 조회만 합니다.
  - 데이터 쓰기가 필요할시 db직접 insert가 아닌 api를 통해서만 시도합니다.
- 인증은 다음과 같은 환경설정에 따라 진행할수 있으며, 조회api는 인증없이도 가능 ,등록/수정/삭제 api는 인증을위해 LOGIN을 이용
  - mcp는 sse 를 이용하며 X-API-Key 헤더를 통해 인증해야 사용가능합니다.

### 인증정보 
docker-compose.local-psmon.yml 로컬환경에서만 유효합니다.
```
  MEMORIZER_Server__UserName: admin
  MEMORIZER_Server__Password: admin123
  MEMORIZER_Server__ApiKey: your-api-key-here
```

### 추가개선지침
- ui/graph에서  View Content 클릭하면 나오는 영역도 markdown view하는 영역으로, ui/blog ViewMore 를 누르면 작동하는 view모드와 동일하게 개선
