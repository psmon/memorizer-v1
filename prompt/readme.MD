# Claude Code 이용 프로그램업데이트

원본버전 : https://github.com/Aaronontheweb/memorizer-v1

원본 오픈소스에서 출발해 Claude코드를 활용해 기능 업그레이드 시도하는 프로젝트입니다.

## Version 1.0.0

- [OpenAI-Upgrade](01-OpenAI-Upgrade.MD)

노트 : 기본버전이 임베디드 Ollama를 활용 임베딩/LLM성능을 높이기위해 OpenAI API설정시 호환되도록 업데이트


## Version 1.0.1

- [GraphDB-Memory](02-GraphDB-Memory.MD)
- [GraphDB-Upgrade](02-GraphDB-Upgrade.MD)


노트 : 이 버전부터 이 프로젝트가 제공하는 메모리를 함께활용해, 프로그램업데이트 개선시도되며 GraphDB를 활용해 메모리를 찾을수 있는 고급기능을 추가합니다.


## claude cli

```
claude mcp add --transport sse memorizer https://mcp.webnori.com/sse

claude mcp add --transport sse memorizer https://mcp.webnori.com/sse \
  --header "X-API-Key: xxxxxxxxxxxxxxxxxxx"    

claude mcp list
```

```
claude mcp remove memorizer

claude mcp add --transport sse memorizer http://localhost:5012/sse
```

## claude Desktop

```
{
  "mcpServers": {
    "mcp-memory": {
      "command": "npx",
      "args": [
        "-y",
        "mcp-remote",
        "https://mcp.webnori.com/sse",
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

