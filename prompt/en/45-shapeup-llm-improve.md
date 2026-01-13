This project uses dotnet 9.0 and is built as a full-stack application (API, UI).
It supports search and vector search using PostgreSQL, graph exploration using Neo4j,
an ASK conversational bot feature utilizing LLM, and provides various AI features through the MCP interface.
Please refer to the project location/description and local testing methods to execute the improvement instructions.

# Improvement Instructions
- Memory Search Rules Upgrade - PRDMaker (DDD Practice) PART, ShapeUP PART
  - Current: Select one with highest vector similarity for each with relevance 0.3 or above, review 3 for suitability.
  - Improvement: When searching with relevance 0.3 or above, tentatively select 3 with highest vector similarity... use total of 9 for suitability review
    - Use only the 3 with highest LLM-judged relevance as reference memories

The ui/prd feature has a "Generate PRD Demo Wireframe" function, please replace it with the following:
- Replace with a feature that generates PRD demo wireframes using the ShapeUP drawing tool
  - When clicking the "Generate PRD Demo Wireframe" button, navigate to ui/shapeup page and create Free Board (refer to existing features)
  - Design prompts to generate wireframes using the "PRD generated" data (refer to existing PRD Demo Wireframe generation feature)
  - Apply the same replacement method on the ui/prd/share/7Lu9v2 page

When navigating to ui/shapeup?prdWireframe=true after clicking the ShapeUp wireframe generation button:
- Launch the AI Board Generator feature and put the request prompt in the input field
- Generation proceeds using existing features by pressing Generate
- Current state: Only navigation occurs with no response

When navigating to ui/shapeup?prdWireframe=true after clicking the ShapeUp wireframe generation button
ui/shapeup - AI Board Generator, Free Board generation improvements:
- If input text exceeds 2000 characters, use the prompt summarization feature to summarize to under 2000 characters before generation... apply following rules when summarizing:
  - Ensure core features, screen components, user flows, and other content necessary for wireframe generation are preserved during summarization
  - Remove unnecessary explanations, duplicate content, and details
- Apply the same summarization feature for cases not coming from PRD requests if over 2000 characters
- Show toast popup notification when summarization is applied
- If under 2000 characters, use the existing prompt request as-is
- Add memory search option checkbox during generation (default is disabled)
  - When checked: Use memory search
  - When unchecked: Do not use memory search
- There seems to be a bug where prompts are saved when sharing and loading... prompts are being displayed
- There are many cases of overlapping text in Free Board generation, apply following improvements:
  - Ensure proper spacing so no text elements overlap, add prompt cautions to prevent overlapping

# Reference
You can refer to the following previous instructions for this improvement:
- prompt/kr/42-shapeup-pitch-improve.md: Recent improvement instructions that are currently working well.

## Project Location and Description
- Refer to the prompt/kr/agent.md file.

## Local Testing Method
- Do not perform build and execution. Testing is done directly and modifications are made based on feedback.


## Test Prompts
DDD Practice generation feature test prompt
```
Create a bulletin board applying reactive stream SOLID principles
```

ShapeUP
```
Draw reactive stream SOLID architecture
```

ShapeUP SVG BOX test prompt
```
Draw a small village using svg... houses and roads should be balanced, trees and landscaping should also be properly placed
Use separate svg elements for each component instead of a single svg
```
