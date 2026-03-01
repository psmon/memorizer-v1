This project uses dotnet 9.0 and is composed as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, graph traversal using Neo4j,
and also includes an ASK conversational bot feature utilizing LLM, with various AI features provided through the MCP interface.
Please follow the improvement instructions by referring to the project location, description, and local testing methods.

# Feature to Add
- ui/askbot: ask bot
  - When making a conversation request, internal memory is searched to attach related content as reference candidates. When no related memory is found, web search knowledge is used as a fallback.
  - When web search is needed, only use the headless-based module (src/Memorizer/Services/IWebSearchService.cs)
    - src/Memorizer/Services/IWebSearchService.cs: This module has been pre-implemented and tested.

## Project Location and Description
- Refer to the prompt/kr/agent.md file.

## Local Testing Method
- Run the build and fix any build errors.
- Write unit tests for the added code and run them. (Existing unit tests should also work and be reinforced)
- src/Memorizer/appsettings.Sam.json: Run this environment and fix any runtime errors, run in local mode.
- The development PostgreSQL database is already running via local Docker, so DB queries and investigation are possible - available for runtime error and functionality error reporting via read.
- Once running normally, direct testing is planned, so wait for feedback.
  - Official deployment is Linux-based via .NET Docker, but local testing will be performed on Windows with feedback pending.

# Additional Instructions
- When web search references are used, add functionality to display referenced page URLs, similar to how memory reference buttons appear.
  - To implement this feature, reference information is stored in DB, so migration may be needed.

- Web references don't seem to display on ui/askbot, /ui/askbot/share/eYD0Ex, both pre-share and shared pages.

- Naver parsing seems to be resolved. Based on search strategy, search all 3 providers (Naver, Bing, Google) and adopt the most relevant search results (by judgment).
  Since there may be parsing issues like Naver, leave txt logging records and proceed to check if all 3 work. Before that, improve to a logic that comprehensively searches and adopts from all 3 search targets.
