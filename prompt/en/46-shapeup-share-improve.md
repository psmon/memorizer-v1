This project uses dotnet 9.0 and is built as a full-stack application (API, UI).
It supports search and vector search using PostgreSQL, graph exploration using Neo4j,
an ASK conversational bot feature utilizing LLM, and provides various AI features through the MCP interface.
Please refer to the project location/description and local testing methods to execute the improvement instructions.

# Improvement Instructions
- /ui/shapeup/shares allows viewing shared pages, and we want to improve the share list and share view pages.
  - The view page shows shared pages but they appear as images.
    - Text cannot be copied when trying to use the content.
    - There is an editing feature, and we want to enable viewing by utilizing the editing functionality (/ui/shapeup)
      - When clicking the Edit button on the viewer page, navigate to /ui/shapeup page (Edit button is to the right of the Create New button at the top)
      - When navigating, load from the frontend side based on the last edited state
      - The prompt used for generation does not need to be transferred when navigating
      - All elements on the board canvas should be loaded
      - Since existing DB storage structure exists, check data structure for maximum compatibility without migration - only migrate if absolutely necessary
    - Existing elements can be changed and modified, but saving is not required.
    - PNG, SVG export is available, and should export based on the final edited state
    - When loading a shared page, load the prompt as well
      - The used prompt is provided as a collapsible UI at the bottom, not included in the board canvas
      - The prompt collapsible UI is collapsed by default and opens when clicking the expand icon
      - The prompt collapsible UI has a copy button for copying
      - The prompt collapsible UI is not displayed when there is no prompt
- When editing ui/shapeup, SVG ICONS auto-closes after one use which is inconvenient - improve so it doesn't auto-close (default still closed)
- Improve ui/shapeup editing tools as follows
  - Editing tool order should be:
    - DRAWING TOOLS
    - PROPERTIES
    - SVG ICONS
    - BOARD TEMPLATES (add collapse/expand - default collapsed and doesn't auto-close)
  - Support element copy/paste, Ctrl+C, Ctrl+V creates copy without overlapping, paste to the right of the copy target
    - There is a grouping feature, and grouping should also support copying while maintaining the group
  - Editing tools (Drawing TOOLS area) are too long making editing inconvenient
    - Match the editing tool height to the right canvas work area height and enable natural scrolling
      - When mouse over that area, only that tool scrolls, not affecting the overall scroll
    - (fix) The editing tool height should match the height of the board (canvas) area on the right... currently the tool height is drawn higher

# Reference

## Project Location and Description
- Refer to the prompt/kr/agent.md file.

## Local Testing Method
- Do not perform build and execution. Testing is done directly and modifications are made based on feedback.
