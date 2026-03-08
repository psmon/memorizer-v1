This project uses dotnet 9.0 and is configured as a full-stack (API, UI) application.
It supports search and vector search using PostgreSQL, graph exploration using Neo4j,
and also implements an ASK conversational bot feature using LLM, providing various AI features through MCP interface.
Please follow the improvement instructions referring to the project location/description and local testing methods.

# Features to Improve

- news/ai-tech-now - A page that turns memory fragments into news articles.
  - Most Popular
    - Currently displays 10 items. Add a "more 5" button to load the next 5 items (continuously operable)
- news/ai-tech-now/article - News article detail page.
  - The title appears twice consecutively in the article body. Make it appear only once (remove duplicate second title)
  - Clicking "Share this page link" copies the share link. - Domain address is configured in settings, combine with relative path
  - Render mermaid code blocks as diagrams - Refer to ui/view for implementation
  - The article body has empty space on the right side on mobile. Improve styles to fill the full width

- ui: This is the home route. It has a menu, and at the bottom of the menu
  add an AITECH-NEWS menu item that opens "news/ai-tech-now" in a new window when clicked


## Project Location and Description
- Refer to the prompt/kr/agent.md file.

## Local Testing Method
- Run the build to fix any build errors.
- Write and run unit tests for added code.
- src/Memorizer/appsettings.Sam.json: Run this environment and fix runtime errors. Run in local mode.
- The development database (postgres) is already running via local docker, so DB queries and investigation are possible. - Can be used to report runtime/feature errors via read
- Once running normally, manual testing is planned, so wait for feedback.
  - Official deployment is Linux-based via .NET Docker... but local testing will be done on Windows with feedback provided.
