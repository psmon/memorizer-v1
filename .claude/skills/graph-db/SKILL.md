---
name: graph-db
description: Memorizer 프로젝트의 Neo4j 그래프 DB 활용 스킬. Cypher 쿼리 작성, 메모리 노드 관계 생성, 그래프 동기화, 자연어 → Cypher 변환이 필요할 때 사용. IGraphRepository, GraphSyncActor 패턴 참조.
---

# 그래프 DB 활용 스킬

Memorizer 프로젝트에서 Neo4j 그래프 데이터베이스를 활용하는 방법을 안내합니다.

## 프로젝트 구조

```
src/Memorizer/
├── Services/
│   ├── GraphRepository.cs        # Neo4j 저장소
│   ├── GraphSearchService.cs     # 그래프 검색 서비스
│   ├── GraphSyncService.cs       # 동기화 서비스
│   └── Neo4jDriverFactory.cs     # 드라이버 팩토리
├── Actors/
│   ├── GraphSyncActor.cs         # 그래프 동기화 액터
│   └── GraphRelationshipActor.cs # 관계 생성 액터
├── Controllers/
│   ├── GraphController.cs        # 그래프 API
│   ├── GraphViewController.cs    # 그래프 뷰
│   └── GraphSyncController.cs    # 동기화 API
├── Models/
│   └── GraphModels.cs            # 그래프 모델
└── Settings/
    └── Neo4jSettings.cs          # Neo4j 설정
```

## 그래프 스키마

### 노드 타입

```cypher
// Memory 노드
(:Memory {
    id: "UUID",
    type: "reference|how-to|system|conversation|document",
    source: "LLM|user|system",
    title: "제목",
    summary: "요약 텍스트",
    tags: ["tag1", "tag2"],
    confidence: 0.95,
    createdAt: "ISO datetime",
    updatedAt: "ISO datetime"
})

// Word 노드 (키워드)
(:Word {
    name: "keyword",
    language: "ko|en",
    frequency: 10,
    createdAt: "ISO datetime",
    updatedAt: "ISO datetime"
})
```

### 관계 타입

```cypher
// Memory 간 관계
(m1:Memory)-[:RELATES_TO {
    type: "extends|enhanced-version|supports|contradicts|implements|references|related-to|example-of|explains",
    weight: 0.8,
    createdAt: "ISO datetime"
}]->(m2:Memory)

// Memory-Word 관계
(m:Memory)-[:HAS_KEYWORD {
    relevance: 0.9,
    createdAt: "ISO datetime"
}]->(w:Word)
```

## IGraphRepository 인터페이스

```csharp
public interface IGraphRepository
{
    // 읽기 트랜잭션
    Task<T> ExecuteReadAsync<T>(Func<IAsyncQueryRunner, Task<T>> func, string? database = null);

    // 쓰기 트랜잭션
    Task<T> ExecuteWriteAsync<T>(Func<IAsyncQueryRunner, Task<T>> func, string? database = null);
    Task ExecuteWriteAsync(Func<IAsyncQueryRunner, Task> func, string? database = null);

    // Cypher 쿼리 실행
    Task<List<IRecord>> RunQueryAsync(string cypher, object? parameters = null, bool isWrite = false);

    // 연결 테스트
    Task<bool> TestConnectionAsync();
}
```

## 설정 방법

### appsettings.json

```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "User": "neo4j",
    "Password": "your-password",
    "Database": "neo4j"
  }
}
```

## 사용 예시

### 1. 기본 Cypher 쿼리

```csharp
public class MyGraphService
{
    private readonly IGraphRepository _graphRepository;

    public async Task<List<GraphMemory>> GetRelatedMemories(Guid memoryId)
    {
        var cypher = @"
            MATCH (m:Memory {id: $id})-[r:RELATES_TO]->(related:Memory)
            RETURN related, r.type as relationType, r.weight as weight
            ORDER BY r.weight DESC
            LIMIT 10
        ";

        var records = await _graphRepository.RunQueryAsync(cypher,
            new { id = memoryId.ToString() });

        return records.Select(r => new GraphMemory
        {
            Id = Guid.Parse(r["related"].As<INode>()["id"].As<string>()),
            Title = r["related"].As<INode>()["title"].As<string>(),
            RelationType = r["relationType"].As<string>(),
            Weight = r["weight"].As<double>()
        }).ToList();
    }
}
```

### 2. 관계 생성

```csharp
public async Task CreateRelationship(Guid fromId, Guid toId, string type)
{
    var cypher = @"
        MATCH (from:Memory {id: $fromId})
        MATCH (to:Memory {id: $toId})
        MERGE (from)-[r:RELATES_TO {type: $type}]->(to)
        ON CREATE SET r.createdAt = datetime(), r.weight = 1.0
        ON MATCH SET r.weight = r.weight + 0.1
        RETURN r
    ";

    await _graphRepository.RunQueryAsync(cypher, new
    {
        fromId = fromId.ToString(),
        toId = toId.ToString(),
        type
    }, isWrite: true);
}
```

### 3. 키워드 기반 검색

```csharp
public async Task<List<GraphMemory>> SearchByKeywords(List<string> keywords)
{
    var cypher = @"
        MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word)
        WHERE w.name IN $keywords
        WITH m, COUNT(DISTINCT w) as matchCount
        WHERE matchCount >= 2
        RETURN m, matchCount
        ORDER BY matchCount DESC
        LIMIT 20
    ";

    var records = await _graphRepository.RunQueryAsync(cypher,
        new { keywords });

    return MapToGraphMemories(records);
}
```

### 4. 그래프 경로 탐색

```csharp
public async Task<List<GraphPath>> FindPaths(Guid startId, Guid endId, int maxDepth = 3)
{
    var cypher = @"
        MATCH path = shortestPath(
            (start:Memory {id: $startId})-[:RELATES_TO*1.." + maxDepth + @"]-(end:Memory {id: $endId})
        )
        RETURN path
        LIMIT 5
    ";

    var records = await _graphRepository.RunQueryAsync(cypher, new
    {
        startId = startId.ToString(),
        endId = endId.ToString()
    });

    return MapToGraphPaths(records);
}
```

## 자연어 → Cypher 변환 패턴

LLM을 활용한 자연어 쿼리 변환:

```csharp
public async Task<string> GenerateCypherFromNaturalLanguage(string query)
{
    var prompt = CreateGraphQueryPrompt(query);
    var cypher = await _llmService.CompleteAsync(prompt);
    return ValidateAndSanitizeCypher(cypher);
}

private string CreateGraphQueryPrompt(string query)
{
    return @$"
당신은 Neo4j Cypher 쿼리 전문가입니다.

## 그래프 스키마
- Memory 노드: id, type, title, summary, tags, confidence
- Word 노드: name, language, frequency
- RELATES_TO 관계: type, weight
- HAS_KEYWORD 관계: relevance

## 관계 타입
extends, enhanced-version, supports, contradicts, implements, references, related-to, example-of, explains

## 사용자 쿼리
{query}

## 지침
1. 읽기 전용 쿼리만 생성
2. LIMIT 절 필수 포함 (최대 100)
3. ORDER BY로 결과 정렬
4. 유효한 Cypher 문법 사용

Cypher 쿼리:
";
}
```

## GraphSyncActor 패턴

PostgreSQL 데이터를 Neo4j로 동기화:

```csharp
public class GraphSyncActor : ReceiveActor
{
    public GraphSyncActor(IGraphSyncService syncService)
    {
        // 전체 동기화
        ReceiveAsync<SyncAllRequest>(async msg =>
        {
            await syncService.SyncAllMemoriesToGraphAsync();
            Sender.Tell(new SyncCompleteResponse());
        });

        // 단일 메모리 동기화
        ReceiveAsync<SyncMemoryRequest>(async msg =>
        {
            await syncService.SyncMemoryToGraphAsync(msg.MemoryId);
            Sender.Tell(new SyncCompleteResponse());
        });

        // 관계 생성
        ReceiveAsync<CreateRelationshipRequest>(async msg =>
        {
            await syncService.CreateRelationshipAsync(msg.FromId, msg.ToId, msg.Type);
            Sender.Tell(new RelationshipCreatedResponse());
        });
    }
}
```

## 자주 사용하는 Cypher 패턴

### 허브 노드 찾기
```cypher
MATCH (m:Memory)
WITH m, SIZE([(m)-[]-() | 1]) as degree
WHERE degree > 3
RETURN m, degree
ORDER BY degree DESC
LIMIT 20
```

### 공통 키워드 기반 연관 메모리
```cypher
MATCH (m1:Memory)-[:HAS_KEYWORD]->(w:Word)<-[:HAS_KEYWORD]-(m2:Memory)
WHERE id(m1) < id(m2)
WITH m1, m2, COLLECT(DISTINCT w.name) as commonKeywords
WHERE SIZE(commonKeywords) > 2
RETURN m1, m2, commonKeywords
LIMIT 50
```

### 최근 고신뢰도 메모리
```cypher
MATCH (m:Memory)
WHERE m.confidence > 0.8
  AND m.createdAt > datetime() - duration('P30D')
RETURN m
ORDER BY m.createdAt DESC
LIMIT 30
```

### 확장 관계 탐색
```cypher
MATCH (m1:Memory)-[r:RELATES_TO {type: 'extends'}]->(m2:Memory)
RETURN m1, r, m2
ORDER BY r.createdAt DESC
LIMIT 30
```

## 주의사항

1. **쓰기 vs 읽기**: 쓰기 작업은 `isWrite: true` 명시
2. **트랜잭션**: 복잡한 작업은 `ExecuteWriteAsync` 사용
3. **파라미터화**: SQL 인젝션 방지를 위해 항상 파라미터 사용
4. **LIMIT**: 대량 결과 방지를 위해 항상 LIMIT 포함
5. **인덱스**: 자주 조회하는 속성에 인덱스 생성
