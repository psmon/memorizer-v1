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

## ShapeUp 화이트보드 패턴 (Fabric.js)

### 파일 구조

```
src/Memorizer/
├── Views/ShapeUpView/
│   ├── Index.cshtml          # 메인 뷰 (레이아웃)
│   ├── _ToolPanel.cshtml     # 드로잉 도구 패널
│   ├── _Canvas.cshtml        # 캔버스 + AI 프롬프트 패널
│   ├── _Modals.cshtml        # 토스트, 프로그레스, 공유 모달
│   ├── _ContextMenu.cshtml   # 우클릭 컨텍스트 메뉴
│   ├── _PropertyPopup.cshtml # 속성 팝업
│   └── Share.cshtml          # 공유 페이지
├── wwwroot/css/shapeup.css   # ShapeUp 전용 스타일
└── wwwroot/js/
    ├── shapeup.js            # 캔버스 로직 (드로잉, 화살표, 그룹핑)
    └── shapeup-templates.js  # 보드 템플릿 + AI 생성
```

### Fabric.js 캔버스 초기화

```javascript
// 캔버스 생성
const canvas = new fabric.Canvas('shapeup-canvas', {
    width: container.clientWidth,
    height: container.clientHeight - 100,
    backgroundColor: '#f8f9fa',
    selection: true
});

// 마우스 휠 줌
canvas.on('mouse:wheel', function(opt) {
    const delta = opt.e.deltaY;
    let zoom = canvas.getZoom();
    zoom *= 0.999 ** delta;
    if (zoom > 5) zoom = 5;
    if (zoom < 0.1) zoom = 0.1;
    canvas.zoomToPoint({ x: opt.e.offsetX, y: opt.e.offsetY }, zoom);
    opt.e.preventDefault();
});
```

### 도형 그리기 패턴

```javascript
function handleDrawStart(opt) {
    isDrawing = true;
    const pointer = canvas.getPointer(opt.e);
    startX = pointer.x;
    startY = pointer.y;

    switch (currentTool) {
        case 'rect':
            currentShape = new fabric.Rect({
                left: startX, top: startY,
                width: 0, height: 0,
                fill: fillColor,
                stroke: strokeColor,
                strokeWidth: strokeWidth,
                rx: 8, ry: 8
            });
            canvas.add(currentShape);
            break;
        // circle, line, arrow...
    }
}
```

### 화살표 자석 연결 패턴

```javascript
// 가장 가까운 앵커 포인트 찾기
function getClosestAnchor(shape, point) {
    const anchors = getAnchorPoints(shape);
    return anchors.reduce((closest, anchor) => {
        const dist = Math.hypot(anchor.x - point.x, anchor.y - point.y);
        return dist < closest.dist ? { ...anchor, dist } : closest;
    }, { dist: Infinity });
}

// 앵커 포인트 계산 (top, bottom, left, right)
function getAnchorPoints(obj) {
    const bounds = obj.getBoundingRect();
    return [
        { x: bounds.left + bounds.width / 2, y: bounds.top, anchor: 'top' },
        { x: bounds.left + bounds.width / 2, y: bounds.top + bounds.height, anchor: 'bottom' },
        { x: bounds.left, y: bounds.top + bounds.height / 2, anchor: 'left' },
        { x: bounds.left + bounds.width, y: bounds.top + bounds.height / 2, anchor: 'right' }
    ];
}
```

### SVG BOX 패턴

```javascript
// SVG 아이콘 라이브러리
const svgIcons = {
    'arrow-right': { path: 'M5 12h14m-7-7l7 7-7 7', viewBox: '0 0 24 24' },
    'user': { path: 'M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M12 7a4 4 0 1 0 0-8 4 4 0 0 0 0 8z', viewBox: '0 0 24 24' },
    'cloud': { path: 'M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z', viewBox: '0 0 24 24' },
    // ... more icons
};

// SVG path 유효성 검사
function validateSvgPath(pathData) {
    if (!pathData || typeof pathData !== 'string') return false;
    const validCommands = /^[MmZzLlHhVvCcSsQqTtAa]/;
    const pathCommands = pathData.trim().split(/(?=[MmZzLlHhVvCcSsQqTtAa])/);
    return pathCommands.every(cmd => validCommands.test(cmd.trim()));
}
```

### JSON 파싱 개선 패턴 (LLM 응답)

```javascript
// LLM이 주석을 포함할 수 있으므로 제거
function removeJsonComments(str) {
    let result = '', inString = false, escape = false, i = 0;
    while (i < str.length) {
        const char = str[i], nextChar = str[i + 1];
        if (escape) { result += char; escape = false; i++; continue; }
        if (char === '\\' && inString) { result += char; escape = true; i++; continue; }
        if (char === '"') { inString = !inString; result += char; i++; continue; }
        if (!inString) {
            if (char === '/' && nextChar === '/') {
                while (i < str.length && str[i] !== '\n') i++;
                continue;
            }
            if (char === '/' && nextChar === '*') {
                i += 2;
                while (i < str.length - 1 && !(str[i] === '*' && str[i+1] === '/')) i++;
                i += 2;
                continue;
            }
        }
        result += char; i++;
    }
    return result;
}

// JSON 추출 (코드블록, 균형 중괄호)
function extractJsonFromContent(content) {
    const cleanContent = removeJsonComments(content);
    // Strategy 1: 코드블록에서 추출
    const codeBlockMatch = cleanContent.match(/```(?:json)?\s*([\s\S]*?)```/);
    if (codeBlockMatch) { /* parse */ }
    // Strategy 2: 균형 중괄호 추출
    // Strategy 3: 키워드 기반 매칭
    // ...
}
```

### AI 보드 생성 + 메모리 검색 패턴

```javascript
async function generateFreeBoard() {
    // 1. 키워드 추출 → 메모리 검색 (SSE phase 이벤트)
    // 2. LLM 배치 평가로 상위 3개 메모리 선택
    // 3. 메모리 참조와 함께 보드 생성

    const response = await fetch('/api/shapeup/generate-freeboard', {
        method: 'POST',
        body: JSON.stringify({ prompt, useMemory: true })
    });

    // SSE 스트리밍 처리
    const reader = response.body.getReader();
    while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        const lines = decoder.decode(value).split('\n');
        for (const line of lines) {
            if (line.startsWith('data:')) {
                const data = JSON.parse(line.substring(5));
                if (data.phase) showProgress(data.message);
                if (data.memory_found) showToast(data.message);
                if (data.content) fullContent += data.content;
            }
        }
    }

    // JSON 파싱 후 렌더링
    renderFreeBoard(fullContent);
}
```

### PRD 와이어프레임 연동 패턴

```javascript
// PRD 페이지에서 ShapeUp으로 전환
function generateWireframe() {
    const wireframePrompt = buildWireframePrompt(); // PRD 데이터 기반
    sessionStorage.setItem('prdWireframePrompt', wireframePrompt);
    window.location.href = '/ui/shapeup?prdWireframe=true';
}

// ShapeUp에서 PRD 와이어프레임 모드 감지
function checkPrdWireframeMode() {
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('prdWireframe') === 'true') {
        const prompt = sessionStorage.getItem('prdWireframePrompt');
        if (prompt) {
            sessionStorage.removeItem('prdWireframePrompt');
            openAIPanel();
            document.getElementById('ai-prompt').value = prompt;
            showToast('info', 'PRD 와이어프레임', 'Generate 버튼을 클릭하세요.');
        }
    }
}
```

## 주의사항

1. **SSE 버퍼링**: Nginx 사용 시 `X-Accel-Buffering: no` 필수
2. **인증**: 조회 페이지는 인증 불필요, 등록/수정은 인증 필요
3. **XSS 방지**: Markdown 렌더링 시 DOMPurify로 sanitize
4. **모바일 대응**: 테이블, 다이어그램에 가로 스크롤 적용
5. **에러 처리**: 스트리밍 중 에러 발생 시 사용자에게 알림
6. **Fabric.js 커스텀 속성**: `toObject` 오버라이드로 공유/저장 시 커스텀 속성 보존
7. **JSON 파싱**: LLM 응답에 주석이 포함될 수 있으므로 제거 후 파싱
