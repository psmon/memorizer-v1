# 개선 제안 및 로드맵

## 개요

Memorizer-v1의 현재 구현을 분석하고, 향후 개선 방향과 확장 가능성을 제시합니다. 실무에서 활용 가능한 구체적인 개선안과 우선순위를 포함합니다.

## 현재 시스템 강점

### 1. 견고한 아키텍처

✅ **액터 모델 기반**
- 확장 가능한 분산 시스템 구조
- 격리된 상태 관리
- 비동기 메시지 처리

✅ **이중 저장소 전략**
- PostgreSQL (벡터 검색) + Neo4j (그래프 관계)
- 각 DB의 강점을 최대한 활용
- 동기화 메커니즘 구현

✅ **SSE 기반 실시간 스트리밍**
- 사용자 경험 향상
- 추론 과정의 투명성
- 간단한 구현 (WebSocket 대비)

### 2. MCP 통합

✅ **표준 프로토콜 준수**
- Claude Desktop 등 외부 도구와 통합
- 확장 가능한 도구 인터페이스
- 텔레메트리 및 로깅 지원

## 우선순위별 개선 제안

### 🚀 높은 우선순위 (단기: 1-2개월)

#### 1. 검색 성능 최적화

**현재 문제점**:
- 벡터 검색 실패 시 키워드 검색을 순차적으로 재시도 (최대 3회)
- 각 재시도마다 별도의 DB 쿼리 발생
- 검색 시간이 길어질 수 있음

**개선안**:

```csharp
// 현재 구현 (순차적)
foreach (var keyword in extractedKeywords.Take(3))
{
    if (memories.Count > 0) break;
    memories = await _storage.Search(keyword, ...);
}

// 개선안 (병렬 처리)
public async Task<List<Memory>> ParallelKeywordSearch(
    List<string> keywords,
    int maxResults,
    double minSimilarity)
{
    // 모든 키워드로 병렬 검색
    var searchTasks = keywords
        .Take(3)
        .Select(keyword => _storage.Search(keyword, maxResults, minSimilarity, null));

    var results = await Task.WhenAll(searchTasks);

    // 결과 병합 및 중복 제거
    return results
        .SelectMany(r => r)
        .GroupBy(m => m.Id)
        .Select(g => g.First())
        .OrderByDescending(m => m.Similarity)
        .Take(maxResults)
        .ToList();
}
```

**예상 효과**:
- 검색 시간 60% 단축
- 더 다양한 결과 획득

#### 2. 캐싱 레이어 추가

**현재 문제점**:
- 동일한 쿼리에 대해 매번 임베딩 생성 및 DB 검색
- 자주 사용되는 메모리도 매번 조회

**개선안**:

```csharp
public class MemoryCache : IMemoryCache
{
    private readonly IDistributedCache _cache;  // Redis
    private readonly IStorage _storage;

    public async Task<List<Memory>> GetOrSearchAsync(
        string query,
        int limit,
        double minSimilarity)
    {
        // 1. 쿼리 해시 생성
        var cacheKey = $"search:{ComputeHash(query)}:{limit}:{minSimilarity}";

        // 2. 캐시 확인
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<List<Memory>>(cached);
        }

        // 3. DB 검색
        var results = await _storage.Search(query, limit, minSimilarity, null);

        // 4. 캐시 저장 (5분)
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(results),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            }
        );

        return results;
    }

    // 임베딩 캐시
    public async Task<float[]> GetOrCreateEmbeddingAsync(string text)
    {
        var cacheKey = $"embedding:{ComputeHash(text)}";

        var cached = await _cache.GetAsync(cacheKey);
        if (cached != null)
        {
            return DeserializeEmbedding(cached);
        }

        var embedding = await _llmService.GetEmbeddingAsync(text);

        await _cache.SetAsync(
            cacheKey,
            SerializeEmbedding(embedding),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            }
        );

        return embedding;
    }
}
```

**기술 스택**:
- Redis (분산 캐시)
- IDistributedCache (ASP.NET Core)

**예상 효과**:
- 반복 검색 시 응답 시간 90% 단축
- OpenAI API 호출 비용 절감

#### 3. 컨텍스트 윈도우 관리 개선

**현재 문제점**:
- 단기 메모리 최대 500자로 제한
- 긴 대화에서 중요한 정보 손실 가능

**개선안**:

```csharp
public class ConversationContextManager
{
    private readonly ILlmService _llmService;
    private readonly int _maxTokens = 2000;  // GPT-4 기준

    public async Task<string> OptimizeContextAsync(
        List<ConversationEntry> history,
        string currentQuery)
    {
        // 1. 현재 쿼리와 관련된 대화만 선택
        var relevantEntries = await SelectRelevantEntriesAsync(history, currentQuery);

        // 2. 토큰 수 계산
        var totalTokens = EstimateTokens(relevantEntries);

        // 3. 제한 초과 시 요약
        if (totalTokens > _maxTokens)
        {
            return await SummarizeEntriesAsync(relevantEntries, _maxTokens);
        }

        return FormatEntries(relevantEntries);
    }

    private async Task<List<ConversationEntry>> SelectRelevantEntriesAsync(
        List<ConversationEntry> history,
        string currentQuery)
    {
        // 최근 3개는 무조건 포함
        var recent = history.TakeLast(3).ToList();

        // 나머지는 관련성 기준으로 선택
        var older = history.SkipLast(3);
        var relevant = new List<ConversationEntry>(recent);

        foreach (var entry in older.Reverse())
        {
            // 임베딩 유사도로 관련성 판단
            var similarity = await CalculateSimilarityAsync(
                entry.UserMessage + " " + entry.BotResponse,
                currentQuery
            );

            if (similarity > 0.7)
            {
                relevant.Insert(0, entry);
            }

            if (relevant.Count >= 10) break;  // 최대 10개
        }

        return relevant;
    }

    private async Task<string> SummarizeEntriesAsync(
        List<ConversationEntry> entries,
        int maxTokens)
    {
        var prompt = $@"
Summarize the following conversation history concisely while preserving key information.
Maximum length: {maxTokens / 2} tokens.

Conversation:
{FormatEntries(entries)}

Summary:";

        return await _llmService.CompleteAsync(prompt);
    }
}
```

**예상 효과**:
- 긴 대화에서도 컨텍스트 유지
- 관련성 높은 정보 우선 보존

### ⚡ 중간 우선순위 (중기: 3-6개월)

#### 4. 멀티모달 지원

**확장 영역**:
- 이미지 메모리 (스크린샷, 다이어그램)
- 코드 스니펫 (구문 강조, 실행 가능)
- PDF 문서

**개선안**:

```csharp
public class MultiModalMemory : Memory
{
    public MemoryContentType ContentType { get; set; }
    public string? ImageUrl { get; set; }
    public string? CodeLanguage { get; set; }
    public byte[]? BinaryData { get; set; }
}

public enum MemoryContentType
{
    Text,
    Image,
    Code,
    PDF,
    Mixed
}

// 이미지 메모리 저장
public async Task<Memory> StoreImageMemoryAsync(
    byte[] imageData,
    string description,
    string[] tags)
{
    // 1. 이미지를 Azure Blob Storage에 저장
    var imageUrl = await _blobStorage.UploadAsync(imageData);

    // 2. GPT-4 Vision으로 설명 생성
    var visionDescription = await _llmService.AnalyzeImageAsync(imageData);

    // 3. 설명 + 사용자 입력으로 임베딩 생성
    var combinedText = $"{description}\n\nImage Analysis: {visionDescription}";
    var embedding = await _llmService.GetEmbeddingAsync(combinedText);

    // 4. 메모리 저장
    return await _storage.StoreMemory(
        "image",
        combinedText,
        "user",
        tags,
        0.9,
        title: description,
        imageUrl: imageUrl
    );
}
```

#### 5. 지식 그래프 자동 구축

**현재 한계**:
- 관계 생성이 주로 수동
- 자동 제안은 있지만 적극적이지 않음

**개선안**:

```csharp
public class AutoGraphBuilder : ReceiveActor
{
    private readonly IActorRef _graphRelationshipActor;
    private readonly IGraphSearchService _graphSearch;

    public AutoGraphBuilder(...)
    {
        // 새 메모리 저장 이벤트 구독
        Receive<MemoryStoredEvent>(HandleMemoryStored);

        // 배치 처리 스케줄 (매일 자정)
        Context.System.Scheduler.ScheduleTellRepeatedly(
            TimeSpan.Zero,
            TimeSpan.FromDays(1),
            Self,
            new BuildGraphBatch(),
            Self
        );
    }

    private async Task HandleMemoryStored(MemoryStoredEvent evt)
    {
        // 1. 유사한 메모리 찾기
        var similarMemories = await _storage.Search(
            evt.Memory.Text,
            10,
            0.7,
            evt.Memory.Tags
        );

        // 2. LLM으로 관계 타입 판단
        var relationships = await AnalyzeRelationshipsAsync(
            evt.Memory,
            similarMemories
        );

        // 3. 신뢰도 높은 관계만 자동 생성 (0.8 이상)
        var highConfidence = relationships.Where(r => r.Confidence >= 0.8);

        foreach (var rel in highConfidence)
        {
            _graphRelationshipActor.Tell(new CreateRelationshipCommand
            {
                FromId = evt.Memory.Id,
                ToId = rel.TargetId,
                Type = rel.Type
            });
        }

        // 4. 중간 신뢰도는 제안으로 저장 (0.6~0.8)
        var mediumConfidence = relationships.Where(r =>
            r.Confidence >= 0.6 && r.Confidence < 0.8);

        await StoreSuggestionsAsync(mediumConfidence);
    }

    private async Task<List<RelationshipSuggestion>> AnalyzeRelationshipsAsync(
        Memory source,
        List<Memory> candidates)
    {
        var prompt = $$"""
        Analyze relationships between the source memory and candidates.

        Source: {{source.Title}}
        Type: {{source.Type}}

        Candidates:
        {{FormatCandidates(candidates)}}

        For each candidate, suggest:
        1. Relationship type (extends, supports, implements, etc.)
        2. Confidence (0.0-1.0)
        3. Reasoning

        Return as JSON array.
        """;

        var response = await _llmService.CompleteAsync(prompt);
        return ParseRelationshipSuggestions(response);
    }
}
```

#### 6. 대화 품질 모니터링

**목적**:
- 사용자 만족도 추적
- 검색 성능 개선
- 프롬프트 최적화

**개선안**:

```csharp
public class ConversationAnalytics
{
    private readonly ILogger<ConversationAnalytics> _logger;
    private readonly ITelemetryClient _telemetry;

    public async Task TrackConversationAsync(
        string sessionId,
        UserChatRequest request,
        ChatBotResponse response,
        SearchMemoryResponse? searchResponse)
    {
        // 1. 메트릭 수집
        var metrics = new ConversationMetrics
        {
            SessionId = sessionId,
            QueryLength = request.Message.Length,
            ResponseType = response.Type,
            ResponseTime = (response.Timestamp - DateTime.UtcNow).TotalMilliseconds,
            MemoriesFound = searchResponse?.Memories.Count ?? 0,
            RelevantMemoriesCount = response.ReferencedMemoryIds?.Count ?? 0,
            SearchRetries = searchResponse?.RetryAttempts ?? 0,
            Timestamp = DateTime.UtcNow
        };

        // 2. Application Insights에 전송
        _telemetry.TrackEvent("ConversationCompleted", new Dictionary<string, string>
        {
            ["SessionId"] = sessionId,
            ["ResponseType"] = response.Type.ToString(),
            ["MemoriesUsed"] = metrics.RelevantMemoriesCount.ToString()
        });

        _telemetry.TrackMetric("ResponseTime", metrics.ResponseTime);
        _telemetry.TrackMetric("SearchRetries", metrics.SearchRetries);

        // 3. 품질 점수 계산
        var qualityScore = CalculateQualityScore(metrics);
        _telemetry.TrackMetric("ConversationQuality", qualityScore);

        // 4. 낮은 품질 대화 로깅 (개선 대상)
        if (qualityScore < 0.5)
        {
            _logger.LogWarning(
                "Low quality conversation detected. SessionId: {SessionId}, Score: {Score}",
                sessionId, qualityScore
            );

            await StoreLowQualityConversationAsync(request, response, metrics);
        }
    }

    private double CalculateQualityScore(ConversationMetrics metrics)
    {
        var score = 1.0;

        // 메모리 기반 응답이 더 높은 점수
        if (metrics.ResponseType == ResponseType.MemoryBased)
            score *= 1.2;

        // 응답 시간이 짧을수록 높은 점수
        if (metrics.ResponseTime < 2000)
            score *= 1.1;
        else if (metrics.ResponseTime > 10000)
            score *= 0.7;

        // 재시도가 적을수록 높은 점수
        if (metrics.SearchRetries == 0)
            score *= 1.1;
        else if (metrics.SearchRetries > 2)
            score *= 0.8;

        // 관련 메모리를 찾았는지
        if (metrics.RelevantMemoriesCount > 0)
            score *= 1.2;

        return Math.Min(score, 1.0);
    }
}
```

### 🔮 낮은 우선순위 (장기: 6개월 이상)

#### 7. 분산 액터 시스템 (Akka.Cluster)

**확장 시나리오**:
- 다중 서버 배포
- 세션 샤딩
- 장애 복구

**개선안**:

```csharp
// Cluster 설정
var config = ConfigurationFactory.ParseString(@"
    akka {
        actor {
            provider = cluster
        }
        remote {
            dot-netty.tcp {
                hostname = ""0.0.0.0""
                port = 8081
            }
        }
        cluster {
            seed-nodes = [""akka.tcp://memorizer@node1:8081""]
            roles = [chatbot]
        }
    }
");

// Cluster Sharding으로 세션 분산
var shardRegion = await ClusterSharding.Get(_actorSystem).StartAsync(
    typeName: "ChatBotActor",
    entityPropsFactory: s => ChatBotActor.Props(s, ...),
    settings: ClusterShardingSettings.Create(_actorSystem),
    messageExtractor: new SessionMessageExtractor()
);

// 세션 ID 기반 샤딩
public class SessionMessageExtractor : HashCodeMessageExtractor
{
    public SessionMessageExtractor() : base(maxNumberOfShards: 100) { }

    public override string EntityId(object message)
    {
        return message switch
        {
            UserChatRequest req => req.SessionId,
            _ => null
        };
    }
}
```

#### 8. RAG 파이프라인 고도화

**추가 기능**:
- Query rewriting (다중 변형)
- Re-ranking (재순위화)
- Hypothetical Document Embeddings (HyDE)

**개선안**:

```csharp
public class AdvancedRAGPipeline
{
    // 1. Query Rewriting
    private async Task<List<string>> GenerateQueryVariationsAsync(string originalQuery)
    {
        var prompt = $@"
Generate 3 different variations of the following query,
each focusing on different aspects:

Original: {originalQuery}

Variations (one per line):";

        var response = await _llmService.CompleteAsync(prompt);
        return response.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    // 2. Multi-Query Search
    private async Task<List<Memory>> MultiQuerySearchAsync(string originalQuery)
    {
        var queries = await GenerateQueryVariationsAsync(originalQuery);
        queries.Insert(0, originalQuery);  // 원본도 포함

        var searchTasks = queries.Select(q => _storage.Search(q, 5, 0.6, null));
        var results = await Task.WhenAll(searchTasks);

        return results
            .SelectMany(r => r)
            .GroupBy(m => m.Id)
            .Select(g => g.OrderByDescending(m => m.Similarity).First())
            .OrderByDescending(m => m.Similarity)
            .Take(10)
            .ToList();
    }

    // 3. Re-ranking with Cross-Encoder
    private async Task<List<Memory>> ReRankResultsAsync(
        string query,
        List<Memory> candidates)
    {
        var scores = new List<(Memory memory, double score)>();

        foreach (var memory in candidates)
        {
            var score = await CalculateRelevanceScoreAsync(query, memory.Text);
            scores.Add((memory, score));
        }

        return scores
            .OrderByDescending(s => s.score)
            .Select(s => s.memory)
            .ToList();
    }

    // 4. HyDE (Hypothetical Document Embeddings)
    private async Task<List<Memory>> HyDESearchAsync(string query)
    {
        // 가상의 이상적인 답변 생성
        var hydePrompt = $@"
Write a detailed, factual answer to the following question
as if you were writing reference documentation:

Question: {query}

Answer:";

        var hypotheticalDoc = await _llmService.CompleteAsync(hydePrompt);

        // 가상 문서의 임베딩으로 검색
        return await _storage.Search(hypotheticalDoc, 10, 0.7, null);
    }
}
```

#### 9. 사용자 피드백 루프

**목적**:
- 검색 결과 개선
- 프롬프트 최적화
- 메모리 품질 향상

**개선안**:

```csharp
// UI에 피드백 버튼 추가
public class FeedbackRequest
{
    public string SessionId { get; set; }
    public Guid? MemoryId { get; set; }
    public FeedbackType Type { get; set; }  // Helpful, NotHelpful, Incorrect
    public string? Comment { get; set; }
}

public enum FeedbackType
{
    Helpful,
    NotHelpful,
    Incorrect,
    Outdated
}

// 피드백 처리 액터
public class FeedbackActor : ReceiveActor
{
    public FeedbackActor()
    {
        ReceiveAsync<FeedbackRequest>(HandleFeedback);
    }

    private async Task HandleFeedback(FeedbackRequest feedback)
    {
        // 1. 피드백 저장
        await _feedbackRepo.SaveAsync(feedback);

        // 2. 부정적 피드백 처리
        if (feedback.Type == FeedbackType.Incorrect ||
            feedback.Type == FeedbackType.Outdated)
        {
            // 메모리에 플래그 추가
            await _storage.UpdateMemoryConfidenceAsync(
                feedback.MemoryId.Value,
                confidence => confidence * 0.8
            );

            // 관리자에게 알림
            await NotifyAdminAsync(feedback);
        }

        // 3. 긍정적 피드백 처리
        if (feedback.Type == FeedbackType.Helpful)
        {
            // 신뢰도 향상
            await _storage.UpdateMemoryConfidenceAsync(
                feedback.MemoryId.Value,
                confidence => Math.Min(confidence * 1.1, 1.0)
            );
        }
    }
}
```

## 기술 부채 해결

### 1. 테스트 커버리지 확대

**현재 상태**: 단위 테스트 부족

**목표**:
- Actor 단위 테스트 (Akka.TestKit 사용)
- 통합 테스트 (TestContainers로 DB 테스트)
- E2E 테스트 (Playwright)

```csharp
// Actor 테스트 예시
[Fact]
public async Task ChatBotActor_Should_SearchMemory_When_TechnicalQuery()
{
    // Arrange
    var probe = CreateTestProbe();
    var searchActorMock = CreateTestProbe();

    var chatBot = Sys.ActorOf(ChatBotActor.Props(
        "test-session",
        searchActorMock,
        _decisionActorMock,
        _llmServiceMock
    ));

    // Act
    chatBot.Tell(new UserChatRequest
    {
        SessionId = "test-session",
        Message = "Tell me about Akka.NET",
        UserId = "test-user"
    }, probe);

    // Assert
    searchActorMock.ExpectMsg<SearchMemoryRequest>(msg =>
    {
        msg.Query.Should().Be("Tell me about Akka.NET");
        msg.MaxResults.Should().Be(5);
    });
}
```

### 2. 에러 처리 강화

**개선 영역**:
- 모든 액터에 Supervisor 전략 추가
- 재시도 로직 with exponential backoff
- Circuit breaker for external services

```csharp
// Circuit Breaker 예시
public class ResilientLlmService : ILlmService
{
    private readonly ILlmService _inner;
    private readonly ICircuitBreakerPolicy _circuitBreaker;

    public ResilientLlmService(ILlmService inner)
    {
        _inner = inner;

        _circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, duration) =>
                {
                    _logger.LogError("Circuit breaker opened for {Duration}s", duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset");
                }
            );
    }

    public async Task<string> CompleteAsync(string prompt)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            return await _inner.CompleteAsync(prompt);
        });
    }
}
```

### 3. 문서화 및 API 명세

**추가 문서**:
- OpenAPI (Swagger) 명세
- Actor 메시지 다이어그램
- 배포 가이드
- 트러블슈팅 가이드

## 로드맵 요약

### Phase 1: 성능 최적화 (1-2개월)
- [ ] 병렬 검색 구현
- [ ] Redis 캐싱 레이어 추가
- [ ] 컨텍스트 윈도우 관리 개선
- [ ] 테스트 커버리지 50% 이상

### Phase 2: 기능 확장 (3-6개월)
- [ ] 멀티모달 지원 (이미지, 코드, PDF)
- [ ] 지식 그래프 자동 구축
- [ ] 대화 품질 모니터링
- [ ] 사용자 피드백 시스템

### Phase 3: 엔터프라이즈 준비 (6개월 이상)
- [ ] Akka.Cluster로 분산 시스템 구축
- [ ] 고도화된 RAG 파이프라인
- [ ] 완전한 에러 처리 및 복구
- [ ] 프로덕션 배포 및 모니터링

## 성능 목표

| 메트릭 | 현재 | 목표 (Phase 1) | 목표 (Phase 3) |
|-------|------|---------------|---------------|
| 평균 응답 시간 | 3-5초 | 1-2초 | < 1초 |
| 검색 정확도 | 70% | 85% | 90%+ |
| 동시 세션 지원 | 100 | 500 | 5000+ |
| 캐시 히트율 | 0% | 60% | 80%+ |
| 테스트 커버리지 | 10% | 50% | 80%+ |

## 참고 자료

### 기술 블로그 및 논문
- [RAG 개선 기법](https://arxiv.org/abs/2312.10997)
- [Akka.NET Best Practices](https://getakka.net/articles/intro/what-is-akka.html)
- [Vector Search Optimization](https://www.pinecone.io/learn/vector-search/)

### 관련 오픈소스 프로젝트
- [LangChain](https://github.com/langchain-ai/langchain)
- [Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- [Milvus](https://github.com/milvus-io/milvus)

### 현재 프로젝트 파일
- [ChatBotActor.cs](https://github.com/psmon/memorizer-v1/blob/dev/src/Memorizer/Actors/ChatBotActor.cs)
- [SearchMemoryActor.cs](https://github.com/psmon/memorizer-v1/blob/dev/src/Memorizer/Actors/SearchMemoryActor.cs)
- [MemoryTools.cs](https://github.com/psmon/memorizer-v1/blob/dev/src/Memorizer/Tools/MemoryTools.cs)
