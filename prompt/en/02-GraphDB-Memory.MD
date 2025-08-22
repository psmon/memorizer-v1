## Instructions

Study the following links and store a development tutorial on using Neo4j with the Actor Model in .NET development environment into the memory MCP.

- https://github.com/psmon/AkkaNeo4j/blob/master/AkkaNeo4j/AkkaNeo4j.csproj
- https://github.com/psmon/AkkaNeo4j/blob/master/readme.MD
- https://github.com/psmon/AkkaNeo4j/blob/master/AkkaNeo4j/Models/GraphEvent.cs
- https://github.com/psmon/AkkaNeo4j/blob/master/AkkaNeo4j/Module/GraphEngine.cs
- https://github.com/psmon/AkkaNeo4j/blob/master/AkkaNeo4j/Actors/GraphEventActor.cs


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