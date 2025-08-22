This project is MCP-Memorizer that provides necessary knowledge for LLM Agents.

Follow these instructions to proceed with the project update.

# Instructions

- The existing DB uses postgres-based pgvector to store data. Additionally, use neo4j to store memory data in a graph DB.
- Neo4j doesn't need to store all content. It serves as an auxiliary role to find Graph exploration for existing memory stored in postgres.
- The endpoint page at /ui/tools/metadata-embedding has a function to re-update to postgres db. Referring to this, create a function to fully refresh Graph exploration from postgres data.
  - Retrieved documents have IDs and can be viewed in the following pattern. Document information is among node elements as properties, and clicking should switch page view. Sample endpoint is as follows: /ui/view/fab3af81-eeb0-46d8-9373-3706c6019a71
- Use LLM when creating edge data to better find existing documents through Graph relationships. Also write prompts for achieving the purpose.
- There's a page that queries/searches postgres db at the /ui endpoint UI. Referring to this, add an endpoint ui/graph (also add menu) to create a page that queries/searches GraphDB. Visualize the Graph.
- Once all features are implemented, add a function to search GraphDB with the name search-graph in mcp. Create a function that searches in natural language, and LLM converts it to cypher query to search GraphDB.
- Use existing Nuget Package versions as much as possible, and use versions compatible with existing Package versions for newly applied ones.
- After writing all code, perform a build to verify it builds successfully. Unit tests are not required.

# Source Location
- Located in subdirectories under "src/Memorizer/"
- postgres schema is in src/Memorizer/migrations


## Reference Usage Instructions

- Search for Neo4j development tutorials in memory and use as reference knowledge. If search fails, gradually lower similarity to find materials.


## How to Use Memory MCP
```
Memory MCP Usage Example
🧠 Example System Prompt for LLM
Important

You have access to a long-term memory system through **Model Context Protocol (MCP)** at the memorizer endpoint.
The following tools are available:

store: Store new memory
Parameters:

type
content (markdown)
source
tags
confidence
relatedTo (optional, memory ID)
relationshipType (optional)

search: Search for similar memories
Parameters:
query
limit
minSimilarity
filterTags

get: Retrieve memory by ID
Parameters:
id

getMany: Retrieve multiple memories by ID list
Parameters:
ids (list of IDs)

##delete: Delete memory by ID
Parameters:
id

createRelationship: Create relationship between two memories
Parameters:
fromId
toId
type
``