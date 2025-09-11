# Program Update Using Claude Code

Fork Origin Version : https://github.com/Aaronontheweb/memorizer-v1

This project starts from the original open source and attempts feature upgrades using Claude Code.

## Version 0.0.9

Note: The base version uses embedded Ollama, and you can choose OpenAI API to improve embedding/LLM performance.
Since LLM incurs costs, using lightweight on-device Ollama is recommended for personal research.

- [OpenAI-Upgrade](01-OpenAI-Upgrade.MD)


## Version 1.0.0

Note: From this version, program updates are attempted by utilizing the memory provided by this project together to load necessary context,
and advanced features are added to find memory using GraphDB.
Prompt engineering may be required to convert natural language to Cypher queries with LLM for useful searches. Basic functionality for exploring through GraphView is provided first.

- [GraphDB-Memory](02-GraphDB-Memory.MD)
- [GraphDB-Upgrade](02-GraphDB-Upgrade.MD)
- [GraphDB-Upgrade-Fix](02-GraphDB-Upgrade-FIX.MD)
- [GraphDB-Upgrade3](03-GraphDB-UX-Upgrade.MD)
- [GraphDB-Upgrade4](04-GraphDB-UX-Upgrade.MD)



## Version 1.0.1

Add simplified authentication features for management functions and MCP, improve memory web exploration to non-authenticated view
Use when sharing knowledge in Public mode and wanting to manage memory together with specific users

- [Auth-Upgrade](05-Auth-Upgrade.MD)

After this update, I set up MCP-Memorizer and started training the AKKA framework (from the .NET/Java ecosystem) into memory, so that the Actor Model can also be used for VibeCoding.
The content is written in Korean, so translation may be necessary for English-speaking users.

MCP Memory View – Mainly covers Akka-related context

https://mcp.webnori.com/ui


## Version 1.0.2

Improve GraphDB search functionality with enhanced natural language to Cypher query conversion and better UX for query exploration

- [GraphDB-Prompt-Upgrade](06-GraphDB-Prompt-Upgrade.MD)

## Version 1.0.3

Further improvements to GraphDB search and presentation features, and addition of Blog Memories page

- [GraphDB-Prompt-Upgrade2](07-GraphDB-Prompt-Upgrade.MD)
- [View-Blog-Upgrade](08-View-Blog-Upgrade.MD)

## Version 1.0.4

Blog view UX and performance improvements

- [View-Blog-Upgrade2](09-View-Blog-Upgrade2.MD)

## Version 1.0.5

Custom LLM/Embedding service implementation and configuration improvements

- [CustomLLM-New](10-CustomLLM-New.MD)

## Version 1.0.6

Vector synchronization bug fix for embedding dimension changes

- [VectorSync-Fix01](11-VectorSync-Fix01.MD)

## Version 1.0.7

Prompt improvement for duplicate prompts with same purpose but differences

- [PROMPT-Improve](12.PROMPT-Improve.md)

## Version 1.0.8

Content view enhancement with markdown rendering improvements

- [ContentView-Upgrade](13-ContentView-Upgrade.MD)

## Version 1.0.9

MCP functionality improvement and hybrid search upgrade

- [HybridSearch-Upgrade](14-HybridSerch-Upgrade.md)


## Claude Cli Setting: Use for Vibe Coding

```
claude mcp add memorizer http://localhost:5012/sse --transport sse --header "X-API-Key: your-api-key-here"

claude mcp list

claude mcp remove memorizer
```

## Claude Desktop Setting: Use when easily storing knowledge in memory and creating in conjunction with various MCPs

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

Usage prompt: Find pages to store and learn akka context in Memory, a simple request like the following will do.
```
Make the content of https://getakka.net/ into memory
```

Instructions set in the project:
```
You are an advanced LLM that answers using connected MCP, especially long-term memory.

Operate according to the following Rules referring to the MCP used

# Rule

- If the search target is included in the MCP used, use each appropriate MCP
- When requesting a Confluence search, acquire knowledge using Atlassian MCP
- When requesting a search from memory, acquire knowledge using Memory MCP
- When searching memory, set similarity to 0.7 or higher, if not found try with 0.4 or higher
- Ask whether to save memory and save it, do not auto-save without separate instructions like "save to memory" (important)
- When analyzing URL links and resources cannot be accessed~ subsequently use Browser MCP

# Types of MCP Used
- Memory MCP
- Atlassian MCP capable of controlling Confluence/Jira
- Browser MCP capable of browser exploration
```