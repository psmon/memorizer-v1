This project uses .NET 9.0 and consists of a full-stack (API, UI) architecture.
It provides search and vector search capabilities using PostgreSQL, and graph traversal using Neo4j.

A conversational chatbot is already implemented in ui/askbot, and following these instructions,
we will add a feature to share conversation content via short links.

# Instructions
- The Connected/Disconnected status UI overlaps the New Session button. Display it simply as text in the Session ID display area.
  - The Connected/Disconnected status should be easily distinguishable with different colors in the UI.
- Place the share button next to the New Session button.
- When the share button is clicked, convert all conversation history of the current Session ID to a short link and copy it to the clipboard.
  - The short link is formatted as ui/askbot/share/{shortlink}.
  - The short link consists of a 6-character combination of uppercase/lowercase letters + numbers.
  - The short link is stored in the Postgres DB and mapped to the Session ID.
  - When the short link is clicked, it retrieves all conversation history of that Session ID and the chatbot displays all conversation content.
    - The view screen doesn't need chatbot progression, but should support markdown while displaying all conversation content.


## Project Location and Description
- Located in the src/Memorizer/ subdirectory.
- src/Memorizer/Controllers - Contains API controllers.
- src/Memorizer/Services - Contains service logic including search logic.
- src/Memorizer/Services/ILlmService.cs - Can utilize LLM and embedding.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Actors - Contains actor models, refer to them when actor models are needed.
  - ChatBotActor.cs - Actor model that connects users via SSE edge and processes conversation requests. Generates responses using search and decision actors.
  - SearchMemoryActor.cs - Actor model responsible for memory search.
  - DecisionActor.cs - Actor model that determines if searched memory is related to the request content.
  - MetadataEmbeddingActor.cs - Actor model responsible for metadata embedding. Processes metadata embedding asynchronously when registering memory. This process vectorizes using embedding.
  - GraphsyncActor.cs - Actor model that syncs Postgres data to graph model using Neo4j.
- src/Memorizer.IntegrationTests - Contains integration test project.
- PageUrl: Has the following page URLs:
    - ui/blog
        - ViewMore: View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238: View content by id
    - ui/askbot - Chatbot page
- src/Memorizer.IntegrationTests/Actors: Contains existing unit tests. Refer to unit test methods.

## Local Testing Method
- Can test locally through docker-compose.local-psmon.yml. Use this file if docker operation issues occur.
- The basic configuration may be running~ Don't bring down everything with down command, rebuild and restart only memorizer-app.
  - postgmem-postgres has a well-working DB setup and sample data, so especially don't bring down this component, handle it carefully as if it's a production DB.
- This application is built and run through memorizer-app. When rebuild/restart is needed, use only docker without dotnet cli.
- DB schema changes are not allowed during testing improvements, only READ is allowed, postgres and neo4j are docker-based and working well, don't stop or bring them down.
- For actor model unit testing method, refer to the following, when creating actor models, write or update unit tests for that actor.
  - Write actor model tests at src/Memorizer.IntegrationTests/Actors location.
- When API testing is needed, test using a Client available in .NET. When SSE or WebSocket is needed, use appropriate client modules.

## API Authentication Method for Testing
- Check src/Memorizer/Controllers/AuthController.cs code.. proceed through registration API after passing api authentication..
- Note that views are unauthenticated, memory registration is only possible with authenticated API, after login success with admin/admin123, use the returned cookie value.
- src/Memorizer/Controllers/MemoryController.cs memory registration uses CreateMemory... Don't manipulate db, understand and utilize the provided API.


### Additional Instructions
- Don't record every time.. Only record when sharing.. So content should be saved to askbot_share_links when sharing.. And when clicking share, a popup should appear where you can see the link.. with a copy button together.
- The chatbot conversation session is maintained the same even when the browser is refreshed, but conversation history cannot be restored. Since the actor model is recording the session's conversation content, when refreshed, restore the previous conversation content based on the session id.
- Improve the chatbot's thinking progress animation... in the form of a wave animation.
