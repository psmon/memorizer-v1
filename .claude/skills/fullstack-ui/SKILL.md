---
name: fullstack-ui
description: Memorizer 프로젝트의 풀스택 UI 스킬. ASP.NET MVC 뷰, SSE 스트리밍, Markdown 렌더링, Mermaid 다이어그램, 반응형 UI가 필요할 때 사용. AskBot, PRD Maker 패턴 참조.
---

# 풀스택 UI 스킬

Memorizer 프로젝트에서 풀스택 UI를 구현하는 방법을 안내합니다.

## 프로젝트 구조

```
src/Memorizer/
├── Controllers/
│   ├── HomeController.cs         # 홈 페이지
│   ├── BlogController.cs         # 블로그 뷰
│   ├── AskBotViewController.cs   # 챗봇 UI
│   ├── AskBotController.cs       # 챗봇 API
│   ├── PrdMakerController.cs     # PRD Maker
│   └── GraphViewController.cs    # 그래프 뷰
├── Views/
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Blog/
│   │   └── Index.cshtml
│   ├── AskBotView/
│   │   ├── Index.cshtml          # 챗봇 메인
│   │   └── SharedView.cshtml     # 공유 페이지
│   ├── PrdMaker/
│   │   ├── Index.cshtml          # PRD Maker 메인
│   │   └── SharedView.cshtml     # PRD 공유 페이지
│   └── Shared/
│       ├── _Layout.cshtml        # 공통 레이아웃
│       └── _ViewImports.cshtml
└── wwwroot/
    ├── css/
    ├── js/
    └── lib/                      # 라이브러리
```

## 페이지 URL 구조

```
/                              # 홈
/ui/blog                       # 블로그 리스트
/ui/view/{id}                  # 메모리 상세 보기
/ui/askbot                     # 챗봇
/ui/askbot/share/{shortCode}   # 챗봇 공유
/ui/prdmaker                   # PRD Maker
/ui/prdmaker/share/{shortCode} # PRD 공유
/ui/graph                      # 그래프 뷰
```

## SSE 스트리밍 패턴

### 서버 측 (Controller)

```csharp
[HttpGet("stream")]
public async Task StreamResponse(
    [FromQuery] string prompt,
    CancellationToken cancellationToken)
{
    // SSE 헤더 설정
    Response.ContentType = "text/event-stream";
    Response.Headers["Cache-Control"] = "no-cache";
    Response.Headers["X-Accel-Buffering"] = "no";
    Response.Headers["Connection"] = "keep-alive";

    try
    {
        await foreach (var chunk in _llmService.CompleteStreamingAsync(prompt, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // SSE 형식으로 전송
            var data = JsonSerializer.Serialize(new { content = chunk });
            await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        // 완료 신호
        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
    }
    catch (OperationCanceledException)
    {
        // 클라이언트 연결 끊김
        _logger.LogInformation("SSE connection closed by client");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "SSE streaming error");
        var error = JsonSerializer.Serialize(new { error = ex.Message });
        await Response.WriteAsync($"data: {error}\n\n", cancellationToken);
    }
}
```

### 클라이언트 측 (JavaScript)

```javascript
async function streamResponse(prompt, onChunk, onDone, onError) {
    const eventSource = new EventSource(`/api/stream?prompt=${encodeURIComponent(prompt)}`);

    eventSource.onmessage = function(event) {
        if (event.data === '[DONE]') {
            eventSource.close();
            onDone();
            return;
        }

        try {
            const data = JSON.parse(event.data);
            if (data.error) {
                onError(data.error);
                eventSource.close();
                return;
            }
            onChunk(data.content);
        } catch (e) {
            console.error('Parse error:', e);
        }
    };

    eventSource.onerror = function(error) {
        eventSource.close();
        onError(error);
    };

    return eventSource; // 취소를 위해 반환
}

// 사용 예시
let content = '';
const eventSource = streamResponse(
    userPrompt,
    (chunk) => {
        content += chunk;
        updateUI(content);
    },
    () => {
        console.log('Streaming complete');
        renderMarkdown(content);
    },
    (error) => {
        console.error('Stream error:', error);
    }
);

// 취소
function cancelStream() {
    if (eventSource) {
        eventSource.close();
    }
}
```

## Markdown 렌더링

### marked.js 사용

```html
<!-- 라이브러리 포함 -->
<script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/dompurify/dist/purify.min.js"></script>

<script>
// Markdown 렌더링 설정
marked.setOptions({
    breaks: true,
    gfm: true,
    highlight: function(code, lang) {
        if (lang && hljs.getLanguage(lang)) {
            return hljs.highlight(code, { language: lang }).value;
        }
        return hljs.highlightAuto(code).value;
    }
});

function renderMarkdown(markdown) {
    const html = marked.parse(markdown);
    const sanitizedHtml = DOMPurify.sanitize(html);
    document.getElementById('content').innerHTML = sanitizedHtml;

    // Mermaid 다이어그램 렌더링
    renderMermaidDiagrams();
}
</script>
```

## Mermaid 다이어그램

### 설정 및 렌더링

```html
<script src="https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js"></script>

<script>
// Mermaid 초기화
mermaid.initialize({
    startOnLoad: false,
    theme: 'default',
    securityLevel: 'loose'
});

async function renderMermaidDiagrams() {
    const codeBlocks = document.querySelectorAll('pre code.language-mermaid');

    for (const block of codeBlocks) {
        const code = block.textContent;
        const container = document.createElement('div');
        container.className = 'mermaid-diagram';

        try {
            const { svg } = await mermaid.render('mermaid-' + Date.now(), code);
            container.innerHTML = svg;
            block.parentNode.replaceWith(container);
        } catch (error) {
            console.error('Mermaid render error:', error);
            container.innerHTML = `<pre class="error">${error.message}</pre>`;
            block.parentNode.replaceWith(container);
        }
    }
}
</script>

<style>
.mermaid-diagram {
    text-align: center;
    margin: 1rem 0;
    overflow-x: auto;
}

.mermaid-diagram svg {
    max-width: 100%;
    height: auto;
}
</style>
```

## 반응형 UI 패턴

### CSS 그리드 레이아웃

```css
/* 기본 레이아웃 */
.container {
    display: grid;
    grid-template-columns: 1fr;
    gap: 1rem;
    padding: 1rem;
    max-width: 1200px;
    margin: 0 auto;
}

/* 태블릿 이상 */
@media (min-width: 768px) {
    .container {
        grid-template-columns: 250px 1fr;
    }
}

/* 데스크탑 */
@media (min-width: 1024px) {
    .container {
        grid-template-columns: 300px 1fr 250px;
    }
}
```

### 반응형 테이블

```css
/* 테이블 가독성 개선 */
.responsive-table {
    width: 100%;
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
}

.responsive-table table {
    min-width: 600px;
    border-collapse: collapse;
}

.responsive-table td,
.responsive-table th {
    min-width: 100px;
    padding: 0.75rem;
    border: 1px solid #ddd;
    /* 최소 15글자 */
    min-width: 15ch;
}

/* 모바일에서 카드 스타일 */
@media (max-width: 600px) {
    .responsive-table table {
        display: block;
    }

    .responsive-table tr {
        display: flex;
        flex-direction: column;
        margin-bottom: 1rem;
        border: 1px solid #ddd;
        border-radius: 8px;
    }

    .responsive-table td {
        display: flex;
        justify-content: space-between;
        border: none;
        border-bottom: 1px solid #eee;
    }
}
```

## 단계별 진행 UI 패턴

PRD Maker에서 사용하는 단계별 진행 패턴:

```html
<div class="steps-container">
    <div class="step" id="step1">
        <h3>1단계: 이벤트 스토밍</h3>
        <div class="step-content" id="step1-content"></div>
        <button class="next-btn" id="step1-next" disabled>다음 단계</button>
    </div>

    <div class="step hidden" id="step2">
        <h3>2단계: 예제 맵핑</h3>
        <div class="step-content" id="step2-content"></div>
        <button class="next-btn" id="step2-next" disabled>다음 단계</button>
    </div>

    <div class="step hidden" id="step3">
        <h3>3단계: 요구사항 보완</h3>
        <div class="step-content" id="step3-content"></div>
        <button class="share-btn" id="share-btn" disabled>공유하기</button>
    </div>
</div>

<script>
let currentStep = 1;

async function processStep(step) {
    const contentEl = document.getElementById(`step${step}-content`);
    const nextBtn = document.getElementById(`step${step}-next`);

    // 스트리밍으로 내용 채우기
    await streamResponse(
        getPromptForStep(step),
        (chunk) => {
            contentEl.innerHTML += chunk;
            renderMarkdown(contentEl.innerHTML);
        },
        () => {
            nextBtn.disabled = false;
            renderMermaidDiagrams();
        },
        (error) => {
            contentEl.innerHTML += `<div class="error">${error}</div>`;
        }
    );
}

function nextStep() {
    document.getElementById(`step${currentStep}`).classList.add('completed');
    currentStep++;
    document.getElementById(`step${currentStep}`).classList.remove('hidden');
    processStep(currentStep);
}
</script>

<style>
.step {
    margin: 1rem 0;
    padding: 1rem;
    border: 1px solid #ddd;
    border-radius: 8px;
}

.step.hidden {
    display: none;
}

.step.completed {
    border-color: #4caf50;
    background-color: #f1f8e9;
}

.next-btn, .share-btn {
    margin-top: 1rem;
    padding: 0.5rem 1rem;
    background-color: #2196f3;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
}

.next-btn:disabled, .share-btn:disabled {
    background-color: #ccc;
    cursor: not-allowed;
}
</style>
```

## 공유 기능 패턴

### 숏링크 생성

```csharp
[HttpPost("share")]
public async Task<ActionResult<ShareResponse>> CreateShare([FromBody] ShareRequest request)
{
    // 제목 생성 (LLM 활용)
    var title = await _llmService.GenerateTitle(
        request.Content,
        "share",
        maxTitleLength: 30
    );

    // 숏코드 생성
    var shortCode = GenerateShortCode();

    // 저장
    var share = new SharedContent
    {
        ShortCode = shortCode,
        Title = title,
        Content = request.Content,
        CreatedAt = DateTime.UtcNow
    };

    await _dbContext.SharedContents.AddAsync(share);
    await _dbContext.SaveChangesAsync();

    return Ok(new ShareResponse
    {
        ShortCode = shortCode,
        Url = $"/ui/prdmaker/share/{shortCode}"
    });
}

private string GenerateShortCode()
{
    const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    var random = new Random();
    return new string(Enumerable.Repeat(chars, 8)
        .Select(s => s[random.Next(s.Length)]).ToArray());
}
```

### 공유 뷰 페이지

```html
@model SharedContentViewModel

<div class="shared-view">
    <h1>@Model.Title</h1>
    <p class="meta">공유일: @Model.CreatedAt.ToString("yyyy-MM-dd HH:mm")</p>

    <div class="content" id="content">
        <!-- Markdown 내용이 렌더링됨 -->
    </div>
</div>

<script>
document.addEventListener('DOMContentLoaded', function() {
    const content = @Html.Raw(JsonSerializer.Serialize(Model.Content));
    renderMarkdown(content);
});
</script>
```

## View 컴포넌트 패턴

### 부분 뷰 (_Partial)

```html
<!-- Views/Shared/_MarkdownViewer.cshtml -->
<div class="markdown-viewer" id="@ViewBag.ContainerId">
    <div class="toolbar">
        <button onclick="copyContent()">복사</button>
        <button onclick="downloadContent()">다운로드</button>
    </div>
    <div class="content"></div>
</div>

<!-- 사용 -->
@await Html.PartialAsync("_MarkdownViewer", new { ContainerId = "viewer1" })
```

## 주의사항

1. **SSE 버퍼링**: Nginx 사용 시 `X-Accel-Buffering: no` 필수
2. **인증**: 조회 페이지는 인증 불필요, 등록/수정은 인증 필요
3. **XSS 방지**: Markdown 렌더링 시 DOMPurify로 sanitize
4. **모바일 대응**: 테이블, 다이어그램에 가로 스크롤 적용
5. **에러 처리**: 스트리밍 중 에러 발생 시 사용자에게 알림
