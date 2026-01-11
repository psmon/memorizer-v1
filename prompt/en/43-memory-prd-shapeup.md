This project uses .NET 9.0 and is built as a full-stack application (API + UI).
It supports search and vector search using PostgreSQL, graph traversal using Neo4j,
and includes an ASK conversational bot feature powered by LLM, as well as various AI features through the MCP interface.

Please refer to the project location, description, and local testing methods to perform the improvement instructions.

# Improvement Instructions
- We want to improve the feature where PRD Maker and ShapeUp reference memory fragments when creating content.
- First, refer to the following prompts where existing functionality was completed:
  - prompt/kr/39-PRD-Maker-02.md: PRD Maker has functionality to reference memory fragments when writing PRDs.
  - prompt/kr/42-shapeup-pitch-improve.md: Does not yet reference memory fragments.
- Memory Reference Feature Upgrade
  - PRDMaker PART
    - Search for memories during Step 3 Example Mapping Discussion phase. Refer to Memory Fragment Usage Pattern for logic improvement.
  - ShapeUP PART
    - Add functionality to reference memory fragments during Free Board creation.
      - Improve to search for related memory fragments from the request prompt and reference them. Refer to Memory Fragment Usage Pattern.
  - Memory Fragment Usage Pattern: Code can be separated.
    - Separate the search process and the inference process that determines if searched documents are suitable
    - In PRDMaker, memories participate in the discussion process with improved search logic:
      - Extract or generate 3 useful keywords from Step 2 Event Storming for search attempts
      - Use relevance threshold of 0.3 or higher, selecting the top one for each keyword
      - In the inference phase, if each searched document is deemed suitable for Event Storming, use it as a reference memory
      - Since there are up to 3 memories, participants are distinguished as Memory1, Memory2, Memory3 in the discussion
    - In ShapeUp, memories participate during Free Board creation with improved search logic:
      - Extract or generate 3 key keywords from the creation request prompt for search attempts
      - Use relevance threshold of 0.3 or higher, selecting the top one for each keyword
      - In the inference phase, if each searched document is deemed suitable for Free Board creation, use it as a reference memory
      - Free Board can add text areas, and when referenced, additionally display the referenced memory fragment information and relevance.
    - When buttons are pressed, show the progress step by step, and display the adoption process as an informational toast popup.
      - e.g., "Searched 2 memory fragments. 2 were deemed suitable and referenced."
    - Design prompts well to achieve this objective.

## Project Location and Description
- Refer to the prompt/kr/agent.md file.

## Local Testing Method
- Build and execution are not performed. Testing will be done manually with feedback-based modifications.
