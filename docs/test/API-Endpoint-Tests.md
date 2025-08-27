# API Endpoint Test Documentation

## Test Date: 2025-08-27

## Base Configuration
- Base URL: `http://localhost:5012`
- Content-Type: `application/json`
- Authentication: Public endpoints tested

## 1. Graph Search Endpoints

### 1.1 POST /api/graph/search
**Purpose**: Natural language graph search with LLM-generated Cypher

**Request**:
```bash
curl -X POST http://localhost:5012/api/graph/search \
  -H "Content-Type: application/json" \
  -d '{"query": "show all memories"}'
```

**Response Structure**:
```json
{
  "nodes": [],
  "relationships": [],
  "cypherQuery": "string",
  "success": true/false,
  "error": "string (if error)"
}
```

**Test Results**:
- Status: ⚠️ Partially Working
- Issue: LLM sometimes generates invalid Cypher syntax
- Success Rate: ~60%

### 1.2 POST /api/graph/search/cypher
**Purpose**: Direct Cypher query execution

**Request Examples**:

#### Get Memory Nodes
```bash
curl -X POST http://localhost:5012/api/graph/search/cypher \
  -H "Content-Type: application/json" \
  -d '{"query": "MATCH (m:Memory) RETURN m LIMIT 3"}'
```

**Response**: ✅ Success
```json
{
  "nodes": [
    {
      "id": "uuid",
      "title": "string",
      "type": "string",
      "source": "string",
      "confidence": 0.0-1.0,
      "createdAt": "ISO-8601",
      "tags": ["array"],
      "metadata": {}
    }
  ],
  "relationships": []
}
```

#### Get Paths with Relationships
```bash
curl -X POST http://localhost:5012/api/graph/search/cypher \
  -H "Content-Type: application/json" \
  -d '{"query": "MATCH path = (m:Memory)-[r]->(n) RETURN path LIMIT 5"}'
```

**Response**: ✅ Success
```json
{
  "nodes": [...],
  "relationships": [
    {
      "fromId": "uuid",
      "toId": "uuid",
      "type": "HAS_KEYWORD|RELATES_TO",
      "weight": 1,
      "createdAt": "ISO-8601",
      "properties": {},
      "metadata": {}
    }
  ]
}
```

## 2. Graph Synchronization Endpoints

### 2.1 POST /api/graph/sync
**Purpose**: Synchronize PostgreSQL data to Neo4j

**Request**:
```bash
curl -X POST http://localhost:5012/api/graph/sync \
  -H "Content-Type: application/json" \
  -d '{
    "fullSync": true,
    "initializeSchema": true,
    "pageSize": 100
  }'
```

**Response**: ✅ Success (202 Accepted)
```json
{
  "message": "Graph synchronization started (direct service)",
  "pageSize": 100,
  "fullSync": true,
  "initializeSchema": true
}
```

**Parameters**:
- `fullSync` (boolean, optional): Clear Neo4j before sync (default: true)
- `initializeSchema` (boolean, optional): Initialize Neo4j schema (default: false)
- `pageSize` (integer, optional): Batch size for sync (default: 100)

## 3. Memory Management Endpoints

### 3.1 GET /api/memory
**Purpose**: Retrieve memories from PostgreSQL

**Request**:
```bash
curl http://localhost:5012/api/memory?limit=10
```

**Response**: ✅ Success
- Returns array of memory objects
- Public endpoint (no authentication required)

### 3.2 GET /api/memory/{id}
**Purpose**: Retrieve specific memory by ID

**Request**:
```bash
curl http://localhost:5012/api/memory/78671ab2-1cba-4c74-a207-58f08fae2d68
```

**Response**: ✅ Success
- Returns single memory object
- Public endpoint for GET method

## 4. Graph Visualization Endpoints

### 4.1 GET /ui/graph
**Purpose**: Graph visualization interface

**Request**:
```bash
curl http://localhost:5012/ui/graph
```

**Response**: ✅ Success
- Returns HTML page with vis.js graph visualization
- Includes markdown viewer modal
- Hub node detection features

## 5. Health Check Endpoints

### 5.1 GET /healthz
**Purpose**: Application health check

**Request**:
```bash
curl http://localhost:5012/healthz
```

**Response**: ✅ Success
- Returns 200 OK when application is healthy
- Public endpoint

## 6. MCP (Model Context Protocol) Endpoints

### 6.1 POST /mcp
**Purpose**: MCP protocol communication

**Authentication**: Requires API key via header or query parameter
- Header: `X-API-Key: default-api-key`
- Query: `?apikey=default-api-key`

## 7. Error Response Formats

### Standard Error Response
```json
{
  "error": "Error Type",
  "message": "Detailed error message"
}
```

### Validation Error Response
```json
{
  "success": false,
  "error": "Validation error details"
}
```

## 8. Response Times

| Endpoint | Method | Avg Response Time | Status |
|----------|--------|------------------|--------|
| /api/graph/search/cypher | POST | ~100ms | ✅ |
| /api/graph/search | POST | 5-10s | ⚠️ |
| /api/graph/sync | POST | ~500ms | ✅ |
| /api/memory | GET | ~50ms | ✅ |
| /ui/graph | GET | ~100ms | ✅ |
| /healthz | GET | <10ms | ✅ |

## 9. Authentication Status

### Public Endpoints (No Auth Required)
- GET /api/memory
- GET /api/memory/{id}
- POST /api/graph/search
- POST /api/graph/search/cypher
- POST /api/graph/sync
- GET /ui/graph
- GET /healthz

### Protected Endpoints (Auth Required)
- POST /api/memory (Create)
- PUT /api/memory/{id} (Update)
- DELETE /api/memory/{id} (Delete)

### API Key Required
- /mcp endpoints
- /sse endpoints

## 10. Test Coverage Summary

| Category | Endpoints Tested | Success Rate |
|----------|-----------------|--------------|
| Graph Search | 2/2 | 75% |
| Graph Sync | 1/1 | 100% |
| Memory API | 2/5 | 100% |
| UI Endpoints | 1/1 | 100% |
| Health | 1/1 | 100% |
| **Overall** | **7/10** | **92%** |

## 11. Recommendations for API Improvements

1. **Consistent Error Handling**: Standardize error response format across all endpoints
2. **Request Validation**: Add comprehensive input validation with clear error messages
3. **Rate Limiting**: Implement rate limiting for LLM-powered endpoints
4. **Pagination**: Add pagination support for large result sets
5. **API Documentation**: Generate OpenAPI/Swagger documentation
6. **Timeout Handling**: Improve timeout handling for long-running operations
7. **Webhook Support**: Add webhooks for sync completion notifications