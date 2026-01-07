---
name: mcp-tools
description: Memorizer 프로젝트의 MCP(Model Context Protocol) 도구 구현 스킬. 새로운 MCP 도구 추가, Description 작성, 인증(ApiKey/OAuth) 처리가 필요할 때 사용. MemoryTools.cs 패턴 참조.
---

# MCP 도구 구현 스킬

Memorizer 프로젝트에서 MCP(Model Context Protocol) 도구를 구현하는 방법을 안내합니다.

## 프로젝트 구조

```
src/Memorizer/
├── Tools/
│   └── MemoryTools.cs           # MCP 도구 정의
├── Services/
│   ├── OAuthTokenService.cs     # OAuth 토큰 관리
│   └── GraphSearchService.cs    # 그래프 검색 서비스
├── Settings/
│   └── OAuthSettings.cs         # OAuth 설정
├── Controllers/
│   └── OAuthController.cs       # OAuth 엔드포인트
└── Middleware/
    └── AuthenticationMiddleware.cs
```

## MCP 도구 기본 패턴

### 도구 클래스 정의

```csharp
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace PostgMem.Tools;

[McpServerToolType]
public class MemoryTools
{
    private readonly IStorage _storage;
    private readonly ILogger<MemoryTools> _logger;
    private readonly IGraphSearchService? _graphSearchService;

    public MemoryTools(
        IStorage storage,
        ILogger<MemoryTools> logger,
        IGraphSearchService? graphSearchService = null)
    {
        _storage = storage;
        _logger = logger;
        _graphSearchService = graphSearchService;
    }

    // 도구 메서드들...
}
```

### 도구 메서드 정의

```csharp
[McpServerTool]
[Description("도구의 목적과 사용 시나리오를 상세히 설명")]
public async Task<string> ToolName(
    [Description("파라미터 설명")] string param1,
    [Description("선택적 파라미터")] string? optionalParam = null,
    CancellationToken cancellationToken = default
)
{
    // 구현
    return "결과";
}
```

## Description 작성 베스트 프랙티스

### 도구 Description

LLM이 도구를 올바르게 사용할 수 있도록 상세히 작성:

```csharp
[McpServerTool]
[Description(@"Search for memories similar to the provided text.
Use this to retrieve reference material, how-tos, or examples relevant to the current task.
Filtering by tags can help narrow down to specific types of knowledge.")]
public async Task<string> Search(...)
```

### 복잡한 도구의 Description

Markdown 형식으로 사용법, 예시, 스키마 정보 포함:

```csharp
[McpServerTool]
[Description(@"Search the graph database using natural language queries.
This uses LLM to convert natural language to Cypher queries for Neo4j graph traversal.

## Supported Query Types:
### Memory Type Searches:
- 'Find all reference documents' - retrieves memories of type 'reference'
- 'Show how-to guides' - finds all how-to type memories

### Relationship Exploration:
- 'Find memories that extend DDD concepts' - discovers extension relationships
- 'Show enhanced versions' - finds enhanced-version relationships

### Advanced Analysis:
- 'Find the most connected memories' - identifies hub nodes
- 'Show most frequent keywords' - analyzes keyword patterns

Returns nodes with properties (id, title, type, tags) and relationships.")]
public async Task<string> SearchGraph(...)
```

### 파라미터 Description

```csharp
public async Task<string> Store(
    [Description("The type of memory (e.g., 'reference', 'how-to'). Use 'reference' for reusable knowledge.")]
    string type,

    [Description("Plain text (markdown, code) to store. Use Mermaid for diagrams.")]
    string text,

    [Description("Source of the memory ('user', 'system', 'LLM'). Use 'LLM' for AI-generated knowledge.")]
    string source,

    [Description("Title for the memory. Required and must not be empty.")]
    string title,

    [Description("Optional tags for categorization (e.g., 'coding-standard', 'how-to').")]
    string[]? tags = null,

    [Description("Confidence score (0.0 to 1.0)")]
    double confidence = 1.0
)
```

## 기존 MCP 도구 목록

### Store (메모리 저장)
```csharp
Task<string> Store(
    string type,        // reference, how-to, conversation, document
    string text,        // 저장할 텍스트 (Markdown 지원)
    string source,      // user, system, LLM
    string title,       // 제목 (필수)
    string[]? tags,     // 태그 배열
    double confidence,  // 신뢰도 (0.0-1.0)
    Guid? relatedTo,    // 관련 메모리 ID
    string? relationshipType  // 관계 유형
)
```

### Search (유사도 검색)
```csharp
Task<string> Search(
    string query,           // 검색 쿼리
    int limit,              // 최대 결과 수 (기본 10)
    double minSimilarity,   // 최소 유사도 (기본 0.7)
    string[]? filterTags    // 태그 필터
)
```

### Get / GetMany (조회)
```csharp
Task<string> Get(Guid id)
Task<string> GetMany(Guid[] ids)
```

### Delete (삭제)
```csharp
Task<string> Delete(Guid id)
```

### CreateRelationship (관계 생성)
```csharp
Task<string> CreateRelationship(
    Guid fromId,
    Guid toId,
    string type  // example-of, explains, related-to, extends, etc.
)
```

### SearchGraph (자연어 그래프 검색)
```csharp
Task<string> SearchGraph(string query)
```

### SearchGraphByCypher (Cypher 직접 실행)
```csharp
Task<string> SearchGraphByCypher(string cypherQuery)
```

## 인증 설정

### ApiKey 인증

```json
// appsettings.json
{
  "Server": {
    "ApiKey": "your-api-key-here"
  }
}
```

요청 시 헤더:
```
X-API-Key: your-api-key-here
```

### OAuth 2.0 인증

```json
// appsettings.json
{
  "OAuth": {
    "ClientId": "memorizer-client",
    "ClientSecret": "your-secret",
    "TokenExpirationHours": 24,
    "JwtSecret": "your-jwt-secret-minimum-32-characters"
  }
}
```

토큰 발급:
```bash
curl -X POST http://localhost:5000/oauth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=memorizer-client&client_secret=your-secret"
```

요청 시 헤더:
```
Authorization: Bearer {token}
```

## 새 MCP 도구 추가 절차

### 1. 도구 메서드 추가

```csharp
[McpServerTool]
[Description("새 도구의 목적과 사용법")]
public async Task<string> NewTool(
    [Description("파라미터 설명")] string param,
    CancellationToken cancellationToken = default
)
{
    using var activity = TelemetryConfig.ActivitySource.StartActivity("MemoryTools.NewTool");

    activity?.AddEvent(new ActivityEvent("query.details", DateTimeOffset.UtcNow,
        new ActivityTagsCollection { {"param", param} }));

    try
    {
        // 비즈니스 로직
        var result = await _storage.DoSomething(param, cancellationToken);

        _logger.LogInformation("NewTool completed: {Param}", param);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return FormatResult(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in NewTool: {Param}", param);
        return $"Error: {ex.Message}";
    }
}
```

### 2. 결과 포맷팅

```csharp
private string FormatResult(List<Memory> memories)
{
    var sb = new StringBuilder();
    sb.AppendLine($"Found {memories.Count} memories:");
    sb.AppendLine();

    foreach (var memory in memories)
    {
        sb.AppendLine($"ID: {memory.Id}");
        sb.AppendLine($"Title: {memory.Title}");
        sb.AppendLine($"Type: {memory.Type}");
        // ...
        sb.AppendLine();
    }

    // 제안 추가
    sb.AppendLine("💡 Suggestion: Use GetMany to load related context.");

    return sb.ToString();
}
```

### 3. 서비스 의존성 추가 (필요시)

```csharp
// Program.cs
builder.Services.AddScoped<INewService, NewService>();

// MemoryTools.cs 생성자
public MemoryTools(
    IStorage storage,
    ILogger<MemoryTools> logger,
    INewService newService)  // 추가
{
    _newService = newService;
}
```

## 텔레메트리 패턴

```csharp
using var activity = TelemetryConfig.ActivitySource.StartActivity("MemoryTools.MethodName");

// 쿼리 상세 기록
activity?.AddEvent(new ActivityEvent("query.details", DateTimeOffset.UtcNow,
    new ActivityTagsCollection
    {
        {"query.text", query},
        {"query.limit", limit.ToString()}
    }));

// 결과 기록
_logger.LogInformation("Search completed. Query: {Query}, ResultCount: {Count}",
    query, results.Count);

// 성공/실패 상태
activity?.SetStatus(ActivityStatusCode.Ok, $"Found {count} results");
// 또는
activity?.SetStatus(ActivityStatusCode.Error, "Query failed");
```

## 주의사항

1. **Description 품질**: LLM이 도구를 올바르게 사용하는 핵심
2. **읽기 전용 검증**: Cypher 쿼리는 읽기 전용만 허용
3. **LIMIT 강제**: 대량 결과 방지를 위해 기본 LIMIT 추가
4. **에러 처리**: 사용자 친화적 에러 메시지 반환
5. **로깅**: 모든 작업에 로깅 추가 (디버깅, 모니터링)
6. **CancellationToken**: 모든 비동기 메서드에 전파
