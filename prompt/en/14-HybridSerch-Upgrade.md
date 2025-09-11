This project is developed with .NET 9 and provides functions for searching/storing/modifying/deleting memory using MCP,
and additionally provides functionality to view and manage memory through a web viewer.

We intend to improve MCP functionality according to the following instructions.

## Improvement Instructions
- CreateGraphQueryPrompt contains prompts that help convert natural language to Cypher queries.
- SearchGraph using MCP provides functionality to convert natural language to Cypher queries.
  - After understanding the CreateGraphQueryPrompt content, update the Description for MCP so that LLM can generate more accurate Cypher queries
- Additionally, add SearchGraphByCypher functionality so that LLM can directly call Cypher queries
  - After understanding the CreateGraphQueryPrompt content, update the Description for MCP for proper use of SearchGraphByCypher
- Please do not modify the prompts used in CreateGraphQueryPrompt
- Refer to the project location and description, and once code improvement is complete, familiarize yourself with the local testing method and proceed
- After completion of improvements, analyze MemoryTools in MCP-GUIDE.MD to introduce how to utilize MCP functionality and usage prompts, and save this content to memory

## Project Location and Description
- Located in the src/Memorizer/ subdirectory.
- src/Memorizer/Views - Contains UI-related view files.
- src/Memorizer/Tools/MemoryTools.cs - MCP functionality is defined here and usage guide for LLM is defined in Description.
- PageUrl: Has the following page URLs:
  - ui/blog
    - ViewMore: View content through popup button
  - ui/view/2b61629b-d5b4-41c2-8163-6670893df238: View content by id


## Local Testing Method
- You can test locally through docker-compose.local-psmon.yml. Use this when checking operation after modifying code.
- This application is built and operated through memorizer. When rebuild/restart is needed, please use docker-compose method without dotnet cli
- Database schema changes are not allowed during test improvements.
  - Only understand and read postgres schema. No need to restart postgres, only query when improved.
  - When data writing is needed, only attempt through api, not direct db insert.
- Authentication can be performed according to the following configuration settings. Query api is possible without authentication, registration/modification/deletion api uses LOGIN for authentication
  - mcp uses sse and must be authenticated through X-API-Key header to be used.

### Authentication Information
Valid only in docker-compose.local-psmon.yml local environment.
```
  MEMORIZER_Server__UserName: admin
  MEMORIZER_Server__Password: admin123
  MEMORIZER_Server__ApiKey: your-api-key-here
```

### Additional Improvement Instructions
- The area that appears when clicking View Content in ui/graph is also a markdown view area, improve it to be the same as the view mode that operates when pressing ViewMore in ui/blog