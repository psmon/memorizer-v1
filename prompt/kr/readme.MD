# Claude Code 이용 프로그램업데이트

Fork Origin Version : https://github.com/Aaronontheweb/memorizer-v1

원본 오픈소스에서 출발해 Claude코드를 활용해 기능 업그레이드 시도하는 프로젝트입니다.

## Version 0.0.9

노트 : 기본버전이 임베디드 Ollama를 활용하고 있으며, 임베딩/LLM성능을 높이기위해 OpenAI API를 선택할수 있습니다.
LLM은 비용이 드는 단점이 있어서 개인연구용은 역시 라이트웨이한 온디바이스 Ollama를 활용하는것이 추천합니다.

- [OpenAI-Upgrade](01-OpenAI-Upgrade.MD)


## Version 1.0.0

노트 : 이 버전부터 이 프로젝트가 제공하는 메모리를 함께활용해 필요한 컨텍스트를 할습시킨후 프로그램업데이트 
개선시도되며  GraphDB를 활용해 메모리를 찾을수 있는 고급기능을 추가합니다. 
자연어를 Cypher 쿼리로 LLM으로 변환해 유용한검색이 되게까지 하기위해서는 프롬프트 엔지니어링이 필요할수 있습니다. GraphView를 통해 탐색하는 기본기능을 우선제공합니다.

- [GraphDB-Memory](02-GraphDB-Memory.MD)
- [GraphDB-Upgrade](02-GraphDB-Upgrade.MD)
- [GraphDB-Upgrade-Fix](02-GraphDB-Upgrade-FIX.MD)
- [GraphDB-UX-Upgrade3](03-GraphDB-UX-Upgrade.MD)
- [GraphDB-UX-Upgrade4](04-GraphDB-UX-Upgrade.MD)


## Version 1.0.1

관리기능과 MCP에 인증기능 간소화된 인증기능추가, 메모리 웹탐색은 비인증뷰로 개선 
Public모드로 지식을 공유하고 특정 사용자와 메모리를 함께 관리하고자할때 활용

- [Auth-Upgrade](05-Auth-Upgrade.MD)

이 업데이트 이후  MCP-Memorizer를 구축하고, 닷넷/자바 진영의 AKKA를 메모리에 학습시켜 액터모델도 바이브코딩에 이용하고 있습니다.
컨텐츠는 한글로 작성되어, 영어권에서는 번역이 필요할수도 있습니다.

MCP Memory View - Akka관련 Context를 주로 다룸
- https://mcp.webnori.com/ui


## Version 1.0.2

자연어를 Cypher 쿼리로 변환하는 기능 개선과 쿼리 탐색을 위한 더 나은 UX로 GraphDB 검색 기능 개선

- [GraphDB-Prompt-Upgrade](06-GraphDB-Prompt-Upgrade.MD)

## Version 1.0.3

GraphDB 검색 및 표현 기능 추가 개선, Blog Memories 페이지 추가

- [GraphDB-Prompt-Upgrade2](07-GraphDB-Prompt-Upgrade.MD)
- [View-Blog-Upgrade](08-View-Blog-Upgrade.MD)

## Version 1.0.4

블로그 뷰 UX 및 성능 개선

- [View-Blog-Upgrade2](09-View-Blog-Upgrade2.MD)

## Version 1.0.5

Custom LLM/Embedding 서비스 구현 및 설정 개선

- [CustomLLM-New](10-CustomLLM-New.MD)

## Version 1.0.6

임베딩 차원 변경에 대한 벡터 동기화 버그 수정

- [VectorSync-Fix01](11-VectorSync-Fix01.MD)

## Version 1.0.7

동일 목적이지만 차이가 있는 중복 프롬프트 개선

- [PROMPT-Improve](12.PROMPT-Improve.md)

## Version 1.0.8

마크다운 렌더링 개선을 통한 콘텐츠 뷰 향상

- [ContentView-Upgrade](13-ContentView-Upgrade.MD)

## Version 1.0.9

하이브리드 검색 기능 개선 및 ASKBot 대화형 챗봇 구현

- [HybridSearch-Upgrade](14-HybridSerch-Upgrade.md)
- [ASKBOT-ActorModel](15.01-ASKBOT-ActorModel.md)
- [ASKBOT-SSE](15.02-ASKBOT-Websocket.md)
- [ASKBOT-IMPROVE1](15.03-ASKBOT-IMPROVE1.md)

## Version 1.1.0

메뉴 UI/UX 개선 - 반응형 햄버거 메뉴 및 애니메이션 구현

- [MenuView-Upgrade](16.MenuView-Upgrade.MD)

## Version 1.1.1

ASKBot 대화 컨텍스트 관리 기능 개선 - 세션별 대화 맥락 유지 및 추론 과정 선택적 표시

- [ASKBOT-ActorModel-Upgrade](17.ASKBOT-ActorModel-Upgrade.md)


통합 테스트 개선 - Actor 모델 테스트 업데이트 및 실제 장치 기반 테스트 강화

- [Unitest-Upgrade](18.Unitest-Upgrade.md)


## Claude Cli Setting : Vibe 코딩할시 활용

```
claude mcp add memorizer http://localhost:5012/sse --transport sse --header "X-API-Key: your-api-key-here"

claude mcp list

claude mcp remove memorizer
```

## Claude Desktop Setting : 지식을을 간편하게 메모리에 저장할때 다양한 MCP와 연계해 만들때 활용

```
{
  "mcpServers": {
    "mcp-memory": {
      "command": "npx",
      "args": [
        "-y",
        "mcp-remote",
        "http://localhost:5012/sse",
        "--header",
        "X-API-Key:${MCP_API_KEY}"
      ],
      "env": {
        "MCP_API_KEY": "xxxxxxxxxxxxxxxxxxx"
      }
    }
  }
}
```

### Claude Desktop Tip

활용 프롬프트 : akka 컨텐스트를 Memory에 저장하기, 학습하기 위한 페이지를 찾고 다음과 같은 심플한 요청이면 됩니다.
```
https://getakka.net/ 의 내용을 메모리화해
```

프로젝트에 설정된 지침 :
```
당신은 연결된 MCP, 특히 장기기억 메모리 활용해 답변을 하는 진보된 LLM입니다.

활용 MCP를 참고  다음 Rule을 준수해 작동할것

# Rule

- 검색대상이 활용 MCP에 포함되어 있으면 적합한 MCP를 각각활용할것
- 컨플검색 요청을하면  Atlassian MCP를 활용해 지식을 획득할것
- 메모리에서 검색 요청하면, Memory MCP를 활용해 지식을 획득할것
- 메모리 검색시 유사도는 0.7이상으로 하고, 검색이 안될시 0.4이상으로 시도
- 메모리 저장 여부는 물어보고 저장할것, "메모리 저장해" 와같이 별도 지침없을때 자동저장 금지(중요)
- URL 링크를 분석요청시 리소스접근안될시~  Browser MCP를 후속으로 활용합니다.

# 활용 MCP 종류
- 메모리 MCP
- 컨플/지라 제어가능한 Atlassian MCP
- 브라우저 탐색가능한 Browser MCP
```