# MCP (Model Context Protocol) Guide for Memorizer

This guide provides comprehensive documentation for using the Memorizer MCP tools to manage, search, and analyze memory storage with graph database capabilities.

## Table of Contents
- [Overview](#overview)
- [Available Tools](#available-tools)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)
- [Advanced Patterns](#advanced-patterns)

## Overview

Memorizer provides a powerful set of MCP tools for memory management with both vector-based similarity search and graph-based relationship analysis. The system combines PostgreSQL with pgvector for similarity search and Neo4j for complex relationship traversal.

### Key Features
- **Hybrid Search**: Combine vector similarity with graph relationships
- **Knowledge Graph**: Automatically builds relationships between memories
- **Multi-format Support**: Store reference docs, how-tos, conversations, and more
- **Intelligent Tagging**: Automatic keyword extraction and tagging
- **Confidence Scoring**: Track reliability of stored information

## Available Tools

### 1. Store - Save New Memories
Store any type of information with automatic embedding and relationship detection.

**Parameters:**
- `type`: Memory type (reference, how-to, system, conversation, document)
- `text`: Content to store (markdown, code, prose)
- `source`: Origin of memory (LLM, user, system)
- `title`: Descriptive title (required)
- `tags`: Optional categorization tags
- `confidence`: Reliability score (0.0-1.0)
- `relatedTo`: Link to existing memory ID
- `relationshipType`: Type of relationship

**Example Prompt:**
```
Store this Docker best practices guide as a reference document with high confidence:
[Your Docker content here]
```

### 2. Search - Find Similar Memories
Vector-based similarity search with optional tag filtering.

**Parameters:**
- `query`: Natural language search query
- `limit`: Maximum results (default: 10)
- `minSimilarity`: Minimum similarity threshold (0.0-1.0)
- `filterTags`: Optional tag filters

**Example Prompts:**
```
Search for memories about containerization and orchestration

Find all how-to guides related to React with minimum 80% similarity

Search for high-confidence reference documents about domain-driven design
```

### 3. SearchGraph - Natural Language Graph Search
Execute complex graph queries using natural language.

**Supported Query Types:**

#### Memory Type Searches
```
Find all reference documents
Show how-to guides about testing
Get system configuration memories
```

#### Keyword and Tag Searches
```
Find memories about Docker or Kubernetes
Show memories related to AI and machine learning
Search for reactive programming patterns
```

#### Relationship Exploration
```
Find memories that extend DDD concepts
Show enhanced versions of existing memories
Find examples of design patterns
Show memories that support microservices architecture
```

#### Advanced Analysis
```
Find the most connected memories (hub nodes)
Show most frequent keywords in the knowledge base
Find memories with common keywords about security
Recent high-confidence memories from the last 30 days
```

#### Graph Patterns
```
Find reference documents with practical examples
Show knowledge clusters about cloud computing
Find hub memories with many connections
Trace relationship chains from concepts to implementations
```

### 4. SearchGraphByCypher - Direct Cypher Queries
Execute raw Cypher queries for advanced graph analysis.

**Example Cypher Queries:**

```cypher
# Find all memories with more than 5 relationships
MATCH (m:Memory)
WITH m, SIZE([(m)-[]-() | 1]) as degree
WHERE degree > 5
RETURN m, degree ORDER BY degree DESC LIMIT 20

# Find memory evolution chains
MATCH path = (m1:Memory)-[:RELATES_TO {type: 'enhanced-version'}*1..3]->(m2:Memory)
RETURN path LIMIT 10

# Analyze keyword co-occurrence
MATCH (m:Memory)-[:HAS_KEYWORD]->(w1:Word)
MATCH (m)-[:HAS_KEYWORD]->(w2:Word)
WHERE w1.name < w2.name
WITH w1, w2, COUNT(DISTINCT m) as cooccurrence
WHERE cooccurrence > 3
RETURN w1.name, w2.name, cooccurrence
ORDER BY cooccurrence DESC LIMIT 20
```

### 5. Get - Retrieve Specific Memory
Fetch a memory by its unique ID.

**Example Prompt:**
```
Get memory with ID 2b61629b-d5b4-41c2-8163-6670893df238
```

### 6. GetMany - Retrieve Multiple Memories
Fetch multiple memories by their IDs in one operation.

**Example Prompt:**
```
Get memories with IDs [id1, id2, id3] to compare their approaches
```

### 7. Delete - Remove Memory
Delete a specific memory from the database.

**Example Prompt:**
```
Delete outdated memory with ID [memory-id]
```

### 8. CreateRelationship - Link Memories
Create semantic relationships between memories.

**Relationship Types:**
- `extends`: Extends or builds upon concepts
- `enhanced-version`: Improved version of existing memory
- `supports`: Provides supporting evidence
- `contradicts`: Presents opposing view
- `implements`: Practical implementation
- `references`: Direct reference
- `related-to`: General relationship
- `example-of`: Concrete example
- `explains`: Provides explanation

**Example Prompt:**
```
Create an 'example-of' relationship from memory [example-id] to memory [pattern-id]
```

## Usage Examples

### Building a Knowledge Base

1. **Store Reference Documentation**
```
Store this comprehensive guide about microservices architecture as a reference document with tags ['architecture', 'microservices', 'distributed-systems'] and high confidence
```

2. **Add Practical Examples**
```
Store this microservices implementation example as a how-to, linking it as an 'example-of' relationship to the microservices reference
```

3. **Track Enhancements**
```
Store this updated version of the authentication guide as an 'enhanced-version' of the original guide
```

### Research and Discovery

1. **Find Related Concepts**
```
Use SearchGraph to find all memories that extend or implement OAuth 2.0 concepts
```

2. **Explore Knowledge Clusters**
```
Search for knowledge clusters about security to understand connected topics
```

3. **Trace Learning Paths**
```
Find relationship chains from basic concepts to advanced implementations in machine learning
```

### Knowledge Management

1. **Identify Key Resources**
```
Find hub memories that are highly connected and serve as central knowledge points
```

2. **Analyze Coverage**
```
Show the most frequent keywords to understand knowledge base coverage
```

3. **Quality Control**
```
Find recent high-confidence memories to review latest additions
```

## Best Practices

### 1. Structured Storage
- Use clear, descriptive titles for searchability
- Choose appropriate memory types (reference vs how-to vs example)
- Include relevant tags for better categorization
- Set accurate confidence scores based on source reliability

### 2. Relationship Management
- Create relationships immediately when storing related content
- Use specific relationship types (avoid generic 'related-to' when possible)
- Link examples to their reference documentation
- Track enhancement chains for evolving knowledge

### 3. Search Strategies
- Start with natural language SearchGraph for exploration
- Use SearchGraphByCypher for complex analytical queries
- Combine vector Search with graph traversal for comprehensive results
- Follow relationship suggestions to discover related content

### 4. Content Organization
- Store reference materials with 'reference' type
- Store practical guides as 'how-to' type
- Link implementations to their theoretical foundations
- Maintain confidence scores to track information reliability

## Advanced Patterns

### Pattern 1: Building Domain Knowledge
```
1. Store core domain concepts as reference documents
2. Add practical implementations as how-tos with 'implements' relationships
3. Store real-world examples with 'example-of' relationships
4. Create 'extends' relationships between related concepts
```

### Pattern 2: Knowledge Evolution Tracking
```
1. Store initial version of a concept
2. Store improvements as new memories with 'enhanced-version' relationships
3. Use graph queries to trace the evolution over time
4. Analyze confidence scores to track knowledge maturity
```

### Pattern 3: Cross-Domain Connections
```
1. Use keyword analysis to find memories with common concepts
2. Create 'related-to' relationships between different domains
3. Use hub node analysis to find bridge concepts
4. Explore knowledge clusters to understand domain intersections
```

### Pattern 4: Learning Path Construction
```
1. Find basic concepts using type and tag filters
2. Trace 'extends' relationships to advanced topics
3. Follow 'example-of' relationships for practical applications
4. Use confidence scores to prioritize reliable sources
```

## Performance Tips

1. **Use Appropriate Tools**
   - Use Search for simple similarity queries
   - Use SearchGraph for relationship exploration
   - Use SearchGraphByCypher only for complex analytical needs

2. **Optimize Queries**
   - Always include LIMIT clauses in Cypher queries
   - Use tag filters to narrow search scope
   - Adjust similarity thresholds based on needs

3. **Relationship Strategy**
   - Create relationships during storage when possible
   - Batch relationship creation for related content
   - Use specific relationship types for better traversal

## Troubleshooting

### Common Issues and Solutions

1. **No search results found**
   - Lower the similarity threshold
   - Use broader search terms
   - Check if content exists with different tags

2. **Graph queries returning too many results**
   - Add LIMIT clauses to Cypher queries
   - Use more specific relationship types
   - Filter by node properties

3. **Relationship creation fails**
   - Verify both memory IDs exist
   - Check relationship type is valid
   - Ensure memories are compatible for linking

## Authentication for API Access

When using the MCP tools through the API:
- Read operations: No authentication required
- Write operations: Use X-API-Key header
- SSE endpoints: Require API key authentication

## Conclusion

The Memorizer MCP tools provide a powerful framework for building and managing a knowledge graph. By combining vector similarity search with graph relationships, you can create a rich, interconnected knowledge base that supports complex queries and knowledge discovery.

Remember to:
- Store content with appropriate metadata
- Create meaningful relationships
- Use the right tool for each query type
- Maintain high-quality, well-organized knowledge

For additional support or feature requests, please refer to the project documentation or submit issues to the repository.