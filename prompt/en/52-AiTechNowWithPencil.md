This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, graph exploration using Neo4j,
and additionally implements an ASK conversational bot feature using LLM, and provides various AI features through the MCP interface.
Please perform the improvement instructions by referring to the project location/description and local testing methods.

# Feature to Add
- news/ai-tech-now : AI Tech Now news page
  - This page does not need to provide the existing menu. Apply so that a designed page can be displayed
  - It can be accessed without login. Apply responsive design
- Search Filter Description
  - Keyword search: Same as ui/blog Search functionality, first check how to explore and display memory by referring to this feature
  - Default filter: There are categories below. Since categories are not classified, categorize based on keywords
    - ex> Vibe: "vibe" or "Vibes" or "바이브" : Including Korean, in this pattern
      - All: ("vibe" or "Vibes" or "바이브") and ("claude code" or "claude-code" or "클로드")....... Targets all supported categories.
- UI: Apply the design from Pencil/news.pen. Content to be filled uses the following integration (requests organized in UI order):
- Category - When clicking a category, refer to the filter from instructions and the category pencil design
 - Headline
   - Headline content: 1 most recent full article
   - Headline right list: Random 5 titles only within default filter
 - More Top Stories: Display recent 2nd, 3rd, 4th items in preview mode (max 5 lines)
 - Opinion: Keep as is (no data integration)
 - Most Popular
   - Display recent 5th through 14th, 10 titles only
 - Editor's PICK: Display 11th most recent in preview mode
 - Article (Article DetailPage pencil definition): Display the corresponding memory when clicking an article
   - Related Articles: Display connected memories


## Project Location and Description
- Refer to the prompt/kr/agent.md file.

## Local Testing Method
- Perform a build and fix build errors.
- Write unit tests for added code and run them.
- src/Memorizer/appsettings.Sam.json: Start this environment and improve runtime errors. Run in local mode.
- The development DB postgres is already running via local docker, so DB queries and investigation are possible. - Can use read for runtime error occurrence and feature error reports
- Once running normally, will test directly and wait for feedback.
  - Official deployment is done via .NET Docker on Linux-based infrastructure... but local testing will be performed on Windows with feedback expected.


## Additional Instructions
- The keyword search at the top was missing. When performing keyword search, it should be able to search by ignoring all designated filters and default filters
 - There is keyword search within the ui/blog feature, refer to this for the search
