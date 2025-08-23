This project is MCP-Memorizer, which provides necessary knowledge for LLM Agents.
It synchronizes memory stored in postgres DB to Neo4j graph database and adds functionality to create relationships using LLM.
Follow the instructions below to proceed with project updates.


# Instructions
- The ui/graph endpoint is a page for querying and searching the graph DB. We want to improve the search functionality on this page.
 - Search functionality and related APIs should be accessible without authentication. If there are authentication restrictions in the search API, please remove them
 - Since somewhat long queries can be entered in Cypher Query, improve it to allow multi-line input instead of single line.
 - Since Cypher Query can be somewhat complex, improve it to allow selection of frequently used query examples from a dropdown to help with query writing.
 - Natural is a feature that performs searches in natural language. LLM should convert natural language to cypher queries for graph db search, but the LLM prompt is not appropriate and doesn't generate correct queries. Improve the prompt to enable natural language search.
 - Add dropdown selection for query samples attempted with Natural search to provide help, with examples in English as GraphDB Relation examples.
 - Display the Cypher Query used after Natural search execution in the UI as well.
 - Cypher Query UX and GraphView operate separately for query results. Modify GraphView to be compatible with CyperQuery attempted result mode, and improve compatibility with the Limit option. Query samples don't need Limit.
- Apply the improved version to the search-graph functionality already loaded in MCP.


# Source Location
- Project files are in "src/Memorizer/"
- Unit test files are in "src/Memorizer.IntegrationTests/"
- postgres schema is in src/Memorizer/migrations


# Testing Instructions After Code Completion
- Keep the original memory data in postgres. Initialize neo4j graph db and perform resync test. Refer to the implemented code related to api/graph/sync
- After all code writing is complete, perform unit tests and improve any errors or malfunctioning parts. Since LLM is used to convert natural language to cypher queries, check if correct cypher queries are generated and improve the prompt.
- After unit tests are complete, run on local port 5012 (restart if already running)... Here, perform API tests only with the improved content. postgres, ollama, neo4j etc. are already running and you can check their status and ports using docker ps.
- When restarting the application after local program update on netcore 5012, the previously used port may not be released properly and may not run correctly. First check if the application works well when restarting. If necessary, use force port kill limited to 5012
- When final testing is complete... organize useful CypherQuery examples for this project in GRAPH.md. They should be useful working samples.


# This Project's GraphDB Data for Memory Structure
Using neo4j graph database, description of Memory, Word nodes and relationships.


## Node : Memory Properties

Deals with Memory Content as core data. Understanding this content first will help with improvements.

```
<elementId>: 4:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:90
<id>: 90
confidence: 1.0
createdAt: 2025-08-22T01:31:57.6292047Z
id: 8f02c0f0-bb7e-42e5-aa99-9904f4d9bcb8
source: LLM
summary: # SSE-PUSH-SYSTEM - Server-Sent Events Real-time Event Push System

SSE (Server-Sent Events) based real-time event push system that implements topic-based pub/sub pattern, allowing clients to subscribe to specific topics and receive events in real-time.

## Tech Stack
- **Language**: Kotlin 2.0.0...
tags: sse,server-sent-events,real-time,pub-sub,spring-webflux,pekko,actor-model,kotlin,reactive
title: SSE-PUSH-SYSTEM - Server-Sent Events Real-time Event Push System
type: reference
```

## Node : Word Properties

Deals with Tags extracted from Memory Content and manages them as Word Nodes.

```
<elementId>: 4:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:15
<id>: 15
createdAt: 2025-08-21T17:16:37.6302691Z
frequency: 3
language: en
name: reactive-streams
updatedAt: 2025-08-22T01:31:56.8380793Z
```

## Relationship : HAS_KEYWORD

Memory -> HAS_KEYWORD -> Word relationship connection information exists.
It's equivalent to having Tag information for each Word that Memory has.


```
<elementId>: 5:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:13
<id>: 13
createdAt: 2025-08-21T17:16:37.6378220Z
relevance: 1.0
```


## Relationship : RELATES_TO

Memory-> RELATES_TO -> Memory relationship connection information exists.
Used for managing related documents with various type categories.

```
<elementId>: 5:2eb04ad0-a2a5-47ca-96cc-c206d6cdf8a6:189
<id>: 189
createdAt: 2025-08-22T01:54:37.9541646Z
type: enhanced-version
weight: 1.0
```