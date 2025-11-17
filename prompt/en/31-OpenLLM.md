
This project uses dotnet 9.0 and consists of a full-stack (API, UI) implementation.
It supports search and vector search using PostgreSQL, graph traversal using Neo4j,
and also implements an ASK conversational bot feature utilizing LLM, providing various AI capabilities through MCP interface.

Please refer to the project location, description, and local testing methods to perform the instructions.

# Instructions
- Implement LLMController.cs controller to provide pure LLM API access.
  - The LLM API specification is defined in prompt/kr/10-CustomLLM-New.MD. Refer to this to implement the LLMController.cs controller.
    - Use the same API specification.
    - Implement it to be callable without CORS policy.
    - Make the implemented features accessible through Swagger documentation.
    - Implement ChatCompletion to support both completion and streaming.
      - When using streaming functionality, implement it based on SSE.
- After completing the feature implementation, create a guide document for the frontend to utilize this API.
  - Create it with the document name prompt/docs/01-LLM-API-Usage.md.
  - The document should include API usage methods and sample code.

## Project Location and Description
- Located in the src/Memorizer/ subdirectory.
- src/Memorizer/Controllers - Contains API controllers.
- src/Memorizer/Services - Contains service logic including search logic.
- src/Memorizer/Services/ILlmService.cs - Can utilize LLM and embedding.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Actors - Contains actor models, refer to when actor models are needed.
  - ChatBotActor.cs - An actor model that handles conversation requests by connecting users via SSE edge. Generates responses using search and decision actors.
  - SearchMemoryActor.cs - An actor model responsible for memory search.
  - DecisionActor.cs - An actor model that determines whether searched memory is related to the request content.
  - MetadataEmbeddingActor.cs - An actor model responsible for metadata embedding. Processes metadata embedding asynchronously when registering memory. Vectorizes using embedding in this process.
  - GraphsyncActor.cs - An actor model that synchronizes Postgres data to graph model using Neo4j.
- src/Memorizer.IntegrationTests - Contains integration test project.
- PageUrl : Has the following page URLs.
    - ui/blog
        - ViewMore : View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238 : View content by id
    - ui/askbot - Chatbot page
    - ui/askbot/share/{shortlink} - View shared chatbot conversation content page
- src/Memorizer.IntegrationTests/Actors : Contains existing unit tests. Refer to unit testing methods.

## Local Testing Method
- Resolve build errors only.
- After fixing build errors, testing will be done directly and modifications will be made based on feedback.
