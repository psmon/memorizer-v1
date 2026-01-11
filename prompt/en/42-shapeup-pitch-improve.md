This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, graph traversal using Neo4j,
and also implements an ASK conversational bot feature utilizing LLM, providing various AI features through the MCP interface.

Please follow the improvement instructions while referring to the project location, description, and local testing method.

# Improvement Instructions
- Enhance the ShapeUp Pitch generation feature implemented in prompt/kr/41-shapeup-pitch.md
  - UI/UX Improvements: ui/shapeup
    - Left side: drawing tools, Right side: canvas, Bottom-right: utility toolbar for zoom, pan, and generation
      - Improve utility toolbar to be fixed at the bottom of the right canvas so it's always visible
        - Currently, the utility toolbar is hidden by scroll area causing editing inconvenience
    - In drawing tools, Select (arrow) feature only allows object selection via drag and drop; improve to also allow selection by click
      - (fix) First shape is selectable but second drawn shape is not - fix this issue
    - Introduce magnetic snap feature for arrow tool
      - When dragging on a shape, snap to the shape like a magnet
        - Apply magnetic snap to both drag start and drag end shapes for easy arrow connection between two shapes
        - When connected shapes are moved, arrows should move together
        - (fix) Arrow tool selected, drag starts from shape1 and ends at shape2... arrow not connecting
        - (fix) Arrow should be shown during drag and drop... currently arrow only appears after connection is complete (magnetic snap confirmed working)
          - (fix) Arrow start and end points should preview following mouse position during drag, but currently no preview before drawing completion
          - (fix) After preview feature was introduced, existing magnetic snap detection not working well
    - Improve Text tool
      - Currently, "Text" is auto-filled on initial text tool creation causing inconvenience - start with empty string
      - Text object should only be created when text value is entered... cancel creation if no input
    - Add Grouping feature
      - Show context menu on right-click
        - Add group function when shapes are selected
        - Add ungroup function
        - Grouped objects can be moved together when selected
        - Grouped shapes are locked, but Text can still be edited even when grouped (higher selection priority)


## Project Location and Description
- Refer to prompt/kr/agent.md file.


## Local Testing Method
- Do not perform build and run. Testing will be done directly with feedback-based modifications.

## Additional Improvements
-
