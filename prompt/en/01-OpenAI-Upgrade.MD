This project is MCP-Memorizer that uses built-in Ollama for LlmService and EmbeddingService.

# Instructions
- Add OpenAILlmService and OpenAIEmbeddingService to enable use of OpenAI API.
- When the configuration URL is "https://api.openai.com/v1", use OpenAI API instead of the built-in Ollama.
Otherwise, use the built-in Ollama with existing code logic.
- After writing all code, perform a build to verify it builds successfully. Unit tests are not required.

# Source Location
- Located in subdirectories under "src/Memorizer/"
- When using OpenAI, utilize the appsettings.json configuration file.


# Reference
- Dotnet OpenAI sample code location: https://github.com/openai/openai-dotnet/tree/main/examples



