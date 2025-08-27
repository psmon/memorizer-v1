# Test Commands Reference Guide

## Environment Setup Commands

### 1. Docker Container Management

#### Check Running Containers
```bash
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

#### Neo4j Container Commands
```bash
# Access Neo4j shell
docker exec -it memorizer-neo4j cypher-shell -u neo4j -p password

# Clear Neo4j database
docker exec memorizer-neo4j cypher-shell -u neo4j -p password "MATCH (n) DETACH DELETE n"

# Count nodes
docker exec memorizer-neo4j cypher-shell -u neo4j -p password "MATCH (n) RETURN COUNT(n)"

# Check node types
docker exec memorizer-neo4j cypher-shell -u neo4j -p password \
  "MATCH (n) RETURN labels(n)[0] as NodeType, COUNT(n) as Count ORDER BY NodeType"

# Check relationships
docker exec memorizer-neo4j cypher-shell -u neo4j -p password \
  "MATCH ()-[r]->() RETURN type(r) as RelType, COUNT(r) as Count ORDER BY Count DESC"
```

#### PostgreSQL Container Commands
```bash
# Access PostgreSQL
docker exec -it memorizer-postgres psql -U postgres -d postgmem

# Check memory count
docker exec memorizer-postgres psql -U postgres -d postgmem -c "SELECT COUNT(*) FROM memories"

# View memory types
docker exec memorizer-postgres psql -U postgres -d postgmem -c \
  "SELECT type, COUNT(*) FROM memories GROUP BY type"
```

### 2. Application Startup

#### Local Development with dotnet run
```bash
# Set environment variable and run
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
~/.dotnet/dotnet run --urls "http://localhost:5012"

# Run in background
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 && \
~/.dotnet/dotnet run --urls "http://localhost:5012" &
```

#### Docker Build and Run
```bash
# Build image
docker build -t memorizer-app -f src/Memorizer/Dockerfile .

# Run container
docker run -d --name memorizer-app \
  -p 5012:8080 \
  --network memorizer-network \
  -e ASPNETCORE_ENVIRONMENT=Development \
  memorizer-app
```

### 3. Process Management

#### Kill Process on Port
```bash
# Find process using port 5012
netstat -tulpn 2>/dev/null | grep :5012

# Kill process by PID
kill -9 [PID]

# Force kill all processes on port
lsof -ti:5012 | xargs -r kill -9  # Requires lsof
fuser -k 5012/tcp                  # Alternative with fuser
```

## API Testing Commands

### 1. Graph Synchronization

#### Full Sync with Schema Initialization
```bash
curl -X POST http://localhost:5012/api/graph/sync \
  -H "Content-Type: application/json" \
  -d '{"fullSync": true, "initializeSchema": true}' \
  -v
```

#### Incremental Sync
```bash
curl -X POST http://localhost:5012/api/graph/sync \
  -H "Content-Type: application/json" \
  -d '{"fullSync": false, "pageSize": 50}'
```

### 2. Graph Search Queries

#### Simple Memory Query
```bash
curl -X POST http://localhost:5012/api/graph/search/cypher \
  -H "Content-Type: application/json" \
  -d '{"query": "MATCH (m:Memory) RETURN m LIMIT 5"}' | jq .
```

#### Query with Relationships
```bash
curl -X POST http://localhost:5012/api/graph/search/cypher \
  -H "Content-Type: application/json" \
  -d '{"query": "MATCH path = (m:Memory)-[r]->(n) RETURN path LIMIT 5"}' | jq .
```

#### Memory to Memory Relationships
```bash
curl -X POST http://localhost:5012/api/graph/search/cypher \
  -H "Content-Type: application/json" \
  -d '{"query": "MATCH (m1:Memory)-[r:RELATES_TO]->(m2:Memory) RETURN m1, r, m2"}' | jq .
```

#### Find Hub Nodes
```bash
curl -X POST http://localhost:5012/api/graph/search/cypher \
  -H "Content-Type: application/json" \
  -d '{"query": "MATCH (n)-[r]-() WITH n, COUNT(r) as connections WHERE connections > 3 RETURN n, connections ORDER BY connections DESC"}' | jq .
```

### 3. Natural Language Search

#### Basic Natural Language Query
```bash
curl -X POST http://localhost:5012/api/graph/search \
  -H "Content-Type: application/json" \
  -d '{"query": "show memories about reactive streams"}' | jq .
```

### 4. Memory API

#### List Memories
```bash
curl http://localhost:5012/api/memory?limit=10 | jq .
```

#### Get Specific Memory
```bash
curl http://localhost:5012/api/memory/[MEMORY_ID] | jq .
```

### 5. Health Checks

#### Application Health
```bash
curl http://localhost:5012/healthz
```

#### Check UI Access
```bash
curl -s http://localhost:5012/ui/graph | head -20
```

## Debugging Commands

### 1. Log Monitoring

#### Follow Application Logs
```bash
# If running with dotnet run
journalctl -f | grep Memorizer

# Docker logs
docker logs -f memorizer-app

# Filter for specific components
docker logs memorizer-app 2>&1 | grep -i "graph\|sync"
```

### 2. Network Debugging

#### Check Port Availability
```bash
netstat -tulpn 2>/dev/null | grep :5012
ss -tulpn | grep :5012  # Alternative
```

#### Test Connectivity
```bash
# Test Neo4j connection
nc -zv localhost 7687

# Test PostgreSQL connection
nc -zv localhost 5432

# Test application
curl -I http://localhost:5012
```

### 3. Database Inspection

#### Neo4j Web Interface
```bash
# Open in browser
xdg-open http://localhost:7474
# Login: neo4j / password
```

#### Quick Database Stats
```bash
# Neo4j stats
echo "MATCH (n) RETURN COUNT(n) as nodes; MATCH ()-[r]->() RETURN COUNT(r) as relationships;" | \
  docker exec -i memorizer-neo4j cypher-shell -u neo4j -p password

# PostgreSQL stats
echo "SELECT 'Memories:' as type, COUNT(*) FROM memories 
      UNION ALL 
      SELECT 'Relationships:', COUNT(*) FROM memory_relationships;" | \
  docker exec -i memorizer-postgres psql -U postgres -d postgmem
```

## Performance Testing

### 1. Response Time Measurement

#### Simple Timing
```bash
time curl -X POST http://localhost:5012/api/graph/search/cypher \
  -H "Content-Type: application/json" \
  -d '{"query": "MATCH (n) RETURN n LIMIT 100"}' > /dev/null
```

#### Detailed Timing with curl
```bash
curl -w "@curl-format.txt" -o /dev/null -s \
  -X POST http://localhost:5012/api/graph/search/cypher \
  -H "Content-Type: application/json" \
  -d '{"query": "MATCH (n) RETURN n LIMIT 100"}'
```

Create `curl-format.txt`:
```
time_namelookup:  %{time_namelookup}s\n
time_connect:  %{time_connect}s\n
time_appconnect:  %{time_appconnect}s\n
time_pretransfer:  %{time_pretransfer}s\n
time_redirect:  %{time_redirect}s\n
time_starttransfer:  %{time_starttransfer}s\n
time_total:  %{time_total}s\n
```

### 2. Load Testing

#### Multiple Concurrent Requests
```bash
# Using GNU parallel
seq 1 10 | parallel -j 5 \
  'curl -s -X POST http://localhost:5012/api/graph/search/cypher \
   -H "Content-Type: application/json" \
   -d "{\"query\": \"MATCH (m:Memory) RETURN m LIMIT 1\"}" > /dev/null && echo "Request {} completed"'
```

## Utility Scripts

### Complete Test Suite
```bash
#!/bin/bash
# test-suite.sh

echo "=== GraphDB Integration Test Suite ==="

# 1. Check services
echo "1. Checking services..."
docker ps | grep -E "neo4j|postgres|memorizer" || echo "Services not running!"

# 2. Clear Neo4j
echo "2. Clearing Neo4j..."
docker exec memorizer-neo4j cypher-shell -u neo4j -p password "MATCH (n) DETACH DELETE n"

# 3. Sync data
echo "3. Syncing data..."
curl -X POST http://localhost:5012/api/graph/sync \
  -H "Content-Type: application/json" \
  -d '{"fullSync": true, "initializeSchema": true}'

sleep 3

# 4. Verify sync
echo "4. Verifying sync..."
docker exec memorizer-neo4j cypher-shell -u neo4j -p password \
  "MATCH (n) RETURN labels(n)[0] as NodeType, COUNT(n) as Count"

# 5. Test search
echo "5. Testing search..."
curl -X POST http://localhost:5012/api/graph/search/cypher \
  -H "Content-Type: application/json" \
  -d '{"query": "MATCH (m:Memory) RETURN COUNT(m) as count"}'

echo "\n=== Test Suite Complete ==="
```

## Notes

1. Always ensure Docker containers are running before testing
2. Use `jq` for JSON formatting (`apt-get install jq`)
3. Check application logs when tests fail
4. Verify network connectivity between containers
5. Monitor memory and CPU usage during load tests