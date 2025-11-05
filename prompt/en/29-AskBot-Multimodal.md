This project uses .NET 9.0 and consists of a full-stack application (API + UI).
It supports search and vector search using PostgreSQL, and graph traversal using Neo4j.

The ui/askbot already has an interactive chatbot implemented, including a feature to share conversation history.
We aim to enhance its functionality according to the following instructions.

# Instructions
- Currently, askbot only accepts text requests and cannot handle multi-modal requests that include images.
- Improve the system to support multi-modal requests as follows:
  - Refer to and utilize the completed unit test for multi-modal functionality in src/Memorizer.IntegrationTests/Services/MultiModalServiceTests.cs.
  - Currently, when an initial request is made, it goes through memory search and decision processes before making a request to the LLM.
    - For text-only requests, it operates the same as before.
    - For multi-modal requests, improve it to pass both image + text to the LLM to receive responses.
  - Since multi-modal is being used for the first time in the service code, add configuration and DI injection.
  - The multi-modal model uses "qwen2/qwen3-vl-8b", and improve it to allow specifying the model name via environment variables.
    - If the environment variable is not specified, it operates with the default model name.
  - There is an image upload feature in the conversation input field, and when text is entered after uploading an image, it operates in multi-modal mode.
    - Only a maximum of 1 image can be uploaded, and when multiple images are uploaded, only the last uploaded image is used. - Constraint by configuration
    - Image uploads have a capacity limit and cannot exceed 3MB. Display an error message if exceeded. - Constraint by configuration
    - Image uploads only allow jpg and png formats, and display an error message for other formats.
    - Images can be uploaded via button, but should also support drag-and-drop or clipboard paste. - Using web technology

## Project Location and Description
- Located in the src/Memorizer/ subdirectory.
- src/Memorizer/Controllers - Contains API controllers.
- src/Memorizer/Services - Contains service logic including search logic.
- src/Memorizer/Services/ILlmService.cs - Can utilize LLM and embeddings.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Actors - Contains actor models, refer to them when needed.
  - ChatBotActor.cs - An actor model that processes conversation requests by connecting with users via SSE edge. Uses search and decision actors to generate responses.
  - SearchMemoryActor.cs - An actor model responsible for memory search.
  - DecisionActor.cs - An actor model that determines whether the searched memory is related to the request content.
  - MetadataEmbeddingActor.cs - An actor model responsible for metadata embedding. Processes metadata embedding asynchronously when registering memory. In this process, it vectorizes using embeddings.
  - GraphsyncActor.cs - An actor model that syncs Postgres data as a graph model using Neo4j.
- src/Memorizer.IntegrationTests - Contains integration test project.
- PageUrl: Has the following page URLs:
    - ui/blog
        - ViewMore: View content through popup button
    - ui/view/2b61629b-d5b4-41c2-8163-6670893df238: View content by id
    - ui/askbot - Chatbot page
    - ui/askbot/share/{shortlink} - View shared chatbot conversation page
- src/Memorizer.IntegrationTests/Actors: Contains existing unit tests. Refer to the unit testing method.

## Local Testing Method
- Only resolve build errors.
- After fixing build errors, testing will be done directly and modifications will be made based on feedback.

## API Authentication Method for Testing
- Check the src/Memorizer/Controllers/AuthController.cs code and proceed through the registration API after passing API authentication.
- Note: Views are unauthenticated, memory registration is only possible with authenticated API. After successful login with admin/admin123, use the returned cookie value.
- src/Memorizer/Controllers/MemoryController.cs: Use CreateMemory for memory registration. Do not manipulate the database; understand and utilize the provided APIs.
