This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, graph exploration using Neo4j,
and also implements ASK conversational chatbot functionality using LLM, providing various AI features through MCP interface.

Please follow the instructions referring to project location and description, and local testing methods.

# Instructions
- PRD Maker feature was implemented according to prompt/kr/37-PRD-Maker.md and prompt/kr/39-PRD-Maker-02.md.
- In this instruction, we will add the following features to further extend PRD Maker functionality:
  - When step 5 Refined PRD generation is complete, attempt to create a working demo wireframe page based on this content.
    - This is implemented as a separate generation activity from the PRD Maker flow.
    - Add a "Generate PRD Demo Wireframe" button that triggers on click.
    - Use LLM-EX to generate demo wireframe based on the refined PRD content.
    - Generation starts from the button click and opens result page in a new window upon completion.
    - Detailed rules for creating frontend pages are described below.
  - Generate demo static pages in src/Memorizer/wwwroot/demo/{shortlink} directory.
    - This page is a PRD demo page consisting of simple HTML, CSS, JS as static files.
    - The page includes main features and screen composition based on the refined PRD content.
    - Apply responsive design to ensure the page looks good on various devices.
    - Include main feature descriptions and user interaction examples on the page.
    - Implement the generated page to open in a new window from PRD Maker UI.
    - Use external CDN for required resources (CSS, JS) to minimize page size.
    - Design the prompt for page generation carefully.
    - Review for errors after completing the code.
    - Review and modify .NET code server settings to ensure static pages work properly.
    - Carefully check prompts and paths so LLM-EX can accurately generate static files for the page.

### Additional Improvements
- Confirmed static page generation in demo/u8AAUd/index.html format
  - Since access is inconvenient, improved to provide direct link in modal window with copy link and open new window features (using relative path to provide full URL)
- This feature should also be available when viewing PRD Shares content
  - When refined PRD content exists, provide the same "Generate PRD Demo Wireframe" button to work identically


## Project Location and Description
- Refer to prompt/kr/agent.md file.

## Local Testing Method
- Only resolve build errors.
- After fixing build errors, testing will be done directly with modifications based on feedback.
