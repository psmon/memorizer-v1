This project uses dotnet 9.0 and is structured as a full-stack application (API, UI).
It supports search and vector search using PostgreSQL, graph exploration using Neo4j,
and also implements an ASK conversational chatbot using LLM, providing various AI features through the MCP interface.
Please follow the improvement instructions referring to the project location, description, and local testing methods.


# Feature to Change
- ClaudeCode:
  - SkillCreate: Skill creation practice

# Improvement Instructions
- Job role skill - After recommending suitable skills, skill creation proceeds through 5 additional survey questions.
- When conducting additional surveys, acquire follow-up questions according to the following Planning & Design principles (prevent duplicate questions)
  - prompt/docs/04-ClaudeCode-PlanningAndDesign.md
- During the survey process, add insights explaining why each question is being asked and answer examples for skill learning purposes.


## Project Location and Description
- Refer to the prompt/kr/agent.md file.

## Local Testing Method
- Run the build to fix build errors.
- Write and run unit tests for added code.
- src/Memorizer/appsettings.Sam.json: Run this environment and fix runtime errors. Run in local mode.
- The development PostgreSQL database is already running via local Docker, so DB queries and inspection are available - can be used to read and report runtime/feature errors.
- Once running properly, manual testing will be conducted and feedback will be awaited.
