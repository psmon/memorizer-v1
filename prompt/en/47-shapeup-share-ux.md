This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, graph exploration using Neo4j,
and also implements an ASK conversational bot feature utilizing LLM, providing various AI features through MCP interface.
Please follow the improvement instructions while referring to project location, description, and local testing methods.

# Improvement Instructions
- /ui/shapeup: Free draw with "AI Board Generator" can generate boards, or you can create them manually.
  - When sharing without using AI generation, no title is generated. In this case, identify the drawn Text elements and auto-generate a title.
    - Ensure "No description available" doesn't appear - generate description for non-AI generated boards too
    - Use LLM to generate Title and Description
    - Consider multiple Text elements - use up to 500 characters from combined text instead of just the first item
    - Apply progress indicator while waiting for LLM response when Share button is pressed
- /ui/shapeup/shares: View shared pages, and we want to improve the share list and share view page.
  - The shared page only has zoom in/out functionality. Enable panning as well.
    - Apply mouse wheel for zoom in/out
    - Reference /ui/shapeup which already has zoom/pan (hand) controls
    - (FIX) Mouse wheel doesn't work on shared page, browser scroll is recognized first (zoom doesn't work), screen area movement with hand cursor also doesn't work
      - Suspected that shared page is in image view mode? Perhaps viewing shared page in whiteboard view mode would work

# Reference

## Project Location and Description
- Refer to prompt/kr/agent.md file.

## Local Testing Method
- Build and execution are not performed. Testing will be done manually with feedback-based modifications.
