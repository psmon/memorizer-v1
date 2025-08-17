
이 프로젝트는 MCP-Memorizer 로  LlmService,EmbeddingService 를 내장된 Ollama 사용합니다.

# 지침
-OpenAILlmService , OpenAIEmbeddingService 를 추가하여 OpenAI API를 사용할 수 있게합니다.
-설정 URL이 "https://api.openai.com/v1" 인경우  내장된 Ollama가 아닌 OpenAI API를 사용합니다.
아닌 경우 기존 동작코드로 내장된 Ollama를 사용합니다.
-코드를 모두 작성후.. 빌드가 되는지 빌드수행합니다. 유닛테스트는 안해도됩니다.

# 소스위치
- "src/Memorizer/ 하위디렉토리에 있습니다.
- OpenAI 사용시 appsettings.json 설정파일을 이용합니다.


# 참고내용
- Dotnet OpenAI 샘플코드위치 : https://github.com/openai/openai-dotnet/tree/main/examples





