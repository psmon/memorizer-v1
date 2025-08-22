This project is MCP-Memorizer that provides necessary knowledge for LLM Agents.

This project adds functionality to synchronize memory stored in postgres DB to Neo4j graph database and create relationships using LLM.

Follow these instructions to proceed with the project update.

# Instructions
- The ui/graph endpoint is a page for querying and searching graph DB. Since the Sync function is separated, remove it from the UI and instead add a function to search GraphDB with cypher queries.
- Additionally add a function to ui/graph that searches in natural language, and LLM converts it to cypher query to search GraphDB. API endpoint exists, so investigate and connect it.
- Connection relationships between Node Details work the same as Postgres connections. Additionally, extract key keywords from content to create NodeWord and create NodeDetail->NodeWord relationships. NodeWord is created per word and prevents duplicate word creation.
- When data is added to PostgreSQL~ data is also added to GraphDB and edges of extended NodeWord are updated together. ui/tools/graph-sync is a full sync function that initializes graph db data and re-performs full update.
- Write unit tests for added or missing graph-related functions, referring to existing unit tests.
- After all code writing is complete, perform unit tests and improve errors and malfunctioning parts. Since LLM is used for converting natural language to cypher queries, verify correct cypher queries are generated while improving prompts.
- When unit tests are complete, run on local port 5012... perform local integrated api test. postgres, ollama, neo4j etc. are already running and you can check running status and ports through docker ps.

# Source Location
- Project files are in "src/Memorizer/
- Unit test files are in "src/Memorizer.IntegrationTests/"
- postgres schema is in src/Memorizer/migrations


## fix
- Change word to name in Word node properties. Because neo4j visualizes and expresses name.
- Graph Nodes display well in Neo4j tool. Check if ui/graph page queries graph db.. seems to express postgres view. Improve view and recheck search function to connect.
- NodeWord is created per word and preventing duplicate word creation means... "Hello" Node should not be created duplicately... but N connections should be possible. Useful for finding documents with many mentioned words. Understand logic and improve.
- In GRAPH.md, understand the types of graphs this project creates and write various sample queries to learn cypher query~