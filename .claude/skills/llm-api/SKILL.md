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

## 메모리 검색 + LLM 배치 평가 패턴

AI 기반 콘텐츠 생성 시 관련 메모리를 검색하고 LLM으로 적합성을 평가하는 패턴:

### 개선된 검색 규칙
- **기존**: 키워드당 1개씩 (총 3개) → 적합성 검토
- **개선**: 키워드당 3개씩 (최대 9개) → LLM 배치 평가 → 최종 3개 선택

### 구현 예시

```csharp
/// <summary>
/// 메모리 검색 + LLM 배치 평가로 최적 메모리 선택
/// Phase: 키워드 추출 → 메모리 검색 (3×3) → LLM 배치 평가 → 최종 선택
/// </summary>
private async Task<List<(string Title, string Content)>> SearchWithBatchEvaluation(
    string userPrompt)
{
    var usefulMemories = new List<(string Title, string Content, string Keyword, double Similarity)>();

    try
    {
        // Phase 1: 키워드 추출 (3개)
        await WriteSSEEvent("phase", new { phase = "extracting", message = "키워드 추출 중..." });

        var keywordPrompt = GetSearchKeywordsExtractionPrompt(userPrompt);
        var keywordsResult = await _llmExService.CompleteAsync(keywordPrompt);

        var keywords = keywordsResult?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim().Replace("\"", "").Replace("'", ""))
            .Where(k => !string.IsNullOrWhiteSpace(k) && k.Length <= 30)
            .Take(3)
            .ToList() ?? new List<string>();

        if (keywords.Count == 0)
        {
            await WriteSSEEvent("memory_found", new { searchedCount = 0, adoptedCount = 0 });
            return new();
        }

        // Phase 2: 키워드별 3개씩 검색 (총 최대 9개)
        await WriteSSEEvent("phase", new { phase = "searching", message = $"메모리 검색 중... ({string.Join(", ", keywords)})" });

        var foundIds = new HashSet<Guid>();
        var candidates = new List<(Guid Id, string Title, string Content, string Keyword, double Similarity)>();

        foreach (var keyword in keywords)
        {
            var memories = await _storage.Search(
                keyword,
                limit: 3,           // 키워드당 3개
                minSimilarity: 0.3,
                cancellationToken: HttpContext.RequestAborted
            );

            foreach (var memory in memories)
            {
                if (foundIds.Contains(memory.Id)) continue;
                foundIds.Add(memory.Id);

                var similarity = memory.Similarity.HasValue ? 1 - memory.Similarity.Value : 0;
                candidates.Add((memory.Id, memory.Title ?? "제목 없음", memory.Text ?? "", keyword, similarity));
            }
        }

        if (candidates.Count == 0)
        {
            await WriteSSEEvent("memory_found", new { searchedCount = 0, adoptedCount = 0 });
            return new();
        }

        // Phase 3: LLM 배치 평가 - 최적 3개 선택
        await WriteSSEEvent("phase", new { phase = "evaluating", message = $"{candidates.Count}개 후보 중 최적 3개 선택 중..." });

        var candidateList = candidates.Select((m, idx) => (
            Title: m.Title,
            Summary: m.Content.Length > 200 ? m.Content.Substring(0, 200) + "..." : m.Content,
            Index: idx + 1
        )).ToList();

        var batchPrompt = GetBatchMemoryRelevancePrompt(userPrompt, candidateList);
        var selectionResult = await _llmExService.CompleteAsync(batchPrompt);

        // 선택된 인덱스 파싱
        var selectedIndices = ParseSelectedIndices(selectionResult, candidates.Count);

        // 최종 선택된 메모리 추가
        foreach (var idx in selectedIndices.Take(3))
        {
            var m = candidates[idx - 1];
            usefulMemories.Add((m.Title, m.Content, m.Keyword, m.Similarity));
        }

        await WriteSSEEvent("memory_found", new {
            searchedCount = candidates.Count,
            adoptedCount = usefulMemories.Count,
            message = usefulMemories.Count > 0
                ? $"{candidates.Count}개 검색, {usefulMemories.Count}개 채택"
                : "연관성 높은 메모리 없음"
        });
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Memory search with batch evaluation failed");
        await WriteSSEEvent("memory_found", new { searchedCount = 0, adoptedCount = 0, message = "검색 오류" });
    }

    return usefulMemories.Select(m => (m.Title, m.Content)).ToList();
}
```

### LLM 배치 평가 프롬프트

```csharp
public static string GetBatchMemoryRelevancePrompt(
    string userPrompt,
    List<(string Title, string Summary, int Index)> candidates)
{
    var candidateList = string.Join("\n", candidates.Select(c =>
        $"[{c.Index}] 제목: {c.Title}\n    내용: {c.Summary}"));

    return $@"다음 사용자 요청에 가장 관련성이 높은 참고자료 번호를 최대 3개까지 선택하세요.
관련성이 낮으면 더 적게 선택해도 됩니다. 관련 자료가 없으면 '없음'이라고 답하세요.

## 사용자 요청
{userPrompt}

## 후보 참고자료
{candidateList}

## 응답 형식
- 관련성 높은 번호만 쉼표로 구분하여 출력 (예: 1, 3)
- 관련 자료 없으면: 없음

선택:";
}
```

### 키워드 추출 프롬프트

```csharp
public static string GetSearchKeywordsExtractionPrompt(string userPrompt) => $@"
다음 텍스트에서 메모리 검색에 사용할 핵심 키워드 3개를 추출하세요.
각 키워드는 30자 이하, 쉼표로 구분합니다.

텍스트: {userPrompt}

키워드 (쉼표 구분):";
```

### SSE Phase 이벤트 패턴

```javascript
// 클라이언트에서 Phase 이벤트 처리
eventSource.onmessage = (event) => {
    const data = JSON.parse(event.data);

    if (data.phase) {
        updateProgress(data.message);  // 진행 상황 표시
    }

    if (data.memory_found) {
        if (data.adoptedCount > 0) {
            showToast('info', '메모리 검색',
                `${data.searchedCount}개 검색, ${data.adoptedCount}개 참고`);
        }
    }

    if (data.content) {
        fullContent += data.content;
    }
};
```

## 주의사항

1. **토큰 제한**: 긴 컨텐츠는 청크로 분할하여 처리
2. **타임아웃**: 복잡한 요청은 타임아웃을 충분히 설정
3. **에러 핸들링**: LLM 응답 실패 시 폴백 로직 구현
4. **스트리밍**: SSE 사용 시 `X-Accel-Buffering: no` 헤더 필수
5. **배치 평가**: 메모리 검색 결과가 많을 때 LLM 배치 평가로 최적 선택
