This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, graph exploration using Neo4j,
ASK conversational bot functionality using LLM, and provides various AI features through the MCP interface.
Please follow the improvement instructions referring to the project location, description, and local testing methods.

# Improvement Instructions
This project uses AI with two configured models: LLM and LLM-EX. They are similar, but LLM uses a lightweight model while LLM-EX uses an advanced model, utilized strategically as needed.
Improve the code so that the following feature areas that use LLM can also use LLM-EX.

## Feature Areas Requiring Upgrade
- ui/askbot
  - All LLM usage in this area
- ui/askbot/share
  - Memory saving functionality

# Reference

## Project Location and Description
- Refer to the prompt/kr/agent.md file.

## Local Testing Method
- Build and execution are not performed. Testing will be done manually, and modifications will be made based on feedback.

---
# Completed (v48)

## Implementation Details

### Phase 1: Response Generation LLM-EX Support
1. **UserChatRequest** - Added `UseExtendedModel` flag
2. **ChatBotActor** - `ILlmExService` injection support, LLM/LLM-EX selection via `CompleteWithLlmAsync` helper method
3. **AskBotController** - `ILlmExService` injection, passed to `StreamingChatBotActor`
4. **SaveMemoryRequest** - Added `UseExtendedModel` flag
5. **ui/askbot** - LLM-EX toggle switch added to header
6. **ui/askbot/share** - LLM-EX analysis option added for memory saving

### Phase 2: Reasoning Process LLM-EX Support (Feedback Applied)
7. **SearchMemoryActor** - `ILlmExService` injection support
   - `DetermineIfSearchNeeded`: Determines if memory search is required
   - `TransformQuery`: Query transformation
   - `ExtractKeywords`: Keyword extraction
8. **DecisionActor** - `ILlmExService` injection support
   - `EvaluateRelevance`: Search result relevance evaluation
9. **UseExtendedModel flag added to message types**:
   - `SearchMemoryRequest`
   - `AnalyzeQueryTypeRequest`
   - `MultiTopicSearchRequest`
   - `EvaluateRelevanceRequest`
10. **ChatBotActor** - Passes `UseExtendedModel` when making requests to child actors

## Modified Files
- src/Memorizer/Actors/ChatBotMessages.cs
- src/Memorizer/Actors/ChatBotActor.cs
- src/Memorizer/Actors/SearchMemoryActor.cs
- src/Memorizer/Actors/DecisionActor.cs
- src/Memorizer/Controllers/AskBotController.cs
- src/Memorizer/Views/AskBot/Index.cshtml
- src/Memorizer/Views/AskBot/Share.cshtml
- prompt/kr/agent.md
