This project uses .NET 9.0 and is composed of a full-stack (API, UI) architecture.
It supports search and vector search using PostgreSQL, graph traversal using Neo4j,
and also implements an ASK conversational bot feature utilizing LLM, as well as providing various AI capabilities through an MCP interface.

Please refer to the project location and description, and local testing methods to execute the instructions.

# Instructions
- We are improving the ASKBOT. The existing functionality of ASKBOT can be verified by implementing the requirements in prompt/kr/15.01-ASKBOT-ActorModel.md.
- There is a feature that searches for up to 3 similar documents related to the question and references them. Search-Related Topic Inference Process
  - First, use LLM to determine whether the question requires one type of document, two types, or more. Write prompts appropriate for this purpose.
    - When one document is needed: Same as existing - reference 3 documents with high relevance
      - Example: "Search for A and summarize it for me"
    - When two documents are needed: When document types A and B are required - reference 2 documents (1 from A, 1 from B) that have relevance
      - Example: "Search for A and B and find commonalities"
    - When three or more are needed: A, B, C (maximum 3 limit) - reference up to 3 documents (1 each) with relevance
  - If there is no relevance, respond without reference (not searching documents/memories), same as existing behavior


## Project Location and Description
- Located in the src/Memorizer/ subdirectory
- src/Memorizer/Controllers - Contains API controllers
- src/Memorizer/Services - Contains service logic including search logic
- src/Memorizer/Services/ILlmService.cs - LLM and embedding services available
- src/Memorizer/Views - Contains UI-related view files
- src/Memorizer/Actors - Contains actor models, refer to when actor models are needed
  - ChatBotActor.cs - Actor model that processes conversation requests from users connected via SSE edge. Uses search and decision actors to generate responses
  - SearchMemoryActor.cs - Actor model responsible for memory search
  - DecisionActor.cs - Actor model that determines whether the searched memory is relevant to the request
  - MetadataEmbeddingActor.cs - Actor model responsible for metadata embedding. Processes metadata embedding asynchronously when registering memory. Vectorizes using embedding in this process
  - GraphsyncActor.cs - Actor model that syncs Postgres data to Neo4j graph model
- src/Memorizer.IntegrationTests - Contains integration test project
- PageUrl: Has the following page URLs:
    - ui/blog
        - ViewMore: View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238: View content by ID
    - ui/askbot - Chatbot page
    - ui/askbot/share/{shortlink} - Page to view shared chatbot conversation content
- src/Memorizer.IntegrationTests/Actors: Contains existing unit tests. Refer to unit testing methods

## Local Testing Method
- Only resolve build errors
- After fixing build errors, testing will be done directly and modifications will be made based on feedback
