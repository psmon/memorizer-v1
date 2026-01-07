---
name: llm-api
description: Memorizer 프로젝트의 LLM API 활용 스킬. ILlmService 인터페이스를 통한 LLM 통합, 스트리밍 응답 처리, 프롬프트 설계 패턴을 사용할 때 호출. Ollama, OpenAI, Custom LLM 지원.
---

# LLM API 활용 스킬

Memorizer 프로젝트에서 LLM(Large Language Model) API를 활용하는 방법을 안내합니다.

## 프로젝트 구조

```
src/Memorizer/
├── Services/
│   ├── ILlmService.cs           # LLM 서비스 인터페이스
│   ├── OllamaLlmService.cs      # Ollama 구현체
│   ├── OpenAILlmService.cs      # OpenAI 구현체
│   ├── CustomLlmService.cs      # Custom API 구현체
│   ├── ILlmExService.cs         # 확장 LLM 서비스 (고급 분석용)
│   ├── OllamaLlmExService.cs    # Ollama EX 구현체
│   ├── OpenAILlmExService.cs    # OpenAI EX 구현체
│   └── CustomLlmExService.cs    # Custom EX 구현체
├── Settings/
│   ├── LlmSettings.cs           # LLM 설정
│   └── LlmExSettings.cs         # LLM-EX 설정
├── Controllers/
│   └── LLMController.cs         # LLM API 컨트롤러
└── Prompts/
    └── PromptTemplates.cs       # 프롬프트 템플릿
```

## ILlmService 인터페이스

```csharp
public interface ILlmService : IDisposable
{
    // 헬스 체크
    Task<LlmHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    // 제목 생성
    Task<string> GenerateTitle(string content, string contentType,
        string[]? existingTags = null, int maxTitleLength = 80,
        CancellationToken cancellationToken = default);

    // 일반 완료 요청
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);

    // 스트리밍 완료 요청
    IAsyncEnumerable<string> CompleteStreamingAsync(string prompt,
        CancellationToken cancellationToken = default);

    // 키워드 추출
    Task<List<string>> ExtractKeywordsAsync(string content, string contentType,
        int maxKeywords = 10, CancellationToken cancellationToken = default);
}
```

## 설정 방법

### appsettings.json

```json
{
  "LLM": {
    "Type": "Ollama",           // Ollama | OpenAI | Custom
    "ApiUrl": "http://localhost:11434",
    "Model": "llama3.2:latest",
    "Timeout": "00:02:00"
  },
  "LLM-EX": {
    "Type": "Custom",
    "ApiUrl": "http://192.168.0.68:1234",
    "Model": "openai/gpt-oss-120b",
    "Timeout": "00:05:00"
  }
}
```

## 사용 예시

### 1. 기본 완료 요청

```csharp
public class MyService
{
    private readonly ILlmService _llmService;

    public MyService(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public async Task<string> AnalyzeContent(string content)
    {
        var prompt = $@"
다음 내용을 분석하고 요약해주세요:

{content}

분석 결과:";

        return await _llmService.CompleteAsync(prompt);
    }
}
```

### 2. 스트리밍 응답 (SSE)

```csharp
[HttpGet("stream")]
public async Task StreamResponse([FromQuery] string prompt, CancellationToken ct)
{
    Response.ContentType = "text/event-stream";
    Response.Headers["Cache-Control"] = "no-cache";
    Response.Headers["X-Accel-Buffering"] = "no";

    await foreach (var chunk in _llmService.CompleteStreamingAsync(prompt, ct))
    {
        var data = JsonSerializer.Serialize(new { content = chunk });
        await Response.WriteAsync($"data: {data}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }

    await Response.WriteAsync("data: [DONE]\n\n", ct);
}
```

### 3. 제목 생성

```csharp
var title = await _llmService.GenerateTitle(
    content: memoryText,
    contentType: "reference",
    existingTags: new[] { "dotnet", "akka" },
    maxTitleLength: 50
);
```

## 프롬프트 설계 패턴

### 구조화된 프롬프트

```csharp
private const string AnalysisPrompt = @"
당신은 {ROLE} 전문가입니다.

## 배경
{CONTEXT}

## 작업
{TASK}

## 입력
{INPUT}

## 출력 형식
{OUTPUT_FORMAT}

## 제약사항
- 한국어로 응답할 것
- {CONSTRAINT}
";
```

### 응답 포맷 지정

```csharp
var prompt = @"
다음 텍스트에서 키워드를 추출하세요.

텍스트: {content}

응답 형식 (JSON):
{
  ""keywords"": [""keyword1"", ""keyword2""]
}
";
```

## LLM-EX 서비스 (고급 분석용)

복잡한 분석이 필요한 경우 LLM-EX를 사용합니다.

```csharp
public class PrdMakerController
{
    private readonly ILlmExService _llmExService;

    public async IAsyncEnumerable<string> GenerateEventStorming(string prd)
    {
        var prompt = CreateEventStormingPrompt(prd);

        await foreach (var chunk in _llmExService.CompleteStreamingAsync(prompt))
        {
            yield return chunk;
        }
    }
}
```

## DI 등록

```csharp
// Program.cs 또는 ServiceCollectionExtensions.cs
services.AddLlmServices(configuration);
services.AddLlmExServices(configuration);
```

## 주의사항

1. **토큰 제한**: 긴 컨텐츠는 청크로 분할하여 처리
2. **타임아웃**: 복잡한 요청은 타임아웃을 충분히 설정
3. **에러 핸들링**: LLM 응답 실패 시 폴백 로직 구현
4. **스트리밍**: SSE 사용 시 `X-Accel-Buffering: no` 헤더 필수
