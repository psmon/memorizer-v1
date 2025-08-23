# Graph Database Query Examples for Memorizer

This document contains useful Cypher query examples for working with the Memorizer Neo4j graph database.

## NEW UI Features (Updated)
- **Multi-line Cypher Query Input**: Textarea with Ctrl+Enter execution
- **Query Examples Dropdown**: Quick access to common patterns
- **Natural Language Search**: Automatic Cypher generation from natural language
- **Automatic LIMIT Application**: UI respects node limit dropdown (no need for LIMIT in queries)
- **GraphView Integration**: Query results are displayed in the graph visualization
- **Public API Access**: Graph search APIs no longer require authentication

## Schema Overview

### Nodes
- **Memory**: Stores memory content with properties like id, type, source, title, summary, tags, confidence, createdAt
- **Word**: Stores keywords/tags with properties like name, language, frequency, createdAt, updatedAt

### Relationships
- **RELATES_TO** (Memory � Memory): Connections between memories with type and weight properties
- **HAS_KEYWORD** (Memory � Word): Links memories to their keywords with relevance property

## Basic Queries

### Get All Memories
```cypher
MATCH (m:Memory) 
RETURN m
```

### Find Memories by Type
```cypher
-- Reference documents
MATCH (m:Memory {type: 'reference'}) 
RETURN m

-- How-to guides
MATCH (m:Memory {type: 'how-to'}) 
RETURN m

-- System memories
MATCH (m:Memory {type: 'system'}) 
RETURN m
```

## Keyword and Tag Searches

### Find Memories by Keywords
```cypher
-- Find memories about Docker or Kubernetes
MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word) 
WHERE w.name IN ['docker', 'kubernetes', 'container'] 
RETURN DISTINCT m, w
```

### Search in Title and Summary
```cypher
-- Find memories about SSE/Server-Sent Events
MATCH (m:Memory) 
WHERE m.title CONTAINS 'SSE' 
   OR m.summary CONTAINS 'Server-Sent' 
   OR 'sse' IN m.tags 
RETURN m
```

### Most Frequent Keywords
```cypher
MATCH (w:Word) 
RETURN w 
ORDER BY w.frequency DESC
```

### Memories with Common Keywords
```cypher
MATCH (m1:Memory)-[:HAS_KEYWORD]->(w:Word)<-[:HAS_KEYWORD]-(m2:Memory) 
WHERE m1.id <> m2.id 
WITH m1, m2, COUNT(DISTINCT w) as common_keywords 
WHERE common_keywords > 2 
RETURN m1, m2, common_keywords 
ORDER BY common_keywords DESC
```

## Relationship Queries

### Find Specific Relationship Types
```cypher
-- Find 'extends' relationships
MATCH (m1:Memory)-[r:RELATES_TO {type: 'extends'}]->(m2:Memory) 
RETURN m1, r, m2

-- Find enhanced versions
MATCH (m1:Memory)-[r:RELATES_TO {type: 'enhanced-version'}]->(m2:Memory) 
RETURN m1, r, m2

-- Find examples
MATCH (m1:Memory)-[r:RELATES_TO {type: 'example-of'}]->(m2:Memory) 
RETURN m1, r, m2
```

### Most Connected Memories
```cypher
MATCH (m:Memory)-[r]-(other) 
WITH m, COUNT(r) as connections 
WHERE connections > 3 
RETURN m, connections 
ORDER BY connections DESC
```

### Memory Relationship Paths
```cypher
-- Find paths between reference and how-to documents
MATCH path = (m1:Memory {type: 'reference'})-[:RELATES_TO*1..3]-(m2:Memory {type: 'how-to'}) 
RETURN path
```

## Advanced Analysis Queries

### Hub Memories (Many Keywords)
```cypher
MATCH (m:Memory)-[r:HAS_KEYWORD]->(w:Word) 
WITH m, COUNT(DISTINCT w) as keyword_count 
WHERE keyword_count > 5 
RETURN m, keyword_count 
ORDER BY keyword_count DESC
```

### High Confidence Recent Memories
```cypher
MATCH (m:Memory) 
WHERE m.confidence > 0.8 
RETURN m 
ORDER BY m.createdAt DESC
```

### Memories with Rich Metadata
```cypher
MATCH (m:Memory) 
WHERE m.confidence > 0.8 
  AND size(m.tags) > 3 
RETURN m 
ORDER BY m.createdAt DESC
```

### Find Memory Clusters
```cypher
-- Find memories that share multiple keywords
MATCH (m1:Memory)-[:HAS_KEYWORD]->(w:Word)<-[:HAS_KEYWORD]-(m2:Memory)
WHERE m1.id < m2.id
WITH m1, m2, COLLECT(DISTINCT w.name) as shared_keywords
WHERE SIZE(shared_keywords) >= 3
RETURN m1.title, m2.title, shared_keywords
```

## Source-based Queries

### Find LLM-generated Memories
```cypher
MATCH (m:Memory {source: 'LLM'}) 
RETURN m 
ORDER BY m.createdAt DESC
```

### Find User-created Memories
```cypher
MATCH (m:Memory {source: 'user'}) 
RETURN m 
ORDER BY m.createdAt DESC
```

## Complex Pattern Matching

### Reference Documents with Examples
```cypher
MATCH (ref:Memory {type: 'reference'})<-[r:RELATES_TO {type: 'example-of'}]-(example:Memory) 
RETURN ref, r, example
```

### Multi-hop Connections
```cypher
-- Find memories connected through multiple relationships
MATCH path = (m1:Memory)-[:RELATES_TO*2..4]-(m2:Memory) 
WHERE m1.id <> m2.id 
  AND ALL(r IN relationships(path) WHERE r.weight > 0.5)
RETURN path
```

### Find Knowledge Chains
```cypher
-- Find chains of related knowledge
MATCH path = (start:Memory {type: 'reference'})-[:RELATES_TO*1..5]->(end:Memory)
WHERE ALL(r IN relationships(path) WHERE r.type IN ['extends', 'implements', 'example-of'])
RETURN path
```

## Statistics and Aggregation

### Memory Type Distribution
```cypher
MATCH (m:Memory)
RETURN m.type as type, COUNT(*) as count
ORDER BY count DESC
```

### Relationship Type Distribution
```cypher
MATCH ()-[r:RELATES_TO]->()
RETURN r.type as relationship_type, COUNT(*) as count
ORDER BY count DESC
```

### Keywords by Language
```cypher
MATCH (w:Word)
RETURN w.language as language, COUNT(*) as count, AVG(w.frequency) as avg_frequency
ORDER BY count DESC
```

### Memory Creation Timeline
```cypher
MATCH (m:Memory)
RETURN date(m.createdAt) as date, COUNT(*) as memories_created
ORDER BY date DESC
```

## Natural Language Query Examples

When using the natural language search feature, you can use queries like:

- "Find all reference documents about AI"
- "Show memories that extend DDD concepts"
- "Find the most connected memories"
- "Show enhanced versions of existing memories"
- "Find memories about Docker or Kubernetes"
- "Show memories related to reactive programming"
- "Find SSE or Server-Sent Events implementations"
- "Show the most frequently used keywords"
- "Find memories with common keywords"
- "Show recent high-confidence memories"

## Tips for Effective Queries

1. **LIMIT is automatic**: The UI applies LIMIT based on the node limit dropdown (no need to add it)
2. **Use DISTINCT**: When joining through relationships, use DISTINCT to avoid duplicates
3. **Use WITH for aggregation**: Use WITH clauses to perform multi-step aggregations
4. **Index usage**: The database has indexes on Memory.id and Word.name for better performance
5. **Case sensitivity**: Use CONTAINS for case-insensitive text searches
6. **Path queries**: Limit path length (e.g., *1..3) to avoid expensive computations

## Common Relationship Types

- `extends`: One memory extends or builds upon another
- `enhanced-version`: An improved or updated version of a memory
- `supports`: One memory supports or validates another
- `contradicts`: Conflicting or opposing information
- `implements`: Practical implementation of a concept
- `references`: Citation or reference to another memory
- `related-to`: General relationship
- `example-of`: Concrete example of a concept
- `explains`: One memory explains another