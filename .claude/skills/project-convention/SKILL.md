---
name: project-convention
description: Memorizer 프로젝트 컨벤션 스킬. 디렉토리 구조, 네이밍 규칙, 코드 스타일, DB 마이그레이션, 테스트 패턴을 따라야 할 때 사용. 새 기능 추가 시 기존 패턴 준수.
---

# 프로젝트 컨벤션 스킬

Memorizer 프로젝트의 코딩 컨벤션과 구조적 패턴을 안내합니다.

## 기술 스택

- **.NET 9.0** - 풀스택 프레임워크
- **PostgreSQL** - 주 데이터베이스 (벡터 검색 포함)
- **Neo4j** - 그래프 데이터베이스
- **Akka.NET** - 액터 모델
- **Docker Compose** - 로컬 개발 환경

## 디렉토리 구조

```
memorizer-v1/
├── src/
│   ├── Memorizer/                 # 메인 프로젝트
│   │   ├── Actors/                # 액터 모델
│   │   ├── Controllers/           # API 컨트롤러
│   │   ├── Extensions/            # 확장 메서드
│   │   ├── Middleware/            # 미들웨어
│   │   ├── Models/                # 도메인 모델
│   │   ├── Prompts/               # LLM 프롬프트 템플릿
│   │   ├── Services/              # 비즈니스 로직
│   │   ├── Settings/              # 설정 클래스
│   │   ├── Telemetry/             # 텔레메트리
│   │   ├── Tools/                 # MCP 도구
│   │   ├── Views/                 # Razor 뷰
│   │   └── wwwroot/               # 정적 파일
│   ├── Memorizer.IntegrationTests/ # 통합 테스트
│   └── MemorizerProxy/            # 프록시 프로젝트
├── prompt/
│   ├── kr/                        # 한국어 프롬프트
│   │   └── skill-maker/           # 스킬 생성 가이드
│   └── docs/                      # API 문서
├── docker-compose.local-psmon.yml # 로컬 개발 환경
└── .claude/
    └── skills/                    # Claude Code 스킬
```

## 네이밍 컨벤션

### 파일 네이밍

```
Controllers/     → {Feature}Controller.cs
Services/        → I{Feature}Service.cs, {Feature}Service.cs
Actors/          → {Feature}Actor.cs, {Feature}Messages.cs
Models/          → {DomainName}.cs
Settings/        → {Feature}Settings.cs
Views/           → {Controller}/{Action}.cshtml
```

### 클래스 네이밍

```csharp
// 인터페이스
public interface IMemoryService { }
public interface ILlmService { }

// 구현체 (타입별)
public class OllamaLlmService : ILlmService { }
public class OpenAILlmService : ILlmService { }
public class CustomLlmService : ILlmService { }

// 액터
public class ChatBotActor : ReceiveActor { }
public class SearchMemoryActor : ReceiveActor { }

// 컨트롤러
public class MemoryController : ControllerBase { }
public class AskBotController : ControllerBase { }
```

### 메서드 네이밍

```csharp
// 비동기 메서드: ~Async 접미사
Task<T> GetMemoryAsync(Guid id);
Task<List<T>> SearchAsync(string query);
IAsyncEnumerable<string> CompleteStreamingAsync(string prompt);

// 핸들러 메서드: Handle~ 접두사
void HandleUserChatRequest(UserChatRequest request);
void HandleSearchMemoryResponse(SearchMemoryResponse response);

// 팩토리 메서드: Create~ 또는 Props
static Props Props(ILlmService llmService);
static Memory Create(string title, string content);
```

## 설정 관리

### appsettings 구조

```
appsettings.json              # 기본 설정
appsettings.Development.json  # 개발 환경
appsettings.Sam.json          # Sam 환경 (로컬)
appsettings.Production.json   # 프로덕션
```

### Settings 클래스 패턴

```csharp
// Settings/LlmSettings.cs
public class LlmSettings
{
    public string Type { get; set; } = "Ollama";
    public string ApiUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2:latest";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
}

// Program.cs에서 바인딩
builder.Services.Configure<LlmSettings>(
    builder.Configuration.GetSection("LLM"));
```

### 환경변수 오버라이드

```yaml
# docker-compose.local-psmon.yml
environment:
  MEMORIZER_Server__UserName: admin
  MEMORIZER_Server__Password: admin123
  MEMORIZER_LLM__Type: Ollama
  MEMORIZER_LLM__ApiUrl: http://host.docker.internal:11434
```

## 컨트롤러 패턴

### API 컨트롤러

```csharp
[ApiController]
[Route("api/[controller]")]
public class MemoryController : ControllerBase
{
    private readonly IMemoryService _memoryService;
    private readonly ILogger<MemoryController> _logger;

    public MemoryController(IMemoryService memoryService, ILogger<MemoryController> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Memory>> GetMemory(Guid id)
    {
        var memory = await _memoryService.GetByIdAsync(id);
        if (memory == null)
            return NotFound();
        return Ok(memory);
    }

    [HttpPost]
    [Authorize]  // 인증 필요
    public async Task<ActionResult<Memory>> CreateMemory([FromBody] CreateMemoryRequest request)
    {
        var memory = await _memoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetMemory), new { id = memory.Id }, memory);
    }
}
```

### View 컨트롤러

```csharp
public class AskBotViewController : Controller
{
    [HttpGet("/ui/askbot")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/ui/askbot/share/{shortCode}")]
    public async Task<IActionResult> SharedView(string shortCode)
    {
        var data = await _service.GetByShortCodeAsync(shortCode);
        if (data == null)
            return NotFound();
        return View(data);
    }
}
```

## 서비스 패턴

### 인터페이스 우선

```csharp
// 인터페이스 정의
public interface IMemoryService
{
    Task<Memory?> GetByIdAsync(Guid id);
    Task<List<Memory>> SearchAsync(string query, int limit = 10);
    Task<Memory> CreateAsync(CreateMemoryRequest request);
}

// 구현체
public class MemoryService : IMemoryService
{
    private readonly ApplicationDbContext _context;

    public MemoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Memory?> GetByIdAsync(Guid id)
    {
        return await _context.Memories.FindAsync(id);
    }
}
```

### DI 등록 (Extensions)

```csharp
// Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLlmServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection("LLM").Get<LlmSettings>()
            ?? new LlmSettings();

        services.Configure<LlmSettings>(configuration.GetSection("LLM"));

        return settings.Type.ToLower() switch
        {
            "ollama" => services.AddSingleton<ILlmService, OllamaLlmService>(),
            "openai" => services.AddSingleton<ILlmService, OpenAILlmService>(),
            "custom" => services.AddSingleton<ILlmService, CustomLlmService>(),
            _ => throw new InvalidOperationException($"Unknown LLM type: {settings.Type}")
        };
    }
}
```

## 테스트 패턴

### 통합 테스트 위치

```
src/Memorizer.IntegrationTests/
├── Actors/                    # 액터 테스트
│   ├── ChatBotActorTests.cs
│   └── SearchMemoryActorTests.cs
├── Services/                  # 서비스 테스트
└── appsettings.json           # 테스트 설정
```

### 액터 테스트 예시

```csharp
using Akka.TestKit.Xunit2;

public class ChatBotActorTests : TestKit
{
    private readonly ILlmService _llmService;

    public ChatBotActorTests()
    {
        // 실제 LLM 서비스 사용 (Mock 아님)
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        _llmService = CreateLlmService(config);
    }

    [Fact]
    public async Task Should_process_user_query()
    {
        // Arrange
        var searchActor = Sys.ActorOf(SearchMemoryActor.Props(_llmService));
        var decisionActor = Sys.ActorOf(DecisionActor.Props(_llmService));
        var chatBot = Sys.ActorOf(ChatBotActor.Props(
            "test-session", searchActor, decisionActor, _llmService));

        // Act
        chatBot.Tell(new UserChatRequest
        {
            SessionId = "test-session",
            Message = "AI 개발 방법론은?"
        });

        // Assert - 응답 형식 검증 (내용은 LLM에 의존)
        var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(60));
        Assert.NotNull(response.Message);
        Assert.True(response.Message.Length > 0);

        // 콘솔 출력으로 품질 확인
        _output.WriteLine($"Response: {response.Message}");
    }
}
```

## 로컬 개발 환경

### Docker Compose 사용

```bash
# 빌드 및 실행
docker-compose -f docker-compose.local-psmon.yml up --build

# 재빌드
docker-compose -f docker-compose.local-psmon.yml build memorizer

# 로그 확인
docker-compose -f docker-compose.local-psmon.yml logs -f memorizer
```

### 주의사항

1. **dotnet CLI 미사용**: 빌드/실행은 Docker Compose로만
2. **DB 스키마 변경 금지**: PostgreSQL, Neo4j 스키마는 읽기 전용
3. **데이터 쓰기**: API를 통해서만 (직접 INSERT 금지)
4. **인증**:
   - 조회 API: 인증 불필요
   - 등록/수정/삭제: 로그인 필요 (admin/admin123)
   - MCP: X-API-Key 헤더 필요

## 인증 정보 (로컬)

```yaml
UserName: admin
Password: admin123
ApiKey: your-api-key-here
```

## 페이지 URL 구조

```
/                                    # 홈
/ui                                  # 메모리 관리 (View, Edit, Delete, Vector Search)
/ui/blog                             # 블로그 스타일 메모리 리스트
/ui/view/{id}                        # 메모리 상세 보기
/ui/askbot                           # AskBot 챗봇
/ui/askbot/share/{shortCode}         # 공유된 챗봇 대화
/ui/sharelist                        # 공유된 챗봇 대화 목록
/ui/architecture                     # 메모리 아키텍처 생성
/ui/architecture/share/{shortCode}   # 공유된 아키텍처
/ui/architecture/shares              # 공유된 아키텍처 목록
/ui/prd                              # PRD Maker (이벤트 스토밍 + 예제 맵핑)
/ui/prd/share/{shortCode}            # 공유된 PRD 분석
/ui/prd/shares                       # 공유된 PRD 분석 목록
/ui/graph                            # 그래프 뷰어
/ui/config                           # 설정 페이지 (스크립트 관리)
```

## 주요 서비스 인터페이스 참조

| 서비스 | 파일 위치 | 용도 |
|--------|----------|------|
| ILlmService | Services/ILlmService.cs | LLM 텍스트 생성 |
| ILlmExService | Services/ILlmExService.cs | 고급 분석용 LLM |
| IEmbeddingService | Services/IEmbeddingService.cs | 텍스트 임베딩 |
| IMultiModalService | Services/IMultiModalService.cs | 이미지+텍스트 분석 |
| IGraphRepository | Services/GraphRepository.cs | Neo4j 그래프 DB |
| IStorage | Services/Memory.cs | 메모리 저장소 |

## 프롬프트 템플릿 위치

- `Services/PromptTemplates.cs` - 그래프 쿼리, 관계 추천 프롬프트
- `Services/PrdMakerPrompts.cs` - PRD 이벤트 스토밍, 예제 맵핑 프롬프트
- `Services/ArchitecturePrompts.cs` - 아키텍처 생성 프롬프트
- `Actors/*.cs` - 액터 내 인라인 프롬프트

## 코드 스타일

- **들여쓰기**: 4 spaces
- **중괄호**: 새 줄에 열기
- **using**: 파일 상단에 정렬
- **nullable**: nullable reference types 활성화
- **async/await**: 비동기 메서드는 ~Async 접미사
