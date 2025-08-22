This project is MCP-Memorizer that provides necessary knowledge for LLM Agents.

This project adds functionality to synchronize memory stored in postgres DB to Neo4j graph database and create relationships using LLM.
Since there is no authentication feature, we plan to create a simplified authentication feature.

Follow these instructions to proceed with the project update.

# Instructions
- Login page exists at home path and performs matching checks through environment configuration without using DB. Add a preview at the top that explains this project.
- When accessing pages that require login without being logged in, redirect to login page.
- There are only two types of authentication: logged in (administrator) and not logged in.
- MCP SSE interface also lacks authentication feature~ improve to allow execution through simplified API-KEY. Configuration is possible through environment settings.


Non-login accessible menus are as follows:
- All Memories: https://mcp.webnori.com/ui
- Graph View: https://mcp.webnori.com/ui/graph
- DetailView: https://mcp.webnori.com/ui/view/f24539b8-af02-4ac3-86be-74239ac0fbd0
  - This page includes Edit/Delete functions which should not be exposed and should not be allowed via API.

When logged in, all menus are included and work the same as before.


# Test Instructions After Code Completion
- After all code writing is complete, perform unit tests and improve errors and malfunctioning parts. Since LLM is used for converting natural language to cypher queries, verify correct cypher queries are generated while improving prompts.
- When unit tests are complete, run on local port 5012... here perform api tests only with improved content. postgres, ollama, neo4j etc. are already running and you can check running status and ports through docker ps.

# Source Location
- Project files are in "src/Memorizer/
- Unit test files are in "src/Memorizer.IntegrationTests/"
