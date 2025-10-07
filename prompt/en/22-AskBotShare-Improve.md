
This project uses dotnet 9.0 and is configured as a full-stack application (API, UI).
It supports search and vector search using PostgreSQL, and graph exploration using Neo4j.

The ui/askbot already has a conversational chatbot implemented, along with a conversation sharing feature.
We want to enhance the sharing functionality and the viewing of shared content according to the following instructions.

# Instructions
- Currently, when sharing a chatbot conversation, related materials are displayed at the end when there are internal references.
  - After using the chatbot, clicking allows viewing in a modal format, but after sharing, the related documents are not displayed in the shared content.
  - Improve so that related documents are displayed the same way even when shared, and can be viewed in a modal format when clicked.
    - Add a column to the askbot_share_links table schema to display related materials.
    - Since there may be no existing data, ensure no exceptions occur when data is absent.


## Project Location and Description
- Located in the src/Memorizer/ subdirectory.
- src/Memorizer/Controllers - Contains API controllers.
- src/Memorizer/Services - Contains service logic including search logic.
- src/Memorizer/Services/ILlmService.cs - Provides LLM and embedding capabilities.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Actors - Contains actor models, refer to them when actor models are needed.
  - ChatBotActor.cs - An actor model that connects with users via SSE and processes conversation requests. Uses search and decision actors to generate responses.
  - SearchMemoryActor.cs - An actor model responsible for memory search.
  - DecisionActor.cs - An actor model that determines if the retrieved memory is relevant to the request.
  - MetadataEmbeddingActor.cs - An actor model responsible for metadata embedding. Processes metadata embedding asynchronously when registering memory, vectorizing using embeddings in the process.
  - GraphsyncActor.cs - An actor model that syncs Postgres data to graph model using Neo4j.
- src/Memorizer.IntegrationTests - Contains integration test project.
- PageUrl : Has the following page URLs:
    - ui/blog
        - ViewMore : View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238 : View content by id
    - ui/askbot - Chatbot page
    - ui/askbot/share/{shortlink} - Shared chatbot conversation view page
- src/Memorizer.IntegrationTests/Actors : Existing unit tests are available. Refer to unit test methods.

## Local Testing Method
- Can be tested locally through docker-compose.local-psmon.yml. Use this file if docker operation has issues.
- The basic configuration may be running~ Don't bring down everything with the down command, only rebuild and restart memorizer-app
  - postgmem-postgres has a working DB setup and database with sample data, so especially don't bring down this component, treat it like a production DB
- This application is built and run through memorizer-app. When rebuild/restart is needed, use only docker without dotnet cli
- For actor model unit testing methods, refer to the following. When creating an actor model, write or update unit tests for that actor.
  - Write actor model tests in src/Memorizer.IntegrationTests/Actors location.
- When API testing is needed, test using a Client available in dotnet. When SSE or websocket is needed, use appropriate client modules.

## API Authentication Method for Testing
- After checking src/Memorizer/Controllers/AuthController.cs code, proceed through the registration API after passing API authentication.
- Note that views are unauthenticated, memory registration is only possible through authenticated API. When logging in, use admin/admin123 to succeed and use the returned cookie value
- For memory registration in src/Memorizer/Controllers/MemoryController.cs, use CreateMemory... Don't manipulate the db directly, understand and utilize the provided API

