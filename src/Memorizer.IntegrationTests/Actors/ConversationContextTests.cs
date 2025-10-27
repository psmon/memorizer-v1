using Akka.Actor;
using Akka.TestKit.Xunit2;
using Memorizer.Actors;
using Memorizer.Models;
using Memorizer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Memorizer.Settings;
using System.Text;
using Npgsql;
using Akka.Hosting;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Memorizer.IntegrationTests.Actors;

/// <summary>
/// Integration tests for conversation context management in ChatBot actors
/// </summary>
public class ConversationContextTests : TestKit, IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IStorage? _storage;
    private ILlmService? _llmService;
    private IActorRef? _searchMemoryActor;
    private IActorRef? _decisionActor;
    private readonly HttpClient _httpClient;
    private string? _authCookie;

    public ConversationContextTests(ITestOutputHelper output)
    {
        _output = output;
        _httpClient = new HttpClient();

        // Build configuration from appsettings.json
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        // Build service provider with manual configuration
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_configuration);
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add HTTP client
        services.AddHttpClient();

        // Manually configure Storage
        services.AddSingleton(sp =>
        {
            string connectionString = _configuration.GetConnectionString("Storage")
                ?? throw new ArgumentNullException("Storage Connection String");
            var sourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            sourceBuilder.UseVector();
            return sourceBuilder.Build();
        });
        services.AddSingleton<IStorage, Storage>();

        // Manually configure Embedding settings and service
        services.AddSingleton<EmbeddingSettings>(sp =>
        {
            var settings = _configuration.GetSection("Embeddings").Get<EmbeddingSettings>()
                ?? throw new ArgumentNullException("Embeddings Settings");
            _output.WriteLine($"Embedding Config - Type: {settings.Type}, URL: {settings.ApiUrl}, Model: {settings.Model}");
            return settings;
        });

        services.AddSingleton<IEmbeddingService>(sp =>
        {
            var settings = sp.GetRequiredService<EmbeddingSettings>();
            var logger = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            string apiType = settings.Type.ToLower();
            _output.WriteLine($"Creating {apiType} embedding service");

            if (apiType.Equals("custom"))
            {
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new CustomEmbeddingService(httpClient, settings, logger.CreateLogger<CustomEmbeddingService>());
            }
            else
            {
                // Default to Ollama for backward compatibility
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new OllamaEmbeddingService(httpClient, settings, logger.CreateLogger<OllamaEmbeddingService>());
            }
        });

        // Manually configure LLM settings and service
        services.AddSingleton<LlmSettings>(sp =>
        {
            var settings = _configuration.GetSection("Llm").Get<LlmSettings>()
                ?? throw new ArgumentNullException("LLM Settings");
            _output.WriteLine($"LLM Config - Type: {settings.Type}, URL: {settings.ApiUrl}, Model: {settings.Model}");
            return settings;
        });

        services.AddSingleton<ILlmService>(sp =>
        {
            var settings = sp.GetRequiredService<LlmSettings>();
            var logger = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            string apiType = settings.Type.ToLower();
            _output.WriteLine($"Creating {apiType} LLM service");

            if (apiType.Equals("ollama"))
            {
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new OllamaLlmService(httpClient, settings, logger.CreateLogger<OllamaLlmService>());
            }
            else
            {
                // Default to Custom
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new CustomLlmService(httpClient, settings, logger.CreateLogger<CustomLlmService>());
            }
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        _output.WriteLine("Initializing ConversationContextTests");
        _storage = _serviceProvider.GetRequiredService<IStorage>();
        _llmService = _serviceProvider.GetRequiredService<ILlmService>();

        // Authenticate to get cookie for API access
        await AuthenticateAsync();

        // Create SearchMemoryActor
        _searchMemoryActor = Sys.ActorOf(SearchMemoryActor.Props(_storage, _llmService), "search-memory-actor");

        // Create DecisionActor
        _decisionActor = Sys.ActorOf(DecisionActor.Props(_llmService), "decision-actor");

        // Add sample memories for testing
        await AddTestMemories();
    }

    private async Task AuthenticateAsync()
    {
        try
        {
            var authRequest = new { username = "admin", password = "admin123" };
            var json = JsonSerializer.Serialize(authRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:5001/api/auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                _authCookie = response.Headers.GetValues("Set-Cookie")?.FirstOrDefault();
                _output.WriteLine("Authentication successful");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Authentication failed: {ex.Message}");
        }
    }

    private async Task AddTestMemories()
    {
        // Add memories about Reactive Streams
        var memory1 = new Memory
        {
            Id = Guid.NewGuid(),
            Type = "reference",
            Source = "test",
            Title = "Reactive Streams Programming",
            Text = "Reactive Streams is an initiative to provide a standard for asynchronous stream processing with non-blocking back pressure. It includes interfaces, methods and protocols that describe the operations and entities for achieving the goal of asynchronous streams of data with non-blocking back pressure.",
            Tags = new[] { "reactive", "streams", "programming", "async" },
            Confidence = 1.0,
            CreatedAt = DateTime.UtcNow
        };

        var memory2 = new Memory
        {
            Id = Guid.NewGuid(),
            Type = "reference",
            Source = "test",
            Title = "AI Development Methodology",
            Text = "AI development methodology includes several phases: problem definition, data collection and preparation, model selection, training, validation, testing, deployment, and monitoring. Each phase requires careful planning and execution to ensure successful AI system implementation.",
            Tags = new[] { "AI", "methodology", "development", "machine learning" },
            Confidence = 1.0,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _storage!.StoreMemory(
                memory1.Type,
                memory1.Text!,
                memory1.Source,
                memory1.Tags,
                memory1.Confidence,
                memory1.Title);
            await _storage!.StoreMemory(
                memory2.Type,
                memory2.Text!,
                memory2.Source,
                memory2.Tags,
                memory2.Confidence,
                memory2.Title);
            _output.WriteLine("Test memories added successfully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to add test memories: {ex.Message}");
        }
    }

    [Fact]
    public async Task ChatBotActor_Should_Maintain_Conversation_Context()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Use TestChatBotSupervisor to capture responses
        var supervisorProps = TestChatBotSupervisor.Props(
            sessionId,
            _searchMemoryActor!,
            _decisionActor!,
            _llmService!,
            TestActor);
        var supervisor = Sys.ActorOf(supervisorProps, $"supervisor-{sessionId}");

        // Act & Assert - First exchange (greeting)
        var request1 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "안녕, 너는 누구야?",
            UserId = "test-user"
        };

        supervisor.Tell(request1, TestActor);
        var response1 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response1);
        Assert.Contains("ASKBot", response1.Message, StringComparison.OrdinalIgnoreCase);
        _output.WriteLine($"First response: {response1.Message}");

        // Second exchange (follow-up question showing context)
        var request2 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "너는 무엇을 할 수 있니?",
            UserId = "test-user"
        };

        supervisor.Tell(request2, TestActor);
        var response2 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response2);
        _output.WriteLine($"Second response: {response2.Message}");

        // Third exchange (acknowledgment showing conversation continuity)
        var request3 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "아 그렇구나, 고마워",
            UserId = "test-user"
        };

        supervisor.Tell(request3, TestActor);
        var response3 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response3);
        _output.WriteLine($"Third response: {response3.Message}");
        // Response should acknowledge the thanks in context

        await Task.CompletedTask; // For async signature
    }

    [Fact]
    public async Task ChatBotActor_Should_Remember_Context_For_Memory_Search()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Use TestChatBotSupervisor to capture responses
        var supervisorProps = TestChatBotSupervisor.Props(
            sessionId,
            _searchMemoryActor!,
            _decisionActor!,
            _llmService!,
            TestActor);
        var supervisor = Sys.ActorOf(supervisorProps, $"supervisor-{sessionId}");

        // Act & Assert - First exchange (express curiosity)
        var request1 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "궁금한게 있어",
            UserId = "test-user"
        };

        supervisor.Tell(request1, TestActor);
        var response1 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response1);
        _output.WriteLine($"First response: {response1.Message}");
        // Should ask what the user is curious about

        // Second exchange (specific question about AI)
        var request2 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "AI 개발방법론이 궁금해",
            UserId = "test-user"
        };

        supervisor.Tell(request2, TestActor);
        var response2 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response2);
        _output.WriteLine($"Second response: {response2.Message}");
        // Should search memory and provide information about AI development methodology
        Assert.True(response2.Type == ResponseType.MemoryBased || response2.Type == ResponseType.General);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task ChatBotActor_Should_Handle_Multiple_Topics_With_Context()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Use TestChatBotSupervisor to capture responses
        var supervisorProps = TestChatBotSupervisor.Props(
            sessionId,
            _searchMemoryActor!,
            _decisionActor!,
            _llmService!,
            TestActor);
        var supervisor = Sys.ActorOf(supervisorProps, $"supervisor-{sessionId}");

        // Test multiple topics in sequence
        var topics = new[]
        {
            "Reactive Streams에 대해 알려줘",
            "그것의 주요 특징은 뭐야?",  // Referring to previous topic
            "AI 개발방법론은 뭐가 있을까?",  // New topic
            "첫 번째 단계가 뭐야?"  // Referring to AI methodology
        };

        ChatBotResponse? previousResponse = null;
        for (int i = 0; i < topics.Length; i++)
        {
            var request = new UserChatRequest
            {
                SessionId = sessionId,
                Message = topics[i],
                UserId = "test-user"
            };

            supervisor.Tell(request, TestActor);
            var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
            Assert.NotNull(response);
            _output.WriteLine($"Topic {i + 1} response: {response.Message}");

            if (i == 1)
            {
                // Second question should understand "그것" refers to Reactive Streams
                Assert.Contains(new[] { "back pressure", "asynchronous", "stream", "비동기", "스트림" },
                    keyword => response.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }
            else if (i == 3)
            {
                // Fourth question should understand it's about AI methodology phases
                Assert.Contains(new[] { "problem", "definition", "문제", "정의", "첫" },
                    keyword => response.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            previousResponse = response;
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task ChatBotActor_Should_Manage_Conversation_History_Limit()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Use TestChatBotSupervisor to capture responses
        var supervisorProps = TestChatBotSupervisor.Props(
            sessionId,
            _searchMemoryActor!,
            _decisionActor!,
            _llmService!,
            TestActor);
        var supervisor = Sys.ActorOf(supervisorProps, $"supervisor-{sessionId}");

        // Act - Send more than 10 messages to test pruning
        for (int i = 1; i <= 12; i++)
        {
            var request = new UserChatRequest
            {
                SessionId = sessionId,
                Message = $"This is message number {i}",
                UserId = "test-user"
            };

            supervisor.Tell(request, TestActor);
            var response = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
            Assert.NotNull(response);
            _output.WriteLine($"Message {i} response received");
        }

        // Send a message that references early conversation
        var finalRequest = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Do you remember what we talked about in the beginning?",
            UserId = "test-user"
        };

        supervisor.Tell(finalRequest, TestActor);
        var finalResponse = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(finalResponse);
        _output.WriteLine($"Final response: {finalResponse.Message}");
        // Should have some context from short-term memory even if early messages are pruned

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Different_Sessions_Should_Have_Isolated_Contexts()
    {
        // Arrange
        var sessionId1 = Guid.NewGuid().ToString();
        var sessionId2 = Guid.NewGuid().ToString();

        // Use TestChatBotSupervisor for both sessions
        var supervisorProps1 = TestChatBotSupervisor.Props(
            sessionId1,
            _searchMemoryActor!,
            _decisionActor!,
            _llmService!,
            TestActor);
        var supervisor1 = Sys.ActorOf(supervisorProps1, $"supervisor-{sessionId1}");

        var supervisorProps2 = TestChatBotSupervisor.Props(
            sessionId2,
            _searchMemoryActor!,
            _decisionActor!,
            _llmService!,
            TestActor);
        var supervisor2 = Sys.ActorOf(supervisorProps2, $"supervisor-{sessionId2}");

        // Act - Session 1: Talk about Reactive Streams
        var request1_1 = new UserChatRequest
        {
            SessionId = sessionId1,
            Message = "Tell me about Reactive Streams",
            UserId = "user1"
        };

        supervisor1.Tell(request1_1, TestActor);
        var response1_1 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response1_1);
        _output.WriteLine($"Session 1 response: {response1_1.Message}");

        // Session 2: Talk about AI
        var request2_1 = new UserChatRequest
        {
            SessionId = sessionId2,
            Message = "Tell me about AI development",
            UserId = "user2"
        };

        supervisor2.Tell(request2_1, TestActor);
        var response2_1 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response2_1);
        _output.WriteLine($"Session 2 response: {response2_1.Message}");

        // Session 1: Ask about "it" - should refer to Reactive Streams
        var request1_2 = new UserChatRequest
        {
            SessionId = sessionId1,
            Message = "What are the main benefits of it?",
            UserId = "user1"
        };

        supervisor1.Tell(request1_2, TestActor);
        var response1_2 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response1_2);
        _output.WriteLine($"Session 1 follow-up: {response1_2.Message}");
        // Should talk about Reactive Streams benefits

        // Session 2: Ask about "it" - should refer to AI development
        var request2_2 = new UserChatRequest
        {
            SessionId = sessionId2,
            Message = "What are the main phases of it?",
            UserId = "user2"
        };

        supervisor2.Tell(request2_2, TestActor);
        var response2_2 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response2_2);
        _output.WriteLine($"Session 2 follow-up: {response2_2.Message}");
        // Should talk about AI development phases

        // Verify contexts are isolated
        Assert.Contains(new[] { "stream", "async", "reactive", "스트림" },
            keyword => response1_2.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(new[] { "phase", "development", "AI", "단계", "개발" },
            keyword => response2_2.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        await Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _httpClient?.Dispose();
        return Task.CompletedTask;
    }
}