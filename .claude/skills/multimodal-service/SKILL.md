---
name: multimodal-service
description: Memorizer 프로젝트의 멀티모달(이미지+텍스트) 서비스 스킬. 이미지 분석, 비전 LLM 통합이 필요할 때 사용. IMultiModalService 인터페이스, AskBot 멀티모달 패턴 참조.
---

# 멀티모달 서비스 스킬

Memorizer 프로젝트에서 이미지와 텍스트를 함께 처리하는 멀티모달 기능을 구현하는 방법을 안내합니다.

## 프로젝트 구조

```
src/Memorizer/
├── Services/
│   ├── IMultiModalService.cs        # 멀티모달 서비스 인터페이스
│   ├── MultiModalCustomService.cs   # Custom API 구현체
│   └── MultiModalOpenAIService.cs   # OpenAI 구현체
├── Settings/
│   └── MultiModalSettings.cs        # 멀티모달 설정
├── Actors/
│   └── ChatBotActor.cs              # 멀티모달 요청 처리
└── Views/
    └── AskBotView/
        └── Index.cshtml             # 이미지 업로드 UI
```

## IMultiModalService 인터페이스

```csharp
public interface IMultiModalService : IDisposable
{
    /// <summary>
    /// 이미지와 텍스트 프롬프트를 함께 분석
    /// </summary>
    Task<string> AnalyzeImageAsync(
        byte[] imageData,
        string prompt,
        string imageFormat = "jpeg",
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 서비스 상태 확인
    /// </summary>
    Task<MultiModalHealthResult> CheckHealthAsync(
        CancellationToken cancellationToken = default
    );
}

public class MultiModalHealthResult
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public string? ErrorDetails { get; set; }
}
```

## 설정 방법

### appsettings.json

```json
{
  "MultiModal": {
    "Type": "Custom",
    "ApiUrl": "http://localhost:11434",
    "Model": "qwen2-vl:7b",
    "Timeout": "00:03:00",
    "MaxImageSizeBytes": 3145728
  }
}
```

### 환경변수 오버라이드

```yaml
# docker-compose
environment:
  MEMORIZER_MultiModal__Type: Custom
  MEMORIZER_MultiModal__ApiUrl: http://host.docker.internal:11434
  MEMORIZER_MultiModal__Model: qwen2-vl:7b
```

## 사용 예시

### 1. 기본 이미지 분석

```csharp
public class ImageAnalysisService
{
    private readonly IMultiModalService _multiModalService;

    public ImageAnalysisService(IMultiModalService multiModalService)
    {
        _multiModalService = multiModalService;
    }

    public async Task<string> AnalyzeImage(byte[] imageData, string question)
    {
        return await _multiModalService.AnalyzeImageAsync(
            imageData,
            question,
            "jpeg"
        );
    }
}
```

### 2. ChatBotActor에서 멀티모달 처리

```csharp
public class ChatBotActor : ReceiveActor
{
    private readonly IMultiModalService? _multiModalService;

    private void HandleUserChatRequest(UserChatRequest request)
    {
        if (request.IsMultiModal)
        {
            AddReasoningStep("Processing multi-modal request (image + text)...");
            GenerateMultiModalResponse(request, Sender);
            return;
        }

        // 일반 텍스트 요청 처리...
    }

    private async Task<ChatBotResponse> GenerateMultiModalResponseAsync(UserChatRequest request)
    {
        if (_multiModalService == null)
        {
            // 멀티모달 서비스 미설정 시 텍스트 전용 응답
            return await GenerateGeneralResponseAsync(request);
        }

        if (request.ImageData == null || request.ImageData.Length == 0)
        {
            return await GenerateGeneralResponseAsync(request);
        }

        // 이미지 분석
        var response = await _multiModalService.AnalyzeImageAsync(
            request.ImageData,
            request.Message,
            request.ImageFormat ?? "png"
        );

        return new ChatBotResponse
        {
            SessionId = request.SessionId,
            Message = response,
            Type = ResponseType.General,
            ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
        };
    }
}
```

## 프론트엔드 이미지 업로드

### 파일 선택 + 드래그앤드롭 + 클립보드

```html
<div class="image-upload-area" id="dropZone">
    <input type="file" id="imageInput" accept="image/jpeg,image/png" hidden>
    <button type="button" onclick="document.getElementById('imageInput').click()">
        이미지 선택
    </button>
    <p>또는 드래그앤드롭 / Ctrl+V로 붙여넣기</p>
</div>

<div id="imagePreview" style="display: none;">
    <img id="previewImg" src="" alt="Preview">
    <button onclick="removeImage()">제거</button>
</div>

<script>
let currentImageData = null;
let currentImageFormat = null;
const MAX_SIZE = 3 * 1024 * 1024; // 3MB

// 파일 선택
document.getElementById('imageInput').addEventListener('change', handleFileSelect);

// 드래그앤드롭
const dropZone = document.getElementById('dropZone');
dropZone.addEventListener('dragover', (e) => {
    e.preventDefault();
    dropZone.classList.add('dragover');
});
dropZone.addEventListener('dragleave', () => dropZone.classList.remove('dragover'));
dropZone.addEventListener('drop', handleDrop);

// 클립보드 붙여넣기
document.addEventListener('paste', handlePaste);

function handleFileSelect(e) {
    const file = e.target.files[0];
    if (file) processFile(file);
}

function handleDrop(e) {
    e.preventDefault();
    dropZone.classList.remove('dragover');
    const file = e.dataTransfer.files[0];
    if (file) processFile(file);
}

function handlePaste(e) {
    const items = e.clipboardData.items;
    for (const item of items) {
        if (item.type.startsWith('image/')) {
            const file = item.getAsFile();
            processFile(file);
            break;
        }
    }
}

function processFile(file) {
    // 포맷 검증
    if (!['image/jpeg', 'image/png'].includes(file.type)) {
        alert('JPG, PNG 파일만 허용됩니다.');
        return;
    }

    // 크기 검증
    if (file.size > MAX_SIZE) {
        alert('파일 크기는 3MB를 초과할 수 없습니다.');
        return;
    }

    // 미리보기 표시
    const reader = new FileReader();
    reader.onload = (e) => {
        document.getElementById('previewImg').src = e.target.result;
        document.getElementById('imagePreview').style.display = 'block';

        // Base64 데이터 저장
        currentImageData = e.target.result.split(',')[1];
        currentImageFormat = file.type.split('/')[1];
    };
    reader.readAsDataURL(file);
}

function removeImage() {
    currentImageData = null;
    currentImageFormat = null;
    document.getElementById('imagePreview').style.display = 'none';
    document.getElementById('imageInput').value = '';
}

// 요청 전송
async function sendMessage() {
    const message = document.getElementById('messageInput').value;

    const requestBody = {
        message: message,
        sessionId: sessionId
    };

    // 이미지가 있으면 멀티모달 요청
    if (currentImageData) {
        requestBody.imageData = currentImageData;
        requestBody.imageFormat = currentImageFormat;
        requestBody.isMultiModal = true;
    }

    const response = await fetch('/api/askbot/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(requestBody)
    });

    // 응답 처리...
    removeImage(); // 전송 후 이미지 초기화
}
</script>

<style>
.image-upload-area {
    border: 2px dashed #ccc;
    padding: 20px;
    text-align: center;
}
.image-upload-area.dragover {
    border-color: #2196f3;
    background-color: #e3f2fd;
}
#imagePreview img {
    max-width: 200px;
    max-height: 200px;
}
</style>
```

## 서버 측 요청 처리

### Controller

```csharp
[HttpPost("chat")]
public async Task<ActionResult<ChatBotResponse>> Chat([FromBody] ChatRequest request)
{
    var userRequest = new UserChatRequest
    {
        SessionId = request.SessionId,
        Message = request.Message,
        IsMultiModal = request.IsMultiModal
    };

    // 이미지 데이터가 있으면 Base64 디코딩
    if (request.IsMultiModal && !string.IsNullOrEmpty(request.ImageData))
    {
        userRequest.ImageData = Convert.FromBase64String(request.ImageData);
        userRequest.ImageFormat = request.ImageFormat ?? "jpeg";
    }

    var response = await _chatBotActor.Ask<ChatBotResponse>(userRequest);
    return Ok(response);
}

public class ChatRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsMultiModal { get; set; }
    public string? ImageData { get; set; }  // Base64
    public string? ImageFormat { get; set; }
}
```

## DI 등록

```csharp
// Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddMultiModalServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var settings = configuration.GetSection("MultiModal").Get<MultiModalSettings>();

    if (settings == null || string.IsNullOrEmpty(settings.ApiUrl))
    {
        // 멀티모달 미설정 시 null 서비스 등록
        services.AddSingleton<IMultiModalService?>(sp => null);
        return services;
    }

    services.Configure<MultiModalSettings>(configuration.GetSection("MultiModal"));

    return settings.Type.ToLower() switch
    {
        "openai" => services.AddSingleton<IMultiModalService, MultiModalOpenAIService>(),
        "custom" => services.AddSingleton<IMultiModalService, MultiModalCustomService>(),
        _ => services.AddSingleton<IMultiModalService, MultiModalCustomService>()
    };
}
```

## Custom API 요청 형식

Ollama 호환 API:

```csharp
var requestBody = new
{
    model = _settings.Model,
    prompt = prompt,
    images = new[] { Convert.ToBase64String(imageData) },
    stream = false
};

var response = await _httpClient.PostAsJsonAsync("/api/generate", requestBody);
```

## 주의사항

1. **파일 크기**: 클라이언트/서버 양쪽에서 크기 제한 검증
2. **포맷 제한**: JPEG, PNG만 허용 (보안)
3. **Base64 인코딩**: 이미지 데이터는 Base64로 전송
4. **폴백 처리**: 멀티모달 서비스 미설정 시 텍스트 전용 응답
5. **타임아웃**: 이미지 분석은 시간이 걸릴 수 있음 (3분 권장)
6. **메모리 관리**: 대용량 이미지는 메모리 소비 주의
