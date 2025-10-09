
This project uses .NET 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, and graph traversal using Neo4j.

The following features have been recently added and are working well:
- The ui/askbot/share/{shortlink} page displays shared chatbot conversation content.
  - This page can be viewed without authentication and allows registering shared conversation content into memory.
    - Memory registration requires permissions, so authentication is needed.
    - It uses the Store function of MemoryTools for storage.
      - After storage, if there are referenced documents, they can be linked between memories using relations.
      - LLM is used to perform this function.

We are planning to enhance functionality according to the following improvement guidelines.

# Improvement Guidelines
- The Save As Memory feature for shared chatbot conversations works well and will be improved as follows:
  - Currently, only one conversation/response pair is saved to memory, and a BadRequest error occurs when there are two pairs.
     - Improve to save to memory even when there are multiple conversation pairs.
     - Do not save all at once to memory, but save each conversation pair separately.
     - After all memories are added, interconnect these memories.

## Project Location and Description
- Located in the src/Memorizer/ subdirectory.
- src/Memorizer/Controllers - Contains API controllers.
- src/Memorizer/Services - Contains service logic including search logic.
- src/Memorizer/Services/ILlmService.cs - Can use LLM and embeddings.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Actors - Contains actor models, refer to when actor models are needed.
  - ChatBotActor.cs - Actor model that connects users via SSE edge and processes conversation requests. Generates responses using search and decision actors.
  - SearchMemoryActor.cs - Actor model responsible for memory search.
  - DecisionActor.cs - Actor model that determines whether searched memories are relevant to the request.
  - MetadataEmbeddingActor.cs - Actor model responsible for metadata embedding. Processes metadata embedding asynchronously when registering memory. Vectorizes using embeddings in this process.
  - GraphsyncActor.cs - Actor model that syncs Postgres data to graph model using Neo4j.
- src/Memorizer.IntegrationTests - Contains integration test projects.
- PageUrl: Has the following page URLs:
    - ui: Includes content View with Edit, Delete functions, and Vector Search is also available.
    - ui/blog
        - ViewMore: View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238: View content by ID
    - ui/askbot - Chatbot page
    - ui/askbot/share/{shortlink} - Shared chatbot conversation view page
    - ui/sharelist - Shared chatbot conversation list page
- src/Memorizer.IntegrationTests/Actors: Existing unit tests are available. Refer to unit testing methods.

## Local Testing Method
- Can test locally through docker-compose.local-psmon.yml. Use this file if there are docker operation issues.
- The basic configuration may be running - do not bring down everything with the down command, only rebuild and restart memorizer-app
  - postgmem-postgres has a well-functioning DB setup and sample data, so especially do not down this component, treat it carefully as if it were a production DB
- This application is built and operated through memorizer-app. When rebuild/restart is needed, use only docker without dotnet cli
- For actor model unit testing methods, refer to the following. When creating an actor model, write or update unit tests for that actor.
  - Write actor model tests in src/Memorizer.IntegrationTests/Actors location.
- When API testing is needed, test using a Client available in .NET. When SSE or WebSocket is needed, use appropriate client modules.

## API Authentication Method for Testing
- After checking src/Memorizer/Controllers/AuthController.cs code, proceed through registration API after passing API authentication.
- Note that views are unauthenticated, memory registration is only possible through authenticated API. After successful login with admin/admin123, use the returned cookie value
- Memory registration in src/Memorizer/Controllers/MemoryController.cs uses CreateMemory... Do not manipulate the DB, understand and use the provided API

