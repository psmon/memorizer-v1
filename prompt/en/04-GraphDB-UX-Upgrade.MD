This project is MCP-Memorizer that provides necessary knowledge for LLM Agents.

This project adds functionality to synchronize memory stored in postgres DB to Neo4j graph database and create relationships using LLM.

Follow these instructions to proceed with the project update.

# Instructions
- Recently changed word to name in Word node properties. Since word property is not used, maintain only name.
- For Memory node and Word relationships, use tags already extracted in postgres without using LLM. That is, word's name becomes tag and consider updates for both when stored from mcp and ui/tools/graph-sync full update.
- When data is added to PostgreSQL~ data is also added to GraphDB and edges of extended NodeWord are updated together. ui/tools/graph-sync is a full sync function that initializes graph db data and re-performs full update.
- When executing ui/tools/graph-sync Start Sync, initialize existing GraphDB with "MATCH (n) DETACH DELETE n;" query, then fetch all data from postgres DB and add Memory nodes to GraphDB. Word nodes are also created at this time.
- Memory to Memory connections add relationships from postgres DB as-is to GraphDB.
- Write unit tests for added or missing graph-related functions, referring to existing unit tests.
- After all code writing is complete, perform unit tests and improve errors and malfunctioning parts. Since LLM is used for converting natural language to cypher queries, verify correct cypher queries are generated while improving prompts.
- When unit tests are complete, run on local port 5012... perform local integrated api test. postgres, ollama, neo4j etc. are already running and you can check running status and ports through docker ps.

# Source Location
- Project files are in "src/Memorizer/
- Unit test files are in "src/Memorizer.IntegrationTests/"
- postgres schema is in src/Memorizer/migrations