This project uses dotnet 9.0 and consists of a full-stack (API, UI) architecture.
It supports search and vector search using PostgreSQL, and graph exploration using Neo4j.

Based on prompt/kr/20-AskBotShare.md, we already implemented the feature to share chatbot results.
When sharing, data is stored in the askbot_share_links table.
Follow the instructions below to list this content and add functionality to view it by clicking.

## Instructions
- Add ui/sharelist page.
  - Add a menu called "AI-Knowledges" that navigates to this page when clicked.
  - This page lists all records from the askbot_share_links table.
  - At the bottom, add a concept explanation about LLM regenerating new knowledge using memory knowledge.
  - Clicking opens ui/askbot/share/{shortlink} page in a new window.
  - Add pagination to show 30 items per page.
  - Display records in card format instead of simple table format.
    - Each record includes the following information:
    - Short link (clickable)
    - Created date/time
    - Session ID
    - Conversation summary (summary column)
    - Keep the card design simple and clean.

## Project Location and Description
- Located under src/Memorizer/ subdirectory.
- src/Memorizer/Controllers - Contains API controllers.
- src/Memorizer/Services - Contains service logic including search logic.
- src/Memorizer/Services/ILlmService.cs - Can use LLM and embedding.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Actors - Contains actor models, refer to them when actor models are needed.
  - ChatBotActor.cs - Actor model that connects users via SSE edge to handle conversation requests. Uses search and decision actors to generate responses.
  - SearchMemoryActor.cs - Actor model responsible for memory search.
  - DecisionActor.cs - Actor model that determines if searched memory is relevant to the request.
  - MetadataEmbeddingActor.cs - Actor model responsible for metadata embedding. Processes metadata embedding asynchronously when registering memory. Vectorizes using embedding during this process.
  - GraphsyncActor.cs - Actor model that syncs Postgres data to graph model using Neo4j.
- src/Memorizer.IntegrationTests - Contains integration test project.
- PageUrl: Has the following page URLs:
    - ui/blog
        - ViewMore: View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238: View content by id
    - ui/askbot - Chatbot page
- src/Memorizer.IntegrationTests/Actors: Existing unit tests are available. Refer to unit testing methods.

## Local Testing Method
- Can test locally through docker-compose.local-psmon.yml. Use this file if docker operation issues occur.
- Basic configuration may be running~ Don't down everything, just rebuild and restart only memorizer-app
  - postgmem-postgres has a well-working DB setup and sample data included, so especially don't down this element, treat it carefully as if it were a production DB
- This application is built and operated through memorizer-app. When rebuild/restart is needed, use only docker without dotnet cli
- During test improvements, db schema changes are not allowed, only READ is allowed, and postgres/neo4j work well based on docker, don't stop or down them.

## API Authentication Method for Testing
- Check src/Memorizer/Controllers/AuthController.cs code and proceed through registration API after passing api authentication
- Note that views are unauthenticated, memory registration is only possible through authenticated API, use the cookie value returned after successful login with admin/admin123
