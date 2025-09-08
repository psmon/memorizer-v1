This project uses .NET 9.0 and consists of a full-stack (API, UI) architecture.
Please review the supplementary explanations below and execute the improvement instructions.

## Improvement Instructions
- Content in the content view is primarily written in MD (Markdown) format.
- Apply markdown styles to the ui/view page similar to the ui/blog popup view. For example, code views should have dark mode with language display and copy functionality.
- Both ui/view and ui/blog popup views are not displaying mermaid diagrams. Please improve to enable mermaid diagram display.
- Since displaying mermaid may be difficult to solve with styles alone, you may add JS libraries as needed.
  - You may use markdown JS with mermaid support, with the goal of becoming similar to GitHub's markdown view.
- Add zoom/pan buttons for mermaid diagrams, and enable zoom in/out with scroll.

## Project Location and Description
- Located in the src/Memorizer/ subdirectory.
- src/Memorizer/Views - Contains UI-related view files.
- PageUrl: Has the following page URLs:
  - ui/blog
    - ViewMore: Content viewing through popup button
  - ui/view/2b61629b-d5b4-41c2-8163-6670893df238: Content viewing by ID

## Local Testing Method
- Can be tested locally through docker-compose.local-psmon.yml. Use this file if there are Docker issues.
- This application is built and operated through memorizer. When rebuild/restart is needed, please use Docker only without dotnet CLI.
- After running Docker, when testing through API, for authentication, check the authentication method and login with admin/admin123, then use the authentication cookie.
- Database schema changes are not allowed during test improvements; only PostgreSQL schema understanding and reading are utilized. PostgreSQL doesn't need to be restarted, and improvements only involve queries.

## API Authentication Method for Testing
- Check the code in src/Memorizer/Controllers/AuthController.cs.. After passing API authentication, proceed through the registration API..
- Note that views are unauthenticated, memory registration is only possible through authenticated API. After successful login with admin/admin123, use the returned cookie value.
- src/Memorizer/Controllers/MemoryController.cs Memory registration uses CreateMemory ... Do not manipulate the DB, understand and utilize the provided API.