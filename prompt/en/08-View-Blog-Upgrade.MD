
This project is MCP-Memorizer that provides necessary knowledge to LLM Agents.
The /ui endpoint is a page for viewing memories. Referring to this, we want to add a /ui/blog page to add a page where memories can be viewed in a blog-like format.
Please proceed with page creation using the following instructions and reference information.


# Instructions
- The menu name is Blog Memories. The page title is also Blog Memories.
- This page can operate without login.
- View memories in blog style using paging API functionality, but provide functionality to automatically load the next page according to scrolling.
- Show about 300 characters of the body as a preview, and allow viewing the full content by clicking the View More button, enabling viewing the full content in a popup view without page navigation.
- Show memories in newest order first.
- There's a keyword search function for memory titles and content at the top. Search is possible in SQL like %searchterm% format. After searching, only filtered results are shown on the same page.
- Below the keyword search bar are type and tags convenient button-type filter functions. Multiple filters can be selected and selected filters are applied together with keyword search.
- Applied filters can be removed by clicking the x button. Please separate type filters at the top and tags filters at the bottom.
- Filter buttons display the number of memories matching the current search criteria. For example, displayed as "type: article (123)", "tags: tech (45)", etc.
- After final code completion, check if it builds, fix build errors. Unit tests are not required.

# Reference Information
- Project files are located in "src/Memorizer/"
- Unit test files are in "src/Memorizer.IntegrationTests/"
- Service code for querying and managing memory storage is in "src/Memorizer/Services/Memory.cs"
- Postgres schema is in "src/Memorizer/migrations"