This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, graph traversal using Neo4j,
and also implements an ASK conversational bot feature using LLM, providing various AI features through the MCP interface.

Please follow the instructions referring to the project location and description, and local testing methods.

# Instructions
- src/Memorizer/Tools/MemoryTools.cs
  - MCP is located here and handles authentication through the x-api-key (ApiKey) header. We want to add OAuth 2.0 authentication method.
    - Implement standard OAuth Client ID / Secret method to issue and authenticate tokens.
    - OAuth 2.0 authentication method is optional and can be used alongside ApiKey authentication.
    - When OAuth 2.0 authentication method is applied, ApiKey authentication is ignored.
    - OAuth 2.0 authentication method implements both token issuance and token verification.
    - OAuth 2.0 authentication method is implemented using JWT tokens.
    - Token issuance is implemented through the /oauth/token endpoint.
    - Token verification is implemented by including Authorization: Bearer {token} in the header during MCP requests.
    - When implementing OAuth 2.0 authentication method, add OAuth 2.0 settings to src/Memorizer/appsettings.json.
    - Token expiration time is set to 24h and is configurable.
    - This authentication method is needed because Chat-GPT only supports OAuth 2.0 authentication method when calling MCP.
      - Refer to the OAuth method required for ChatGPT MCP connection if needed.

## Project Location and Description
- Located under src/Memorizer/ subdirectory.
- src/Memorizer/Controllers - Contains API controllers.
- src/Memorizer/Services - Contains service logic including search logic.
- src/Memorizer/Services/ILlmService.cs - Can utilize LLM and embeddings.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Actors - Contains actor models, refer to these when actor models are needed.
  - ChatBotActor.cs - Actor model that connects with users through SSE at the edge to process conversation requests. Generates responses using search and decision actors.
  - SearchMemoryActor.cs - Actor model responsible for memory search.
  - DecisionActor.cs - Actor model that determines whether searched memories are relevant to the request content.
  - MetadataEmbeddingActor.cs - Actor model responsible for metadata embedding. Processes metadata embedding asynchronously during memory registration. Vectorizes using embeddings in this process.
  - GraphsyncActor.cs - Actor model that syncs Postgres data to graph model using Neo4j.
- src/Memorizer.IntegrationTests - Contains integration test project.
- PageUrl: Has the following page URLs:
    - ui/blog
        - ViewMore: View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238: View content by id
    - ui/askbot - Chatbot page
    - ui/askbot/share/{shortlink} - Page to view shared chatbot conversation content
- src/Memorizer.IntegrationTests/Actors: Contains existing unit tests. Refer to unit testing methods

## Local Testing Method
- Only fix build errors.
- After fixing build errors, testing will be done directly and modified according to feedback.

### Additional Fix
- Improve error when saving GraphSync during MCP memory storage
  - JSON serialization error occurs
