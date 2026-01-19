---
name: embedding-service
description: Memorizer 프로젝트의 임베딩/벡터 검색 스킬. 텍스트 벡터화, 유사도 검색, 메타데이터 임베딩이 필요할 때 사용. IEmbeddingService 인터페이스, MetadataEmbeddingActor 패턴 참조.
---

# 임베딩/벡터 검색 스킬

Memorizer 프로젝트에서 텍스트 임베딩과 벡터 검색을 활용하는 방법을 안내합니다.

## 프로젝트 구조

```
src/Memorizer/
├── Services/
│   ├── IEmbeddingService.cs       # 임베딩 서비스 인터페이스
│   ├── OllamaEmbeddingService.cs  # Ollama 구현체
│   ├── OpenAIEmbeddingService.cs  # OpenAI 구현체
│   └── CustomEmbeddingService.cs  # Custom API 구현체
├── Settings/
│   └── EmbeddingSettings.cs       # 임베딩 설정
├── Actors/
│   └── MetadataEmbeddingActor.cs  # 비동기 임베딩 처리 액터
└── Models/
    ├── EmbeddingRequest.cs
    └── EmbeddingResponse.cs
```

## IEmbeddingService 인터페이스

```csharp
public interface IEmbeddingService
{
    // 텍스트를 벡터로 변환
    Task<float[]> Generate(
        string text,
        CancellationToken cancellationToken = default
    );

    // JSON 문서를 벡터로 변환
    Task<float[]> Generate(
        JsonDocument document,
        CancellationToken cancellationToken = default
    );

    // 벡터 차원 수 반환 (기본 384)
    int GetEmbeddingDimensions();
}
```

## 설정 방법

### appsettings.json

```json
{
  "Embedding": {
    "Type": "Ollama",              // Ollama | OpenAI | Custom
    "ApiUrl": "http://localhost:11434",
    "Model": "nomic-embed-text:latest",
    "Dimensions": 384
  }
}
```

### 환경변수 오버라이드

```yaml
# docker-compose
environment:
  MEMORIZER_Embedding__Type: Ollama
  MEMORIZER_Embedding__ApiUrl: http://host.docker.internal:11434
  MEMORIZER_Embedding__Model: nomic-embed-text:latest
```

## 사용 예시

### 1. 기본 임베딩 생성

```csharp
public class MyService
{
    private readonly IEmbeddingService _embeddingService;

    public MyService(IEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task<float[]> GetEmbedding(string text)
    {
        return await _embeddingService.Generate(text);
    }
}
```

### 2. 메모리 저장 시 임베딩

```csharp
public async Task<Memory> StoreMemory(string text, string type, string source)
{
    // 텍스트 임베딩 생성
    var embedding = await _embeddingService.Generate(text);

    var memory = new Memory
    {
        Id = Guid.NewGuid(),
        Text = text,
        Type = type,
        Source = source,
        Embedding = embedding,
        CreatedAt = DateTime.UtcNow
    };

    await _dbContext.Memories.AddAsync(memory);
    await _dbContext.SaveChangesAsync();

    return memory;
}
```

### 3. 벡터 유사도 검색 (PostgreSQL)

```csharp
public async Task<List<Memory>> SearchSimilar(string query, int limit = 10, double minSimilarity = 0.7)
{
    // 쿼리 텍스트 임베딩
    var queryEmbedding = await _embeddingService.Generate(query);

    // 코사인 유사도 검색 (pgvector)
    var sql = @"
        SELECT *, 1 - (embedding <=> @embedding::vector) as similarity
        FROM memories
        WHERE 1 - (embedding <=> @embedding::vector) >= @minSimilarity
        ORDER BY embedding <=> @embedding::vector
        LIMIT @limit
    ";

    return await _dbContext.Memories
        .FromSqlRaw(sql,
            new NpgsqlParameter("embedding", queryEmbedding),
            new NpgsqlParameter("minSimilarity", minSimilarity),
            new NpgsqlParameter("limit", limit))
        .ToListAsync();
}
```

## MetadataEmbeddingActor 패턴

메모리 저장 시 메타데이터 임베딩을 비동기로 처리:

```csharp
public class MetadataEmbeddingActor : ReceiveActor
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IStorage _storage;

    public MetadataEmbeddingActor(IEmbeddingService embeddingService, IStorage storage)
    {
        _embeddingService = embeddingService;
        _storage = storage;

        ReceiveAsync<GenerateMetadataEmbeddingRequest>(HandleGenerateEmbedding);
    }

    private async Task HandleGenerateEmbedding(GenerateMetadataEmbeddingRequest request)
    {
        try
        {
            // 제목 + 태그로 메타데이터 텍스트 생성
            var metadataText = BuildMetadataText(request.Memory);

            // 메타데이터 임베딩 생성
            var metadataEmbedding = await _embeddingService.Generate(metadataText);

            // 메모리 업데이트
            await _storage.UpdateMetadataEmbedding(request.Memory.Id, metadataEmbedding);

            Sender.Tell(new MetadataEmbeddingCompleteResponse
            {
                MemoryId = request.Memory.Id,
                Success = true
            });
        }
        catch (Exception ex)
        {
            Sender.Tell(new MetadataEmbeddingCompleteResponse
            {
                MemoryId = request.Memory.Id,
                Success = false,
                Error = ex.Message
            });
        }
    }

    private string BuildMetadataText(Memory memory)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(memory.Title))
            parts.Add(memory.Title);

        if (memory.Tags?.Length > 0)
            parts.Add(string.Join(" ", memory.Tags));

        if (!string.IsNullOrWhiteSpace(memory.Type))
            parts.Add(memory.Type);

        return string.Join(". ", parts);
    }
}
```

## 메시지 정의

```csharp
public class GenerateMetadataEmbeddingRequest
{
    public Memory Memory { get; set; } = null!;
}

public class MetadataEmbeddingCompleteResponse
{
    public Guid MemoryId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
```

## PostgreSQL 벡터 설정

### 마이그레이션 (pgvector)

```sql
-- 벡터 확장 활성화
CREATE EXTENSION IF NOT EXISTS vector;

-- 메모리 테이블에 벡터 컬럼
ALTER TABLE memories
ADD COLUMN IF NOT EXISTS embedding VECTOR(384);

-- 메타데이터 임베딩 컬럼
ALTER TABLE memories
ADD COLUMN IF NOT EXISTS metadata_embedding VECTOR(384);

-- IVFFlat 인덱스 (대용량 데이터용)
CREATE INDEX IF NOT EXISTS idx_memories_embedding
    ON memories USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100);
```

### 벡터 연산자

| 연산자 | 설명 |
|--------|------|
| `<->` | L2 거리 (유클리드) |
| `<=>` | 코사인 거리 |
| `<#>` | 내적 (음수) |

```sql
-- 코사인 유사도 검색
SELECT *, 1 - (embedding <=> query_vector) as similarity
FROM memories
ORDER BY embedding <=> query_vector
LIMIT 10;

-- L2 거리 검색
SELECT *, embedding <-> query_vector as distance
FROM memories
ORDER BY embedding <-> query_vector
LIMIT 10;
```

## DI 등록

```csharp
// Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddEmbeddingServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var settings = configuration.GetSection("Embedding").Get<EmbeddingSettings>()
        ?? new EmbeddingSettings();

    services.Configure<EmbeddingSettings>(configuration.GetSection("Embedding"));

    return settings.Type.ToLower() switch
    {
        "ollama" => services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>(),
        "openai" => services.AddSingleton<IEmbeddingService, OpenAIEmbeddingService>(),
        "custom" => services.AddSingleton<IEmbeddingService, CustomEmbeddingService>(),
        _ => throw new InvalidOperationException($"Unknown embedding type: {settings.Type}")
    };
}
```

## 폴백 검색 패턴

유사도 검색 결과가 없을 때 임계값을 낮춰서 재시도:

```csharp
public async Task<List<Memory>> SearchWithFallback(string query, double minSimilarity = 0.7)
{
    var results = await SearchSimilar(query, minSimilarity: minSimilarity);

    // 결과 없으면 임계값 10% 낮춰서 재시도
    if (results.Count == 0 && minSimilarity > 0.0)
    {
        var fallbackThreshold = Math.Max(0.0, minSimilarity - 0.1);
        _logger.LogInformation("No results at {Original}, trying {Fallback}",
            minSimilarity, fallbackThreshold);

        results = await SearchSimilar(query, minSimilarity: fallbackThreshold);
    }

    return results;
}
```

## AI 생성 시 메모리 검색 통합 패턴

### 개선된 검색 규칙 (3×3 → LLM 평가 → 최종 3개)

AI 기반 콘텐츠 생성 시 관련 메모리를 더 효과적으로 검색하고 활용하는 패턴:

```
기존: 키워드 3개 × 유사도 1위 1개 = 3개 후보 → 적합성 검토
개선: 키워드 3개 × 유사도 상위 3개 = 9개 후보 → LLM 배치 평가 → 최종 3개 선택
```

### IStorage.Search 활용

```csharp
// 키워드당 상위 3개 검색 (총 최대 9개)
foreach (var keyword in keywords)
{
    var memories = await _storage.Search(
        keyword,
        limit: 3,           // 키워드당 3개
        minSimilarity: 0.3, // 최소 유사도 임계값
        cancellationToken: ct
    );

    foreach (var memory in memories)
    {
        // 중복 제거 (다른 키워드로 이미 검색된 경우)
        if (foundIds.Contains(memory.Id)) continue;
        foundIds.Add(memory.Id);

        // 유사도 변환 (코사인 거리 → 유사도)
        var similarity = memory.Similarity.HasValue ? 1 - memory.Similarity.Value : 0;
        candidates.Add((memory.Id, memory.Title, memory.Text, keyword, similarity));
    }
}
```

### LLM 배치 평가로 최종 선택

```csharp
// 9개 후보를 LLM에게 평가 요청, 최적 3개 선택
var batchPrompt = $@"다음 사용자 요청에 가장 관련성이 높은 참고자료 번호를 최대 3개까지 선택하세요.

## 사용자 요청
{userPrompt}

## 후보 참고자료
{candidateList}

선택 (쉼표 구분):";

var selectionResult = await _llmExService.CompleteAsync(batchPrompt);
var selectedIndices = ParseSelectedIndices(selectionResult);

// 최종 선택된 메모리만 사용
foreach (var idx in selectedIndices.Take(3))
{
    usefulMemories.Add(candidates[idx - 1]);
}
```

### 메모리 검색 옵션 UI

```javascript
// 메모리 검색 여부 체크박스 (기본값: 미사용)
<label>
    <input type="checkbox" id="use-memory-search" />
    메모리 검색 활용
</label>

// 생성 요청 시
const useMemorySearch = document.getElementById('use-memory-search').checked;
fetch('/api/shapeup/generate', {
    method: 'POST',
    body: JSON.stringify({ prompt, useMemorySearch })
});
```

### SSE Phase 이벤트로 진행 상황 표시

```javascript
// 클라이언트에서 Phase 이벤트 처리
eventSource.onmessage = (event) => {
    const data = JSON.parse(event.data);

    // 단계별 진행 상황
    if (data.phase) {
        switch (data.phase) {
            case 'extracting':
                showProgress('키워드 추출 중...');
                break;
            case 'searching':
                showProgress('메모리 검색 중...');
                break;
            case 'evaluating':
                showProgress('적합성 평가 중...');
                break;
            case 'generating':
                showProgress('생성 중...');
                break;
        }
    }

    // 메모리 검색 결과 알림
    if (data.memory_found) {
        showToast('info', `${data.searchedCount}개 검색, ${data.adoptedCount}개 참고`);
    }
};
```

## 주의사항

1. **벡터 차원**: 모델과 DB 스키마의 차원 일치 필수 (기본 384)
2. **비동기 처리**: 대량 임베딩은 액터로 비동기 처리
3. **인덱스 최적화**: 대용량 데이터는 IVFFlat 인덱스 사용
4. **메모리 사용**: 임베딩 배열은 메모리 소비가 큼
5. **타임아웃**: 임베딩 생성은 시간이 걸릴 수 있음
6. **검색 최적화**: 키워드당 3개씩 검색 후 LLM 배치 평가로 최적 선택
