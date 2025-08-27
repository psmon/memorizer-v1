# Graph Database Query Examples for Memorizer

This document contains useful Cypher query examples for working with the enhanced Memorizer Neo4j graph database.

## 🚀 NEW Enhanced Features (Updated)

### UI Enhancements
- **Multi-line Cypher Query Input**: Textarea with Ctrl+Enter execution
- **Query Examples Dropdown**: Quick access to common patterns with categorized examples
- **Natural Language Search**: Advanced LLM-powered Cypher generation from natural language
- **Automatic LIMIT Application**: UI respects node limit dropdown (no need for LIMIT in queries)
- **GraphView Integration**: Query results displayed with enhanced visualization
- **Public API Access**: Graph search APIs no longer require authentication
- **Markdown Content Viewer Modal**: Click nodes to view full markdown content in modal
- **Hub Node Visualization**: Highly connected nodes shown with special styling
- **Enhanced Tooltips**: Rich tooltips showing connection counts, summaries, and metadata
- **Relationship Subtype Display**: RELATES_TO relationships show their specific subtypes

### Graph Search Enhancements
- **Richer Relationship Expressions**: Better visualization of relationship types
- **Connection Count Metadata**: Nodes include connection count information
- **Auto-expansion**: Small result sets automatically expanded with connections
- **Hub Node Detection**: Automatic identification of highly connected nodes

## Schema Overview

### Nodes
- **Memory**: Stores memory content with properties:
  - `id` (string, UUID): Unique identifier
  - `type` (string): reference, how-to, system, conversation, document
  - `source` (string): LLM, user, system
  - `title` (string): Descriptive title
  - `summary` (string): Full markdown content
  - `tags` (string[]): Array of keyword tags
  - `confidence` (double): 0.0-1.0 confidence score
  - `createdAt` (datetime): ISO datetime string

- **Word**: Stores keywords/tags with properties:
  - `name` (string): The keyword/word
  - `language` (string): Language code (en, ko, etc.)
  - `frequency` (int): Usage frequency count
  - `createdAt` (datetime): Creation timestamp
  - `updatedAt` (datetime): Last update timestamp

### Relationships
- **RELATES_TO** (Memory → Memory): 
  - `type` (string): extends, enhanced-version, supports, contradicts, implements, references, example-of, explains
  - `weight` (double): 0.0-1.0 relationship strength
  - `createdAt` (datetime): Relationship creation time

- **HAS_KEYWORD** (Memory → Word):
  - `relevance` (double): 0.0-1.0 keyword relevance
  - `createdAt` (datetime): Link creation time

## Basic Queries with Enhanced Context

### Get All Memories with Their Connections
```cypher
MATCH (m:Memory) 
OPTIONAL MATCH (m)-[r]-(connected)
RETURN m, r, connected
```

### Find Memories by Type with Keywords
```cypher
-- Reference documents with their keywords
MATCH (m:Memory {type: 'reference'}) 
OPTIONAL MATCH (m)-[:HAS_KEYWORD]->(w:Word)
RETURN m, w

-- How-to guides with relationships
MATCH (m:Memory {type: 'how-to'}) 
OPTIONAL MATCH (m)-[r:RELATES_TO]-(related:Memory)
RETURN m, r, related
```

## Enhanced Keyword and Tag Searches

### Find Memories by Keywords with Context
```cypher
-- Find memories about Docker/Kubernetes with connections
MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word) 
WHERE toLower(w.name) IN ['docker', 'kubernetes', 'container', 'k8s']
WITH m, COLLECT(w) as keywords
OPTIONAL MATCH (m)-[r:RELATES_TO]-(related:Memory)
RETURN m, keywords, r, related
```

### Advanced Text Search
```cypher
-- Case-insensitive search for AI-related content
MATCH (m:Memory) 
WHERE toLower(m.title) CONTAINS 'ai' 
   OR toLower(m.summary) CONTAINS 'artificial intelligence'
   OR ANY(tag IN m.tags WHERE toLower(tag) IN ['ai', 'ml', 'deep-learning'])
OPTIONAL MATCH (m)-[r]-(connected)
RETURN m, r, connected
```

### Keyword Analysis with Memory Samples
```cypher
-- Most frequent keywords with example memories
MATCH (w:Word)<-[:HAS_KEYWORD]-(m:Memory)
WITH w, COUNT(DISTINCT m) as memoryCount, COLLECT(DISTINCT m.title)[..5] as sampleTitles
WHERE memoryCount > 1
RETURN w, memoryCount, sampleTitles
ORDER BY w.frequency DESC, memoryCount DESC
```

## Rich Relationship Queries

### Find Specific Relationship Types with Full Context
```cypher
-- Find 'extends' relationships with keywords
MATCH (m1:Memory)-[r:RELATES_TO {type: 'extends'}]->(m2:Memory)
WITH m1, r, m2
OPTIONAL MATCH (m1)-[:HAS_KEYWORD]->(w:Word)
RETURN m1, r, m2, COLLECT(DISTINCT w) as keywords

-- Enhanced versions with timeline
MATCH (original:Memory)<-[r:RELATES_TO {type: 'enhanced-version'}]-(enhanced:Memory)
RETURN original, r, enhanced, 
       duration.between(original.createdAt, enhanced.createdAt) as timeDiff
ORDER BY enhanced.createdAt DESC
```

### Hub Node Discovery
```cypher
-- Find hub memories (highly connected)
MATCH (m:Memory)
WITH m, 
     SIZE([(m)-[:HAS_KEYWORD]->() | 1]) as keywordCount,
     SIZE([(m)-[:RELATES_TO]-() | 1]) as relationCount
WHERE keywordCount > 5 OR relationCount > 3
RETURN m, keywordCount, relationCount, 
       (keywordCount + relationCount * 2) as hubScore
ORDER BY hubScore DESC
```

### Knowledge Network Analysis
```cypher
-- Find memory clusters with shared keywords
MATCH (m1:Memory)-[:HAS_KEYWORD]->(w:Word)<-[:HAS_KEYWORD]-(m2:Memory)
WHERE id(m1) < id(m2)
WITH m1, m2, COLLECT(DISTINCT w.name) as shared_keywords, COUNT(DISTINCT w) as keyword_count
WHERE keyword_count > 2
RETURN m1, m2, shared_keywords, keyword_count
ORDER BY keyword_count DESC
```

## Advanced Graph Patterns

### Knowledge Evolution Tracking
```cypher
-- Track how knowledge evolves over time
MATCH path = (original:Memory)-[r:RELATES_TO*1..4]->(evolved:Memory)
WHERE ALL(rel IN relationships(path) 
      WHERE rel.type IN ['extends', 'enhanced-version', 'implements'])
  AND original.createdAt < evolved.createdAt
RETURN path, 
       [n IN nodes(path) | n.title] as evolution,
       duration.between(original.createdAt, evolved.createdAt) as totalTime
```

### Reference Documents with Implementation Examples
```cypher
-- Find references with their practical implementations
MATCH (ref:Memory {type: 'reference'})<-[r1:RELATES_TO {type: 'example-of'}]-(example:Memory)
WITH ref, COLLECT(example) as examples
OPTIONAL MATCH (ref)<-[r2:RELATES_TO {type: 'implements'}]-(impl:Memory)
RETURN ref, examples, COLLECT(impl) as implementations
```

### Cross-Domain Knowledge Connections
```cypher
-- Find memories that bridge different domains
MATCH (m:Memory)-[:HAS_KEYWORD]->(w1:Word),
      (m)-[:HAS_KEYWORD]->(w2:Word)
WHERE w1.name IN ['docker', 'kubernetes'] 
  AND w2.name IN ['ai', 'ml', 'deep-learning']
RETURN m as bridge, COLLECT(DISTINCT w1.name) + COLLECT(DISTINCT w2.name) as domains
```

## Context-Aware Queries

### High-Confidence Memory Networks
```cypher
-- Find networks of high-confidence memories
MATCH (m:Memory)
WHERE m.confidence >= 0.9
OPTIONAL MATCH (m)-[r:RELATES_TO]-(related:Memory)
WHERE related.confidence >= 0.8
RETURN m, COLLECT(DISTINCT related) as highConfidenceNetwork
ORDER BY m.createdAt DESC
```

### Recent Activity with Relationships
```cypher
-- Recent memories with their connections
MATCH (m:Memory)
WHERE m.createdAt > datetime() - duration('P7D')
OPTIONAL MATCH (m)-[r]-(connected)
RETURN m, COLLECT(DISTINCT {node: connected, type: type(r)}) as connections
ORDER BY m.createdAt DESC
```

### Source-based Analysis with Connections
```cypher
-- LLM-generated memories with their network
MATCH (m:Memory {source: 'LLM'})
OPTIONAL MATCH (m)-[r]-(connected:Memory)
WITH m, COLLECT(DISTINCT connected) as connections
RETURN m, connections, SIZE(connections) as connectionCount
ORDER BY connectionCount DESC, m.createdAt DESC
```

## Statistical Analysis Queries

### Comprehensive Graph Statistics
```cypher
-- Overall graph statistics
MATCH (m:Memory)
WITH COUNT(DISTINCT m) as totalMemories
MATCH (w:Word)
WITH totalMemories, COUNT(DISTINCT w) as totalWords
MATCH ()-[r:RELATES_TO]->()
WITH totalMemories, totalWords, COUNT(r) as totalRelationships
MATCH ()-[k:HAS_KEYWORD]->()
RETURN totalMemories, totalWords, totalRelationships, COUNT(k) as totalKeywordLinks
```

### Relationship Type Analysis
```cypher
-- Analyze relationship type distribution with examples
MATCH (m1:Memory)-[r:RELATES_TO]->(m2:Memory)
WITH r.type as relType, 
     COUNT(*) as count,
     COLLECT(DISTINCT [m1.title, m2.title])[..3] as examples
RETURN relType, count, examples
ORDER BY count DESC
```

### Memory Growth Timeline
```cypher
-- Track memory creation over time with types
MATCH (m:Memory)
WITH date(m.createdAt) as day, m.type as type
RETURN day, type, COUNT(*) as count
ORDER BY day DESC, count DESC
```

## Natural Language Query Examples (Enhanced)

The improved LLM-powered search now understands complex queries:

### Basic Searches
- "Find all reference documents"
- "Show how-to guides about Docker"
- "Get system memories"

### Relationship Searches
- "Find memories that extend DDD concepts"
- "Show enhanced versions of existing memories"
- "Find examples of design patterns"
- "Show memories that support each other"

### Complex Analysis
- "Find the most connected memories"
- "Show hub nodes in the graph"
- "Find memory clusters about AI"
- "Show knowledge evolution chains"

### Keyword and Tag Searches
- "Find memories about Docker or Kubernetes"
- "Show memories related to reactive programming"
- "Find SSE or Server-Sent Events implementations"
- "Show the most frequently used keywords"

### Advanced Queries
- "Find reference documents with practical examples"
- "Show recent high-confidence memories with their connections"
- "Find memories that bridge different domains"
- "Show LLM-generated memories with their network"

## Tips for Effective Queries

### Performance Optimization
1. **Use indexes**: Memory.id and Word.name are indexed for performance
2. **Limit path length**: Use *1..3 instead of unbounded paths
3. **Use WITH for staging**: Break complex queries into stages with WITH
4. **Profile queries**: Use PROFILE prefix to analyze query performance

### Query Writing Best Practices
1. **Case-insensitive searches**: Use toLower() for text comparisons
2. **DISTINCT for uniqueness**: Prevent duplicate results with DISTINCT
3. **OPTIONAL MATCH for nullable relationships**: Don't lose nodes without relationships
4. **Collect for aggregation**: Use COLLECT() to group related data
5. **Order and limit**: Always ORDER BY before LIMIT for consistent results

### UI Integration Tips
1. **No manual LIMIT needed**: UI applies limit from dropdown automatically
2. **Return paths for visualization**: Return full paths for better graph display
3. **Include relationships**: Always return relationships for richer visualization
4. **Use meaningful aliases**: Name your return values for clarity

## Common Relationship Types (Enhanced)

### Content Evolution
- `extends`: Builds upon or extends another memory
- `enhanced-version`: Improved or updated version
- `supersedes`: Replaces an older memory

### Knowledge Structure
- `implements`: Practical implementation of concept
- `example-of`: Concrete example of abstract concept
- `explains`: Detailed explanation of another memory

### Logical Connections
- `supports`: Validates or reinforces another memory
- `contradicts`: Conflicting or opposing information
- `references`: Citation or reference to another memory
- `related-to`: General topical relationship

### Organizational
- `part-of`: Component of a larger concept
- `depends-on`: Requires another memory for context

## API Usage Examples

### Graph Search Endpoint
```bash
# Natural language search
curl -X POST http://localhost:5000/api/graph/search \
  -H "Content-Type: application/json" \
  -d '{"query": "Find the most connected memories"}'

# Direct Cypher query
curl -X POST http://localhost:5000/api/graph/search/cypher \
  -H "Content-Type: application/json" \
  -d '{"query": "MATCH (m:Memory)-[r]-(n) RETURN m, r, n LIMIT 50"}'
```

### MCP Tool Usage
```
# Use the enhanced SearchGraph tool
SearchGraph("Find reference documents with examples")

# Returns structured results with:
# - Hub nodes highlighted
# - Connection counts included
# - Relationship subtypes shown
# - Summary statistics provided
```

## Troubleshooting

### Common Issues
1. **No results returned**: Check case sensitivity, use toLower()
2. **Slow queries**: Add indexes, limit path length, use PROFILE
3. **Duplicate results**: Add DISTINCT to your RETURN clause
4. **Missing relationships**: Use OPTIONAL MATCH instead of MATCH

### Debug Queries
```cypher
-- Check if data exists
MATCH (m:Memory) RETURN COUNT(m) as memoryCount

-- Verify relationships
MATCH ()-[r:RELATES_TO]->() RETURN DISTINCT r.type, COUNT(*) as count

-- Check keywords
MATCH (w:Word) RETURN w.name, w.frequency ORDER BY w.frequency DESC LIMIT 10
```