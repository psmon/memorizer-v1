This project uses dotnet 9.0 and is configured as a full-stack application (API, UI).
It supports search and vector search using PostgreSQL, graph traversal using Neo4j,
and also implements an ASK conversational bot feature using LLM, and provides various AI features through MCP interface.

Please follow the instructions referring to project location and description, and local testing methods.

# Instructions
- The recent prompt/kr/38-LLMEX-OpenAI.md activity has been implemented, and LLMEX has been additionally enabled.
- The basic LLM supports compatibility with Custom, Ollama, OpenAI, etc.
  - CUSTOM is a version that matches the API spec provided by LM Studio.
  - When using OpenAI in LLMEX, there seems to be an error saying API Key is missing. Please refer to the existing implemented LLM interface and implementation to support it according to settings.
  - The AI model for LLMEX is loaded at application start and only one is selected through environment variables.
    - Allow selection of CUSTOM, OLLAMA, or OPENAI.
    - Verify that LLM-EX settings are correctly loaded and used at application startup according to environment variable settings.
- Please also organize the sample yml files with reference to environment variable updates.

## Environment Variable Update
This project supports various LLMs. Update environment variables with reference to the following configuration samples.
If there are missing settings, add them together and remove unnecessary settings. Since LLM-related settings are important, check carefully.

```
- docker-compose.custom-sample.yml: Uses CUSTOM LLM.
  - docker-compose.local-psmon.yml: Uses CUSTOM LLM for personal local testing.
- docker-compose.ollama.yml: Uses Ollama LLM.
  - docker-compose.yml: Default file that uses Ollama LLM.
- docker-compose.openai-sample.yml: Uses OpenAI LLM.
- docker-compose.server.yml: Server file for personal server deployment on rancher 1.6 version using docker-compose version 2. Uses CUSTOM LLM.
```
Create or update the docker-compose.md with documentation for which yml to use for each environment.


## Project Location and Description
- Refer to prompt/kr/agent.md file.

## Local Testing Method
- Only resolve build errors.
- After fixing build errors, testing will be done manually and modifications will be made based on feedback.
