This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
Search and vector search are available using PostgreSQL, graph exploration is possible using Neo4j,
and an ASK conversational bot feature using LLM is also implemented, providing various AI features through the MCP interface.

Please follow the instructions by referring to the project location and description, and local testing methods.

# Instructions
- A CUSTOM multi-modal dedicated LLM service is implemented in src/Memorizer/Services/MultiModalService.cs.
  - We want to additionally implement and extend the OPENAI LLM API.
  - src/Memorizer/Services/OpenAILlmService.cs: Refer to this file to implement the OpenAI multi-modal dedicated LLM service.
    - Only implement the parts corresponding to the interface.
    - Separate the implementations as follows:
      - MultiModalCustomService: Already implemented, refer to existing implementation
      - MultiModalOpenAIService: Newly implement with OpenAI compatible spec
  - Add OpenAI multi-modal LLM settings to the configuration files.
    - src/Memorizer/appsettings.json: Add OpenAI multi-modal LLM settings
      - Note that an API key is required for OpenAI
    - src/Memorizer/appsettings.Sam.json: Add Custom multi-modal LLM settings
      - Custom works without an API key.


## Project Location and Description
- Located under src/Memorizer/ subdirectory.
- src/Memorizer/Controllers - Contains API controllers.
- src/Memorizer/Services - Contains service logic including search logic.
- src/Memorizer/Services/ILlmService.cs - Can use LLM and embedding.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Actors - Contains actor models, refer when actor models are needed.
  - ChatBotActor.cs - Actor model that connects with users via SSE at the edge to process conversation requests. Generates responses using search and decision actors.
  - SearchMemoryActor.cs - Actor model responsible for memory search.
  - DecisionActor.cs - Actor model that determines whether the searched memory is relevant to the request content.
  - MetadataEmbeddingActor.cs - Actor model responsible for metadata embedding. Processes metadata embedding asynchronously when registering memory. Vectorizes using embedding in this process.
  - GraphsyncActor.cs - Actor model that syncs Postgres data to graph model using Neo4j.
- src/Memorizer.IntegrationTests - Integration test project.
- PageUrl: Has the following page URLs:
    - ui/blog
        - ViewMore: View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238: View content by id
    - ui/askbot - Chatbot page
    - ui/askbot/share/{shortlink} - Shared chatbot conversation view page
- src/Memorizer.IntegrationTests/Actors: Contains existing unit tests. Refer for unit testing methods.

## Local Testing Method
- Only resolve build errors.
- After fixing build errors, testing will be done directly and modified according to feedback.
