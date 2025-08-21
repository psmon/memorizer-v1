# MCP-Memorizer Graph Database Structure & Cypher Query Guide

## Graph Schema

### Node Types

#### 1. Memory Node
Represents a stored memory/document in the system.

**Label:** `Memory`

**Properties:**
- `id` (string): UUID of the memory
- `type` (string): Type of memory (e.g., "reference", "how-to", "system", "conversation")
- `title` (string): Title of the memory
- `source` (string): Source of the memory (e.g., "user", "system", "LLM")
- `tags` (string[]): Array of tags
- `confidence` (double): Confidence score (0.0 - 1.0)
- `createdAt` (string): ISO date when created
- `summary` (string): Brief summary of the content

#### 2. Word Node
Represents keywords extracted from memories.

**Label:** `Word`

**Properties:**
- `name` (string): The keyword/word (lowercase)
- `language` (string): Language code (e.g., "en", "ko")
- `frequency` (int): Number of times this word appears across all memories
- `createdAt` (string): ISO date when first created
- `updatedAt` (string): ISO date when last updated

### Relationship Types

#### 1. RELATES_TO
Connects Memory nodes to other Memory nodes.

**Direction:** `(Memory)-[:RELATES_TO]->(Memory)`

**Properties:**
- `type` (string): Relationship type (e.g., "extends", "supports", "contradicts", "implements", "references", "related-to")
- `weight` (double): Strength of relationship (0.0 - 1.0)
- `createdAt` (string): ISO date when created

#### 2. HAS_KEYWORD
Connects Memory nodes to Word nodes.

**Direction:** `(Memory)-[:HAS_KEYWORD]->(Word)`

**Properties:**
- `relevance` (double): Relevance score (0.0 - 1.0)
- `createdAt` (string): ISO date when created

## Sample Cypher Queries

### Basic Queries

#### 1. Find all memories
```cypher
MATCH (m:Memory)
RETURN m
LIMIT 25
```

#### 2. Find memories by type
```cypher
MATCH (m:Memory {type: 'reference'})
RETURN m.title, m.source, m.createdAt
ORDER BY m.createdAt DESC
```

#### 3. Find all keywords
```cypher
MATCH (w:Word)
RETURN w.name, w.frequency
ORDER BY w.frequency DESC
LIMIT 20
```

### Search Queries

#### 4. Search memories by title
```cypher
MATCH (m:Memory)
WHERE m.title CONTAINS 'Docker'
RETURN m
```

#### 5. Search memories by tags
```cypher
MATCH (m:Memory)
WHERE 'kubernetes' IN m.tags
RETURN m.title, m.tags
```

#### 6. Find memories with high confidence
```cypher
MATCH (m:Memory)
WHERE m.confidence > 0.8
RETURN m.title, m.confidence, m.type
ORDER BY m.confidence DESC
```

### Keyword-based Queries

#### 7. Find memories by keyword
```cypher
MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word {name: 'docker'})
RETURN m.title, m.type
```

#### 8. Find most frequent keywords
```cypher
MATCH (w:Word)
RETURN w.name as keyword, w.frequency
ORDER BY w.frequency DESC
LIMIT 10
```

#### 9. Find memories with multiple specific keywords
```cypher
MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word)
WHERE w.name IN ['docker', 'kubernetes', 'container']
WITH m, COUNT(DISTINCT w) as keyword_count
WHERE keyword_count >= 2
RETURN m.title, keyword_count
ORDER BY keyword_count DESC
```

#### 10. Find keywords for a specific memory
```cypher
MATCH (m:Memory {title: 'Docker and Kubernetes Overview'})-[:HAS_KEYWORD]->(w:Word)
RETURN w.name as keyword, w.frequency as global_frequency
ORDER BY w.frequency DESC
```

### Relationship Queries

#### 11. Find related memories
```cypher
MATCH (m1:Memory {title: 'Docker Overview'})-[r:RELATES_TO]->(m2:Memory)
RETURN m2.title, r.type, r.weight
ORDER BY r.weight DESC
```

#### 12. Find memories that extend concepts
```cypher
MATCH (m1:Memory)-[r:RELATES_TO {type: 'extends'}]->(m2:Memory)
RETURN m1.title as extends_from, m2.title as extends_to, r.weight
ORDER BY r.weight DESC
```

#### 13. Find contradicting memories
```cypher
MATCH (m1:Memory)-[r:RELATES_TO {type: 'contradicts'}]->(m2:Memory)
RETURN m1.title, m2.title, r.weight
```

### Advanced Pattern Queries

#### 14. Find memories connected by common keywords
```cypher
MATCH (m1:Memory)-[:HAS_KEYWORD]->(w:Word)<-[:HAS_KEYWORD]-(m2:Memory)
WHERE m1.id <> m2.id
WITH m1, m2, COUNT(DISTINCT w) as common_keywords
WHERE common_keywords >= 3
RETURN m1.title, m2.title, common_keywords
ORDER BY common_keywords DESC
LIMIT 20
```

#### 15. Find most connected memories (hub nodes)
```cypher
MATCH (m:Memory)-[r]-(other)
WITH m, COUNT(r) as connections
RETURN m.title, m.type, connections
ORDER BY connections DESC
LIMIT 10
```

#### 16. Find keyword co-occurrence
```cypher
MATCH (w1:Word)<-[:HAS_KEYWORD]-(m:Memory)-[:HAS_KEYWORD]->(w2:Word)
WHERE w1.name < w2.name
WITH w1.name as word1, w2.name as word2, COUNT(DISTINCT m) as co_occurrences
WHERE co_occurrences >= 2
RETURN word1, word2, co_occurrences
ORDER BY co_occurrences DESC
LIMIT 20
```

### Path Finding Queries

#### 17. Find shortest path between memories
```cypher
MATCH path = shortestPath(
  (m1:Memory {title: 'Docker Overview'})-[*]-(m2:Memory {title: 'Kubernetes Guide'})
)
RETURN path
```

#### 18. Find all paths with specific length
```cypher
MATCH path = (m1:Memory)-[*2..3]-(m2:Memory)
WHERE m1.type = 'reference' AND m2.type = 'how-to'
RETURN m1.title, m2.title, length(path) as path_length
LIMIT 20
```

### Aggregation Queries

#### 19. Count memories by type
```cypher
MATCH (m:Memory)
RETURN m.type as type, COUNT(m) as count
ORDER BY count DESC
```

#### 20. Average confidence by source
```cypher
MATCH (m:Memory)
RETURN m.source as source, AVG(m.confidence) as avg_confidence, COUNT(m) as count
ORDER BY avg_confidence DESC
```

#### 21. Keywords per memory type
```cypher
MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word)
WITH m.type as type, COUNT(DISTINCT w) as unique_keywords, COUNT(*) as total_keywords
RETURN type, unique_keywords, total_keywords, 
       ROUND(toFloat(total_keywords) / unique_keywords, 2) as avg_keyword_usage
ORDER BY unique_keywords DESC
```

### Time-based Queries

#### 22. Recent memories
```cypher
MATCH (m:Memory)
WHERE datetime(m.createdAt) > datetime() - duration('P7D')
RETURN m.title, m.createdAt, m.type
ORDER BY m.createdAt DESC
```

#### 23. Memory creation timeline
```cypher
MATCH (m:Memory)
RETURN date(datetime(m.createdAt)) as date, COUNT(m) as memories_created
ORDER BY date DESC
LIMIT 30
```

### Complex Analysis Queries

#### 24. Find memory clusters by keyword similarity
```cypher
MATCH (m1:Memory)-[:HAS_KEYWORD]->(w:Word)<-[:HAS_KEYWORD]-(m2:Memory)
WHERE m1.id < m2.id
WITH m1, m2, COUNT(DISTINCT w) as shared_keywords
WHERE shared_keywords >= 5
MATCH (m1)-[:HAS_KEYWORD]->(w1:Word)
WITH m1, m2, shared_keywords, COUNT(DISTINCT w1) as m1_keywords
MATCH (m2)-[:HAS_KEYWORD]->(w2:Word)
WITH m1, m2, shared_keywords, m1_keywords, COUNT(DISTINCT w2) as m2_keywords
RETURN m1.title, m2.title, shared_keywords, 
       ROUND(toFloat(shared_keywords) / (m1_keywords + m2_keywords - shared_keywords), 3) as jaccard_similarity
ORDER BY jaccard_similarity DESC
LIMIT 20
```

#### 25. Keyword importance (TF-IDF like)
```cypher
MATCH (w:Word)
WITH COUNT(DISTINCT w) as total_words
MATCH (w:Word)<-[:HAS_KEYWORD]-(m:Memory)
WITH w, COUNT(DISTINCT m) as doc_frequency, total_words
RETURN w.name as keyword, 
       doc_frequency,
       w.frequency as total_occurrences,
       ROUND(log(toFloat(total_words) / doc_frequency), 3) as idf_score
ORDER BY idf_score DESC
LIMIT 20
```

### Graph Modification Queries

#### 26. Create a new memory-to-memory relationship
```cypher
MATCH (m1:Memory {title: 'Docker Overview'})
MATCH (m2:Memory {title: 'Container Security'})
MERGE (m1)-[r:RELATES_TO {type: 'references', weight: 0.8}]->(m2)
SET r.createdAt = datetime().toString()
RETURN m1.title, m2.title, r.type
```

#### 27. Update word frequency
```cypher
MATCH (w:Word {name: 'kubernetes'})
SET w.frequency = w.frequency + 1,
    w.updatedAt = datetime().toString()
RETURN w.name, w.frequency
```

#### 28. Add tags to memories
```cypher
MATCH (m:Memory)
WHERE m.title CONTAINS 'Docker'
SET m.tags = m.tags + ['containerization', 'devops']
RETURN m.title, m.tags
```

### Recommendation Queries

#### 29. Recommend related memories based on keywords
```cypher
MATCH (source:Memory {title: 'Docker Overview'})-[:HAS_KEYWORD]->(w:Word)
WITH collect(w.name) as source_keywords
MATCH (target:Memory)-[:HAS_KEYWORD]->(w2:Word)
WHERE w2.name IN source_keywords
  AND target.title <> 'Docker Overview'
WITH target, COUNT(DISTINCT w2) as matching_keywords, source_keywords
WHERE matching_keywords >= SIZE(source_keywords) * 0.3
RETURN target.title, target.type, matching_keywords, 
       ROUND(toFloat(matching_keywords) / SIZE(source_keywords), 2) as relevance_score
ORDER BY relevance_score DESC
LIMIT 10
```

#### 30. Find learning path
```cypher
MATCH path = (start:Memory {type: 'reference'})-[:RELATES_TO*1..3 {type: 'extends'}]->(end:Memory)
WHERE start.title CONTAINS 'Basic'
  AND NOT exists((end)-[:RELATES_TO {type: 'extends'}]->(:Memory))
RETURN [n in nodes(path) | n.title] as learning_path,
       length(path) as path_length
ORDER BY path_length
LIMIT 10
```

## Query Optimization Tips

1. **Use indexes**: Ensure properties used in WHERE clauses are indexed
2. **Limit results early**: Use LIMIT in subqueries when possible
3. **Use parameters**: For repeated queries, use parameters instead of hardcoded values
4. **Profile queries**: Use `PROFILE` or `EXPLAIN` to understand query execution

## Common Patterns

### Finding Popular Topics
```cypher
MATCH (w:Word)<-[:HAS_KEYWORD]-(m:Memory)
WITH w, COUNT(DISTINCT m) as memory_count
WHERE memory_count >= 3
RETURN w.name as topic, memory_count, w.frequency
ORDER BY memory_count DESC
```

### Memory Network Analysis
```cypher
MATCH (m:Memory)
OPTIONAL MATCH (m)-[r1:RELATES_TO]-(related:Memory)
OPTIONAL MATCH (m)-[r2:HAS_KEYWORD]->(w:Word)
RETURN m.title, 
       COUNT(DISTINCT related) as related_memories,
       COUNT(DISTINCT w) as keywords,
       AVG(r1.weight) as avg_relationship_weight
ORDER BY related_memories DESC
```

### Temporal Analysis
```cypher
MATCH (m:Memory)
WITH date(datetime(m.createdAt)) as creation_date, m
MATCH (m)-[:HAS_KEYWORD]->(w:Word)
RETURN creation_date, 
       COUNT(DISTINCT m) as memories,
       COUNT(DISTINCT w) as unique_keywords,
       collect(DISTINCT m.type) as types
ORDER BY creation_date DESC
```

## Usage Examples

### For Memory Search
Use queries 4-6 to find specific memories based on content, tags, or confidence levels.

### For Knowledge Discovery
Use queries 14-16 to discover relationships between memories through shared keywords.

### For Content Analysis
Use queries 19-21 to understand the distribution and characteristics of your knowledge base.

### For Recommendation Systems
Use queries 29-30 to build recommendation features based on content similarity.