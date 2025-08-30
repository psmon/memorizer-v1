This project is MCP-Memorizer that provides necessary knowledge to LLM Agents.
It's a project that synchronizes memories stored in postgres DB to Neo4j graph database and adds functionality to create relationships using LLM.

Follow these instructions to proceed with the project update.

# Instructions
- The ui/graph endpoint is a page for querying and searching the graph DB. We want to improve the search and presentation features on this page.
- Node(Memory,Word) Relationship (HAS_KEYWORD, RELATES_TO) When RELATES_TO, ensure connection type is well displayed. When searching nodes, improve LLM-based graph exploration response to richly express connectivity between Node(Memory)->RELATES_TO->NODE(Memory), Node(Memory)->HAS_KEYWORD->NODE(WORD).
- Improve the query page to richly express connectivity as well.
- Apply the improved version to the search-graph feature already installed in MCP.
- Memory node's summary is the content. Add a view feature to see content with markdown style in the view that operates after clicking the memory node.
- After completing the code, perform additional test instructions.

# Source Location
- Project files are located in "src/Memorizer/"
- Unit test files are in "src/Memorizer.IntegrationTests/"
- Postgres schema is in src/Memorizer/migrations

# Test Instructions
- Original memory data exists in postgres. Keep this and perform resynchronization test after initializing neo4j graph db. Refer to implemented code related to api/graph/sync
- After all code writing is complete, perform unit tests while improving errors and malfunctioning parts. Since LLM is used for converting natural language to cypher queries, check if correct cypher queries are generated and improve prompts accordingly.
- After unit tests are complete, run locally on port 5012 (restart if already running) and perform API tests focusing on improved content.
- Postgres, ollama, neo4j are already running. Check operation status and ports through docker ps. Refer to docker-compose.local.yml for connection information.
- Application restart may have build and re-execution overhead during repeated testing. Maximize use of HotReload feature when code changes to verify execution without restart. Use kill to re-execute only when judging that changed features are not updated.
- Local execution is done with dotnet run on port 5012. Utilize HotReload feature supported by netcore9 to minimize restarts.
- Investigate if Hotreload is supported in net9 before proceeding.
- When final testing is complete, organize useful CypherQuery examples from this project in GRAPH.md. Since there's an existing guide, update with working useful samples if there are updates with added features.

# GraphDB Data for Memory Structure in This Project
Using neo4j graph database, this explains Memory, Word nodes and relationships.

## Node: Memory Properties

Handles Memory Content as core data. Understanding this content first helps with improvements.

```
<elementId>: 4:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:90
<id>: 90
confidence: 1.0
createdAt: 2025-08-22T01:31:57.6292047Z
id: 8f02c0f0-bb7e-42e5-aa99-9904f4d9bcb8
source: LLM
summary: # SSE-PUSH-SYSTEM - Server-Sent Events Real-time Event Push System

Real-time event push system using SSE(Server-Sent Events), implementing topic-based pub/sub pattern allowing clients to subscribe to specific topics and receive events in real-time.

## Tech Stack
- **Language**: Kotlin 2.0.0...
tags: sse,server-sent-events,real-time,pub-sub,spring-webflux,pekko,actor-model,kotlin,reactive
title: SSE-PUSH-SYSTEM - Server-Sent Events Real-time Event Push System
type: reference
```

## Node: Word Properties

Handles tags extracted from Memory Content, managed as Word Nodes.

```
<elementId>: 4:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:15
<id>: 15
createdAt: 2025-08-21T17:16:37.6302691Z
frequency: 3
language: en
name: reactive-streams
updatedAt: 2025-08-22T01:31:56.8380793Z
```

## Relationship: HAS_KEYWORD

Memory -> HAS_KEYWORD -> Word relationship connection information exists.
Equivalent to having as many tag information as Words that Memory has.

```
<elementId>: 5:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:13
<id>: 13
createdAt: 2025-08-21T17:16:37.6378220Z
relevance: 1.0
```

## Relationship: RELATES_TO

Memory-> RELATES_TO -> Memory relationship connection information exists.
Used for managing related documents with various type categories.

```
<elementId>: 5:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:189
<id>: 189
createdAt: 2025-08-22T01:54:37.9541646Z
type: enhanced-version
weight: 1.0
```

## Post-Completion Follow-up Improvement Instructions - Phase1

In GetGraphVisualizationAsync function, there's a limit that causes only Words to be queried, resulting in no Memory connections in the UX when there are no Memories.
Apply a limit of 100 Memories only, express Words that Memory has, then proceed with Relation processing subsequently. Additionally, Word display size is small - make it slightly larger but not larger than Memory.
Relation functionality works well and needs no improvement. Also adjust Memory count limit default value to 20.