NGROK을 이용해 Memorizer Proxy 서버를 외부에 노출하고 싶습니다.
ngrok와 python은 사전에 설치되어 있습니다.]
다음 정보를 이용해 Memorizer Proxy 서버를  구동하고 ngrok를 통해 외부에 노출하는 스크립트(파워쉘)
를 작성해주세요

스크립트및 프로젝트는 src/MemorizerProxy 디렉토리에 생성합니다.
readme.md 파일에는 스크립트 사용방법을 기술합니다.
api 키는 src/MemorizerProxy/.env 에 저장되어 있으며 이 환경을 스크립트에서 로드합니다.


# Example Code to Create MCP Server Instance

```
from agents.mcp import MCPServerStreamableHttp

async def main():
    server = MCPServerStreamableHttp(
        name="MyCustomServer",
        params={"url": "https://mcp.webnori.com/sse", "headers": {"X-API-Key": "your_api_key"}},
    )
    # Define tools and logic here
    await server.run()
```

