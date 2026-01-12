This project uses dotnet 9.0 and is composed as a full-stack (API, UI).
It supports search and vector search using PostgreSQL, graph traversal using Neo4j,
and also implements an ASK conversational bot feature using LLM, providing various AI features through MCP interface.

Please perform the improvement instructions referring to project location and description, local testing methods.

# Improvement Instructions
- ShapeUP supports free canvas drawing feature with additional elements
  - SVG BOX addition feature
    - Added when drawing vector images
      - Background should be transparent
      - Border line color and thickness adjustable - check existing property controls
      - Resizable, movable, deletable, rotatable etc. - apply existing manipulation features (check existing code)
      - Groupable with other shapes
  - Separately create useful generated icons using SVG BOX for use when drawing
    - e.g.: Arrow icon, user icon, cloud icon, server icon, database icon, etc.
    - Add to DRAWING TOOLS area using expandable mode to save space
- After SVG BOX frontend features are added, proceed with follow-up improvements
  - Test feedback will be provided, proceed with follow-up improvements after passing (no preliminary work)
  - ShapeUP has AI generation feature, design prompts to draw SVG so SVG BOX can be utilized when needed
  - There is also a Share feature, enable sharing and DB storage when SVG BOX is included
    - Check DB save and query functionality

# Reference
The following previous instructions can be referenced for this improvement:
- prompt/kr/42-shapeup-pitch-improve.md: Recent improvement instructions, currently working well.

## Project Location and Description
- Refer to prompt/kr/agent.md file.

## Local Testing Method
- Do not perform build and run. Testing is planned to be done manually and modifications will be made based on feedback.


# Improvements
- SVG box drag draws nothing
  - Expected: Check if free drawing, not just drawing a simple rectangle
  - UX improvement: When SVG BOX is drawn, provide property input for free element drawing
    - Before completion, create with sample content as default value in input field, e.g., create with default SVG for drawing SVG shape
    - Before completion or if internal elements are broken, show X mark as exception handling

- Memory search rules upgrade - PRDMaker (DDD Practice) PART, ShapeUP PART
  - Current: With similarity 0.3 or higher, adopt one with highest vector similarity each, review 3 for suitability
  - Improvement: When searching with similarity 0.3 or higher, tentatively adopt top 3 with highest vector similarity... review suitability using total 9
    - Use only top 3 with highest LLM-judged relevance as reference memory

- ui/prd feature has "PRD Demo Wireframe Generation" function, replace with the following
- Replace with feature to generate PRD demo wireframe using ShapeUP drawing tool
  - When PRD Demo Wireframe Generation button is clicked, navigate to ui/shapeup page and create Free Board (reference existing functionality)
  - Design prompt to generate wireframe using "PRD generated" data (reference existing PRD Demo Wireframe Generation feature)
  - Replace in the same way on ui/prd/share/7Lu9v2 page as well

- When clicking ShapeUp wireframe generation button and transitioning to ui/shapeup?prdWireframe=true
  - Open AI Board Generator feature and put request prompt in input field
  - Generation proceeds using existing functionality by clicking Generate
  - Current state: Only transition occurs with no response

## Test Prompts
DDD Practice generation feature test prompt
```
Create bulletin board applying reactive stream SOLID principles
```

ShapeUP
```
Draw reactive stream SOLID architecture
```

ShapeUP SVG BOX test prompt
```
Draw a small village using SVG... houses and roads should be balanced, trees and landscaping should be appropriately placed
Draw and arrange by elements, not as a single SVG
```
