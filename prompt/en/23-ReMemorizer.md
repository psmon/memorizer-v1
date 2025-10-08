# ReMemorizer Feature Enhancement

This project uses .NET 9.0 and consists of a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL and graph exploration using Neo4j.

We plan to enhance features based on the following improvement guidelines.

# Improvement Guidelines
- On the UI pages, unauthenticated users can only view, while logged-in users can register/update/delete memories.
  - API permission checks are already implemented, but everything is displayed in the UI. Improve to hide memory register/update/delete buttons when unauthenticated.
- On the ui/askbot/share/{shortlink} page, shared chatbot conversation content can be viewed.
  - This page can already be viewed without authentication, and we will add a feature to register shared conversation content as a memory.
    - Memory registration requires permissions, so authentication is needed.
    - Reference the Store function of MemoryTools to save.
      - After saving, if there are reference documents, they can be connected between memories as relationships.
      - An additional LLM may be needed to perform this function. Write prompts referring to MCP tips and use LLM.


## Project Location and Description
- Located under the src/Memorizer/ subdirectory.
- src/Memorizer/Controllers - Contains API controllers.
- src/Memorizer/Services - Contains service logic including search logic.
- src/Memorizer/Services/ILlmService.cs - Can use LLM and embeddings.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Actors - Contains actor models, refer to when actor models are needed.
  - ChatBotActor.cs - Actor model that handles conversation requests by connecting users via SSE edge. Generates responses using search and decision actors.
  - SearchMemoryActor.cs - Actor model responsible for memory search.
  - DecisionActor.cs - Actor model that determines if searched memories are related to the request.
  - MetadataEmbeddingActor.cs - Actor model responsible for metadata embedding. Asynchronously processes metadata embedding during memory registration. Vectorizes using embedding in this process.
  - GraphsyncActor.cs - Actor model that syncs Postgres data to graph model using Neo4j.
- src/Memorizer.IntegrationTests - Integration test project exists.
- PageUrl : Has the following page URLs.
    - ui : Includes Content View with Edit, Delete functions, and Vector Search is also possible.
    - ui/blog
        - ViewMore : View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238 : View content by id
    - ui/askbot - Chatbot page
    - ui/askbot/share/{shortlink} - Shared chatbot conversation view page
    - ui/sharelist - Shared chatbot conversation list page
- src/Memorizer.IntegrationTests/Actors : Existing unit tests exist. Refer to unit testing methods

## Local Testing Method
- Can test locally through docker-compose.local-psmon.yml. If docker operation issues occur, use this file.
- The basic configuration may be running~ do not bring down everything with the down command, only rebuild and restart memorizer-app
  - postgmem-postgres has a working DB setup and sample data, so especially do not down this component, handle it carefully as if it's a production DB
- This application is built and operated through memorizer-app. When rebuild/restart is needed, please use only docker without dotnet cli
- For actor model unit testing methods, refer to the following, when creating an actor model, write or update unit tests for that actor.
  - Write actor model tests in the src/Memorizer.IntegrationTests/Actors location.
- When API testing is needed, use a Client available in .NET for testing. Use appropriate client modules when SSE or websocket is needed.

## API Authentication Method for Testing
- Check src/Memorizer/Controllers/AuthController.cs code.. proceed through registration API after passing api authentication..
- Note that views are unauthenticated, memory registration is only possible with authenticated API, use the cookie value returned after succeeding with login as admin/admin123
- src/Memorizer/Controllers/MemoryController.cs uses CreateMemory for memory registration ... use the provided API by understanding it rather than manipulating the db
