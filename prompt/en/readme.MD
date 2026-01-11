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

Hybrid search functionality improvement and ASKBot conversational chatbot implementation

- [HybridSearch-Upgrade](14-HybridSerch-Upgrade.md)
- [ASKBOT-ActorModel](15.01-ASKBOT-ActorModel.md)
- [ASKBOT-SSE](15.02-ASKBOT-Websocket.md)
- [ASKBOT-IMPROVE1](15.03-ASKBOT-IMPROVE1.md)

## Version 1.1.0

Menu UI/UX improvements - Responsive hamburger menu and animation implementation

- [MenuView-Upgrade](16.MenuView-Upgrade.MD)

## Version 1.1.1

ASKBot conversation context management improvements - Session-based conversation context retention and selective reasoning process display

- [ASKBOT-ActorModel-Upgrade](17.ASKBOT-ActorModel-Upgrade.md)

Integration test improvements - Actor model test updates and enhanced real device-based testing

- [Unitest-Upgrade](18.Unitest-Upgrade.md)

## Version 1.1.2

Dynamic script injection feature implementation - Custom JavaScript dynamic injection into pages and management UI provided

- [InstallScript](19.InstallScript.md)

## Version 1.1.3

AskBot conversation sharing and restoration features - Short link generation for conversation sharing, conversation snapshots in database, session-based conversation restoration on page refresh, and wave animation improvements for thinking indicator

- [AskBotShare](20-AskBotShare.md)

## Version 1.1.4

AI-Knowledges page implementation - Shared conversation list view with card-based responsive UI, pagination, and knowledge regeneration concept display

- [AskBotShareList](21-AskBotShareList.md)

## Version 1.1.5

AskBot shared conversation referenced documents display improvement - Save and display referenced document information in shared conversations, view document details through modal, ensure backward compatibility with existing data

- [AskBotShare-Improve](22-AskBotShare-Improve.md)

## Version 1.1.6

ReMemorizer feature addition - UI permission control improvements (hide edit/delete buttons when unauthenticated), save shared conversations as memory feature (LLM-based automatic metadata extraction and relationship linking)

- [ReMemorizer](23-ReMemorizer.md)

## Version 1.1.7

ReMemorizer multi-conversation pair storage improvement - Save multiple conversation pairs as individual memories, sequential relationship linking (continues-to), enhanced reference document mapping

- [ReMemorizer-Fix](24-ReMemorizer-fix.md)

## Version 1.1.8

AskBot share feature improvements - Mermaid diagram rendering fixes in shared pages, share update functionality for existing sessions, async/await syntax error resolution

- [ReMemorizer-Fix2](25-ReMemorizer-fix2.md)

## Version 1.1.9

ModelContextProtocol module update from 0.3.0 to 0.4.0

## Version 1.2.0

Integration test improvements - Re-enable and update unit tests missed during rapid feature development

- [Unitest-Improve](26-Unitest-Imporove.md)

## Version 1.2.1

Blog view UX improvements - Direct access buttons for related documents with auto-scrolling animation

- [BlogView-Upgrade](27-BlogView-Upgrade.md)

## Version 1.2.2

Multi-modal image analysis feature implementation - Separate module from ILlmService, OpenAI-compatible API format support (LM Studio), cross-platform PNG generation without external libraries

- [Multi-modal-OCR](28-multi-modal-ocr.MD)

## Version 1.2.3

AskBot multi-modal request feature implementation - Image upload (button/drag-and-drop/clipboard), simultaneous image+text analysis, qwen3-vl-8b model integration, 3MB limit and JPG/PNG format support

- [AskBot-Multimodal](29-AskBot-Multimodal.md)

## Version 1.2.4

AskBot shared image saving feature implementation - Local storage of multi-modal request images, image display in Share links, configurable image storage path (default: AskBotImages)

- [AskBot-SaveImage](30-AskBot-SaveImage.md)

## Version 1.2.5

OpenAI-compatible LLM API controller implementation - Pure LLM API endpoint provision, Chat Completions and Text Completions support, SSE-based streaming implementation, no CORS restrictions, Swagger documentation, no authentication required, frontend integration guide provided

- [OpenLLM](31-OpenLLM.md)

## Version 1.2.6

ASKBOT multi-query search feature implementation - Automatic query type analysis (LLM-based), adaptive search strategy for single/multi topics, independent search per topic and result integration, improved Actor model state management

- [ASKBOT-MultiQA](32-ASKBOT-MultiQA.md)

## Version 1.2.7

Multi-modal LLM service OpenAI extension - Separate MultiModalService into Custom/OpenAI two implementations, add MultiModalOpenAIService based on OpenAI SDK, select implementation by Type in configuration files

- [OpenLLM-fix](33-OpenLLM-fix.md)

## Version 1.2.8

MCP OAuth 2.0 authentication method addition - OAuth 2.0 standard authentication implementation for ChatGPT MCP integration, JWT token issuance based on Client ID/Secret, compatible with existing ApiKey authentication, 24-hour token expiration setting

- [Oauth-MCP](34-Oauth-MCP.md)

## Version 1.2.9

AI-driven idea generation - Auto-generate instructions for new idea implementations based on existing instructions

- [Ai-Driven-Idea](35-Ai-Driven-Idea.md)

## Version 1.3.0

Memory Architecture feature implementation - Combine two memories to generate creative architecture ideas, streaming response, Mermaid diagrams and code samples generation, share functionality, light mode UI, infinite scroll share list

- [Memory-Architecture](36-AskBot2.md)

## Version 1.3.1

PRD Maker feature implementation - Deep analysis based on LLM-EX (120B model), PRD interpretation through Event Storming, Example Mapping discussion with virtual collaborators (PM/Dev/QA/UX), 5-step iterative refinement process (PRD Input → Event Storming → Discussion → Example Mapping → Refined PRD), SSE streaming, Mermaid diagrams, short link sharing

- [PRD-Maker](37-PRD-Maker.md)

## Version 1.3.2

LLM-EX OpenAI support addition - Support for three provider types (Custom, Ollama, OpenAI) in LLM-EX service, environment variable-based provider type selection, OAuth settings integration in sample docker-compose files, docker-compose.md configuration guide documentation

- [LLMEX-OpenAI](38-LLMEX-OpenAI.md)

## Version 1.3.3

PRD Maker feature improvements - Added 6th step Bounded Context (BC) definition feature (DDD-based subdomain decomposition, ubiquitous language conflict analysis, transaction boundary analysis), Memory search integration in step 3 Example Mapping discussion ("Memoriz" virtual discussant with related knowledge participation), Mermaid diagrams included, short link sharing functionality

- [PRD-Maker-02](39-PRD-Maker-02.md)

## Version 1.3.4

PRD Demo Wireframe generation feature - LLM-EX based wireframe HTML page generation from refined PRD, static files saved in wwwroot/demo/{shortlink}/, responsive design with Bootstrap 5 CDN, modal window with full URL copy and open in new window features, also available in PRD Shares page when refined PRD exists

- [PRD-Maker-03](40-PRD-Maker-03.md)

## Version 1.3.5

Shape Up Whiteboard feature implementation - Shape Up methodology-based integrated pitch board (Breadboard, Fat Marker, Problem, Risk, Pitch Summary), AI auto-generation (individual/integrated options), draw.io-like drawing tools, magnetic arrow connections, grouping feature, context menu, PNG/SVG export, short link sharing

- [ShapeUp-Pitch](41-shapeup-pitch.md)
- [ShapeUp-Pitch-Improve](42-shapeup-pitch-improve.md)

## Version 1.3.6

Memory fragment reference feature improvements - Memory search by 3 keywords (0.3+ similarity) in PRD Maker Example Mapping discussion with LLM usefulness evaluation for up to 3 memories (Memory1, Memory2, Memory3) participating in discussion, Memory search integration for ShapeUp Free Board generation, Progress status display (phase SSE events), Toast notifications for memory adoption results

- [Memory-PRD-ShapeUp](43-memory-prd-shapeup.md)

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