# Multi-LLM Service Architecture with Interface-based Selection Pattern

## 개요 (Overview)
이 아키텍처 패턴은 통합된 인터페이스 설계를 통해 다양한 LLM 제공자(OpenAI, Ollama, Custom 내부 API) 간의 원활한 전환을 가능하게 합니다. 구현은 의존성 주입과 구성 기반 서비스 선택을 사용합니다.

## 핵심 아키텍처 구성요소

### 1. 인터페이스 정의 (Interface Definitions)

#### ILlmService - 핵심 LLM 서비스 인터페이스
```csharp
public interface ILlmService : IDisposable
{
    // 건강 상태 체크
    Task<LlmHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    
    // 제목 생성
    Task<string> GenerateTitle(
        string content, 
        string contentType, 
        string[]? existingTags = null, 
        int maxTitleLength = 80, 
        CancellationToken cancellationToken = default);
    
    // 일반 완성 요청
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
    
    // 키워드 추출
    Task<List<string>> ExtractKeywordsAsync(
        string content, 
        string contentType, 
        int maxKeywords = 10, 
        CancellationToken cancellationToken = default);
}
```

#### IEmbeddingService - 임베딩 서비스 인터페이스
```csharp
public interface IEmbeddingService
{
    Task<float[]> Generate(string text, CancellationToken cancellationToken = default);
    Task<float[]> Generate(JsonDocument document, CancellationToken cancellationToken = default);
}
```

### 2. 구성 모델 (Configuration Model)

#### LLM 설정
```csharp
public class LlmSettings
{
    public string Type { get; set; } = "Ollama"; // "openai", "ollama", "custom"
    public Uri ApiUrl { get; set; }
    public string Model { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
    public string ApiKey { get; set; } = string.Empty;
}
```

#### 임베딩 설정
```csharp
public class EmbeddingSettings
{
    public string Type { get; init; } = "Ollama";
    public required Uri ApiUrl { get; init; }
    public required string Model { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
    public string ApiKey { get; init; } = string.Empty;
}
```

### 3. 서비스 등록 패턴 (Service Registration Pattern)

```csharp
public static IServiceCollection AddLlmServices(this IServiceCollection services)
{
    services.AddSingleton<ILlmService>(sp =>
    {
        var settings = sp.GetRequiredService<LlmSettings>();
        var logger = sp.GetRequiredService<ILoggerFactory>();
        
        string apiType = settings.Type.ToLower();
        
        // 구성에 따른 서비스 선택
        if (apiType.Equals("openai"))
        {
            return new OpenAILlmService(settings, logger.CreateLogger<OpenAILlmService>());
        }
        else if (apiType.Equals("custom"))
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = settings.ApiUrl;
            httpClient.Timeout = settings.Timeout;
            return new CustomLlmService(httpClient, settings, logger.CreateLogger<CustomLlmService>());
        }
        else
        {
            // 기본값: Ollama
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = settings.ApiUrl;
            httpClient.Timeout = settings.Timeout;
            return new OllamaLlmService(httpClient, settings, logger.CreateLogger<OllamaLlmService>());
        }
    });
}
```

## 구현 패턴 (Implementation Patterns)

### 1. 중앙화된 프롬프트 관리 (Centralized Prompt Management)

`PromptTemplates.cs`를 생성하여 모든 프롬프트 템플릿을 중앙화:
- 프롬프트의 단일 진실 소스 (Single source of truth)
- 서비스 간 일관된 형식
- 쉬운 유지보수 및 업데이트
- 다양한 LLM 제공자 간 재사용 가능

```csharp
public static class PromptTemplates
{
    public const string TitleGenerationSystemMessage = 
        "You are an expert at creating concise, descriptive titles for various types of content.";
    
    public static string CreateTitleGenerationPrompt(
        string content, 
        string contentType, 
        string[]? existingTags, 
        int maxTitleLength)
    {
        // 프롬프트 생성 로직
    }
}
```

### 2. 서비스 구현 구조 (Service Implementation Structure)

각 LLM 서비스는 다음 패턴을 따릅니다:

```csharp
public sealed class CustomLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<CustomLlmService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // 의존성 주입을 통한 생성자
    public CustomLlmService(
        HttpClient httpClient,
        LlmSettings settings,
        ILogger<CustomLlmService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        // 초기화 로직
    }
    
    // 인터페이스 메서드 구현
    public async Task<string> GenerateTitle(...) { }
    public async Task<string> CompleteAsync(...) { }
    public async Task<List<string>> ExtractKeywordsAsync(...) { }
    public async Task<LlmHealthResult> CheckHealthAsync(...) { }
    
    // 서비스별 헬퍼 메서드
    private async Task<string> SendChatRequest(...) { }
    private static string ParseTitleResponse(...) { }
}
```

### 3. 오류 처리 전략 (Error Handling Strategy)

- 실패한 작업에 대한 우아한 폴백
- 각 수준에서의 상세 로깅
- 모니터링을 위한 건강 체크 엔드포인트
- 서비스별 타임아웃 구성

## 구성 예제 (Configuration Examples)

### OpenAI 구성
```json
{
  "LLM": {
    "Type": "openai",
    "ApiUrl": "https://api.openai.com/v1",
    "Model": "gpt-4o-mini",
    "ApiKey": "sk-...",
    "Timeout": "00:02:00"
  },
  "Embeddings": {
    "Type": "openai",
    "ApiUrl": "https://api.openai.com/v1",
    "Model": "text-embedding-3-small",
    "ApiKey": "sk-...",
    "Timeout": "00:00:30"
  }
}
```

### Custom 내부 API 구성
```json
{
  "LLM": {
    "Type": "custom",
    "ApiUrl": "http://192.168.0.50:1234",
    "Model": "openai/gpt-oss-20b",
    "Timeout": "00:02:00"
  },
  "Embeddings": {
    "Type": "custom",
    "ApiUrl": "http://192.168.0.50:1234",
    "Model": "text-embedding-nomic-embed-text-v1.5",
    "Timeout": "00:00:30"
  }
}
```

### Ollama 구성
```json
{
  "LLM": {
    "Type": "ollama",
    "ApiUrl": "http://localhost:11434",
    "Model": "llama3",
    "Timeout": "00:02:00"
  },
  "Embeddings": {
    "Type": "ollama",
    "ApiUrl": "http://localhost:11434",
    "Model": "all-minilm:33m-l12-v2-fp16",
    "Timeout": "00:00:30"
  }
}
```

## 주요 이점 (Key Benefits)

1. **제공자 독립적 (Provider Agnostic)**: 코드 변경 없이 제공자 전환
2. **구성 주도 (Configuration-Driven)**: 구성을 통한 제공자 변경
3. **일관된 인터페이스 (Consistent Interface)**: 제공자와 관계없이 동일한 API
4. **확장 가능 (Extensible)**: 새로운 제공자 추가 용이
5. **테스트 가능 (Testable)**: 인터페이스 기반 설계로 모킹 가능
6. **유지보수 가능 (Maintainable)**: 중앙화된 프롬프트 관리
7. **프로덕션 준비 (Production Ready)**: 건강 체크, 로깅, 오류 처리

## 확장 포인트 (Extension Points)

### 새로운 제공자 추가하기
1. `ILlmService`를 구현하는 새 서비스 클래스 생성
2. ServiceCollectionExtensions에 새 케이스 추가
3. 구성 예제 생성
4. 소비 코드에서는 변경 불필요

### 커스텀 프롬프트 템플릿
모든 프롬프트가 `PromptTemplates.cs`에 중앙화:
- 제목 생성 (Title generation)
- 키워드 추출 (Keyword extraction)
- 그래프 쿼리 생성 (Graph query generation)
- 관계 제안 (Relationship suggestions)

## Docker 배포 (Docker Deployment)

환경 변수 구성 지원:
```yaml
environment:
  - LLM__Type=custom
  - LLM__ApiUrl=http://192.168.0.50:1234
  - LLM__Model=openai/gpt-oss-20b
  - Embeddings__Type=custom
  - Embeddings__ApiUrl=http://192.168.0.50:1234
  - Embeddings__Model=text-embedding-nomic-embed-text-v1.5
```

## 테스팅 전략 (Testing Strategy)

- 모킹된 인터페이스를 사용한 단위 테스트
- 제공자별 통합 테스트
- 구성 유효성 검사
- 건강 체크 엔드포인트

## 아키텍처 다이어그램 (Architecture Diagram)

```
┌─────────────────────────────────────────────────────────────┐
│                        Application Layer                      │
│                    (Controllers, Services, Actors)            │
└────────────────────────────┬───────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                      Interface Layer                          │
│         ┌──────────────┐        ┌─────────────────┐         │
│         │ ILlmService  │        │IEmbeddingService│         │
│         └──────────────┘        └─────────────────┘         │
└────────────────────────────┬───────────────────────────────┘
                             │
                ┌────────────┼────────────┐
                ▼            ▼            ▼
┌──────────────────┐ ┌──────────────┐ ┌──────────────┐
│ OpenAILlmService │ │OllamaService │ │CustomService │
│                  │ │              │ │              │
│ - OpenAI API     │ │ - Ollama API │ │ - Internal   │
│ - GPT Models     │ │ - Local LLM  │ │   API        │
└──────────────────┘ └──────────────┘ └──────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                    Configuration Layer                        │
│                  (appsettings.json, ENV vars)                │
└─────────────────────────────────────────────────────────────┘
```

## 실제 사용 예제 (Practical Usage Example)

```csharp
// Startup.cs 또는 Program.cs에서
builder.Services.AddMemorizer();

// Controller 또는 Service에서
public class MemoryController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly IEmbeddingService _embeddingService;
    
    public MemoryController(
        ILlmService llmService,
        IEmbeddingService embeddingService)
    {
        _llmService = llmService;
        _embeddingService = embeddingService;
    }
    
    // 제공자와 관계없이 동일한 코드
    public async Task<IActionResult> GenerateTitle([FromBody] ContentRequest request)
    {
        var title = await _llmService.GenerateTitle(
            request.Content,
            request.Type,
            request.Tags
        );
        
        return Ok(new { title });
    }
}
```

## 결론 (Conclusion)

이 아키텍처는 SOLID 원칙을 유지하면서 최대한의 유연성을 제공합니다. 인터페이스 기반 설계를 통해 다양한 LLM 제공자를 쉽게 전환할 수 있으며, 중앙화된 구성 관리와 프롬프트 템플릿을 통해 유지보수성을 높였습니다. 

이는 프로덕션 환경에서 비용, 성능, 가용성을 고려하여 최적의 LLM 제공자를 선택할 수 있는 유연한 솔루션을 제공합니다.