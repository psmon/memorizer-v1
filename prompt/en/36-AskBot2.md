This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, graph exploration using Neo4j,
and includes an ASK conversational bot feature utilizing LLM, with various AI capabilities provided through the MCP interface.

Please refer to the project location, description, and local testing methods to perform the instructions.

# Instructions
We are implementing a new feature called Memory Architecture.
It can be used without authentication.

- Two memories can be selected and combined to generate new architecture ideas.
  - Refer to the /ui page for vector search-based memory search. Reference the search functionality from there.
  - Each memory can be selected in left/right viewers.
    - Each has a keyword search field with adjustable similarity.
    - Separate from search... a random memory selection button is also provided.
      - Use existing implemented query functionality for random selection.
      - If A is selected, B should be randomly selected from a different topic/memory than A.
    - When a memory is selected, display the memory content as a card-style preview (digital memory-style UX).
  - After selecting memories, pressing the "Generate Idea" button:
    - First generates an idea prompt by combining the two selected memories.
    - If unsatisfied with the idea, press the regenerate button to generate again.
    - The generated prompt idea can also be edited.
  - Architecture generation can proceed after idea generation.
    - Press the "Generate Architecture" button:
      - Architecture generation allows selecting the sample code language (e.g., C#, Python, Java, etc.)
    - Generate architecture using the two memory contents and the idea for realization.
      - Architecture includes easy explanations, Mermaid diagrams when needed, and simple code representations.
    - Generation proceeds via streaming and can be stopped midway.
    - Generated architecture ideas are displayed in a text viewer.
      - Text viewer supports markup-style Mermaid and code view (copyable, language display).
        - Reference the /ui/view/4938c805-e9b1-48c4-a0a2-e0ef68d9faf6 page.
      - Users can share the generated architecture idea.
        - Share functionality exists in /ui/askbot share button. Use this functionality as-is.
  - Express this generation process smoothly and organically on a single page using streaming.
  - Design LLM prompting well for this feature's purpose.
    - Separate the prompt functionality written for this feature into a separate file - dedicated to this feature.
    - Guide combining the two selected memories well for designing creative ideas.
  - Add text in the footer explaining this feature's purpose and usage briefly.

## Project Location and Description
- Reference the prompt/kr/agent.md file.

## Local Testing Method
- Only resolve build errors.
- After fixing build errors, testing will be done manually with feedback-based modifications.

# Additional Improvements
- When generating Creative Idea prompts:
  - Improve to generate idea prompts in Markdown style and display the view in markdown style.
- Appears to use dark style - improve to light mode style (no toggle needed)
- Display responses naturally via SSE streaming, call LLM API in streaming mode
  - Reference the existing /ui/askbot page's chatbot streaming method
  - If there are non-streaming parts in the call process, change those to streaming as well
    - Check if the functionality to be changed is used elsewhere, review impact
- Prompt improvements
  - Clearly instruct to respond in Korean
- Rendering related
  - Improve Mermaid to display progressively as it's drawn, currently showing broken
  - Some code blocks are not visible - keep code blocks dark theme, text appears black so invisible
    - Add copy button to code blocks, display selected language
- Match ui/askbot share functionality specs - currently has errors
  - ui/askbot/share/Xy6R2y, check DB structure accessible via Docker in current environment
  - DB access info available through ASPNETCORE_ENVIRONMENT=Sam environment
- Add page to view shared List from ui/architecture
  - Display shared content preview in stylish card view
    - Cards display responsively based on width
    - Cards include title, summary, creation time, clickable link
    - Title and summary displayed in markdown style
    - Creation time displayed nicely in local timezone
  - Navigate to shared page on click
  - Implement infinite scroll loading
- No menu access to Architecture sharing
  - Configure menu as follows... related share list as submenu, parent menu is existing feature (no expand needed)
```
...
ASK BOT
 - Knowledges
Memory Architecture
 - Architectures
...
```
- Rename AI Knowledges to Knowledges
- Currently when generating ideas, it writes as follows... remove the idea title label, so the generated idea title itself becomes heading 1
```
# Idea Title
"Actor-Graph Visualization Platform (ActorGraphViz)"

to

# Actor-Graph Visualization Platform (ActorGraphViz)
```
