
Following the Shape Up whiteboard methodology, we will create a board that supports drawing (rendering) the following elements on a single web-based board, and then build a tool that automatically fills in the board based on PRD content. All elements will be included in a single unified board rather than separate boards.

```
Breadboard
Fat Marker Sketch
Problem Definition Board
Risk Assessment Board
Pitch Summary Board
Pitch Assembly: Problem + Appetite + Solution + Rabbit Holes + No-Gos
    ↓
  Completed Pitch Board
```

# Detailed Instructions
- The Shape Up board is created with drawing tools similar to draw.io
  - The board size can expand based on content, with zoom in/out and pan features included
- The bottom-right area allows generation through prompt input
  - PRD can create a board in the format proposed by Shape Up methodology from simple requirements
  - Board generation proceeds by order and type, with prompt design for achieving objectives
  - During generation, the board is rendered in real-time
  - Created boards can be exported as PNG, SVG, or other image formats
  - Created boards can be shared via short links
    - Clicking share opens a modal with the short link, including copy and open in new tab features

# Menu Location
PRD Maker
 - PRD Share: Function for sharing PRD
 - ShapeUP: Shape Up whiteboard and AI generation feature
   - ShapeShared: List of shared ShapeUP boards

# Shape Up Whiteboard Methodology
- https://mcp.webnori.com/ui/view/75b9325e-01c4-49af-94c9-a3b9983e410e

## Additional Fixes
- Improvement for long sentences not wrapping and extending to the right causing viewing difficulties
  - Add auto-wrap feature (check parent box area)
- Adjust PRD Maker and ShapeUP menu to the same depth level
```
- PRD Maker
  - PRD Share
- ShapeUP
  - ShapeShared
```
- Add integrated option in addition to individual board generation in AI generation
  - For integrated generation, implement all 5 boards in a single board
    - Follow the Shape Up whiteboard creation order and development methodology
  - Design different prompts for individual vs integrated generation
    - Individual generation: Prompts tailored to each board's purpose
    - Integrated generation: Comprehensive prompt for completing the entire board
      - Proceed step by step, with each step referencing previous step results
      - Briefly explain each board's purpose then provide guidelines for completing the entire board
      - Separate each board with dividing lines
      - Clearly display each board's title
      - Ensure board contents don't overlap
      - Provide guidelines to maintain consistent style and format across all boards
      - Final completed board should be visually clean and easy to understand
      - Display progress on button and render boards one by one as they complete
- Improvement for AI generation area and movement control area overlapping causing inconvenience
  - Keep movement control area and add AI generation toggle button
    - AI generation area appears when toggle button is clicked
    - AI generation area can be closed with close button
    - Movement control area remains visible when AI generation area is closed - Adjust Z-index
    - Check if canvas pan feature exists in movement controls, add if not (hand icon)


## Local Testing Method
- Do not perform build and run. Testing will be done directly with feedback-based modifications.

## Reference Links
- https://www.reddit.com/r/ProductManagement/comments/194y0oy/report_trialling_basecamps_shape_up_methodology/
- https://basecamp.com/shapeup
- https://en.wikipedia.org/wiki/37signals
