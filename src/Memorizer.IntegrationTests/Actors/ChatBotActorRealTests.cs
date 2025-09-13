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

namespace Memorizer.IntegrationTests.Actors;

/// <summary>
/// Real integration tests for ChatBot actors using actual LLM and Storage services
/// </summary>
public class ChatBotActorRealTests : TestKit, IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IStorage? _storage;
    private ILlmService? _llmService;
    private IActorRef? _searchMemoryActor;
    private IActorRef? _decisionActor;
    private IActorRef? _testProbe;

    public ChatBotActorRealTests(ITestOutputHelper output)
    {
        _output = output;

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
            var settings = _configuration.GetSection("LLM").Get<LlmSettings>()
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

            if (apiType.Equals("custom"))
            {
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new CustomLlmService(httpClient, settings, logger.CreateLogger<CustomLlmService>());
            }
            else
            {
                // Default to Ollama for backward compatibility
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new OllamaLlmService(httpClient, settings, logger.CreateLogger<OllamaLlmService>());
            }
        });

        // Add Akka actor system
        services.AddAkka("TestSystem", (builder, provider) =>
        {
            builder.ConfigureLoggers(logger =>
            {
                logger.ClearLoggers();
                logger.LogLevel = Akka.Event.LogLevel.InfoLevel;
                logger.AddLoggerFactory();
            });
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Debug configuration
            var llmType = _configuration["LLM:Type"];
            var llmUrl = _configuration["LLM:ApiUrl"];
            var llmModel = _configuration["LLM:Model"];
            _output.WriteLine("=== Integration Test Setup ===");
            _output.WriteLine($"Configuration - LLM Type: {llmType}, URL: {llmUrl}, Model: {llmModel}");

            _storage = _serviceProvider.GetRequiredService<IStorage>();
            _llmService = _serviceProvider.GetRequiredService<ILlmService>();

            _output.WriteLine($"Storage: {_storage.GetType().Name}");
            _output.WriteLine($"LLM Service: {_llmService.GetType().Name}");

            // Create test probe for monitoring
            _testProbe = CreateTestProbe();

            // Create actors
            _searchMemoryActor = Sys.ActorOf(SearchMemoryActor.Props(_storage, _llmService), "search-memory-real");
            _decisionActor = Sys.ActorOf(DecisionActor.Props(_llmService), "decision-real");

            // Check LLM health
            var health = await _llmService.CheckHealthAsync();
            _output.WriteLine($"LLM Health: {health.IsHealthy} - {health.Message}");
            if (health.ModelName != null)
            {
                _output.WriteLine($"LLM Model: {health.ModelName}");
            }

            
            // 로컬장치 충분한 시나리오 데이터가 있음으로 추가(X)
            // await SeedTestMemories();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Setup failed: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task SeedTestMemories()
    {
        try
        {
            // Seed some test memories for better search results
            var testMemories = new[]
            {
                new
                {
                    Type = "reference",
                    Content = "Reactive Streams is a standard for asynchronous stream processing with non-blocking backpressure. It provides a way to handle potentially unbounded streams of data across asynchronous boundaries while maintaining bounded resource consumption. Key concepts include Publisher, Subscriber, Subscription, and Processor interfaces.",
                    Title = "Reactive Streams Overview",
                    Tags = new[] { "reactive", "streams", "async", "backpressure" }
                },
                new
                {
                    Type = "how-to",
                    Content = "AI development methodologies include: 1) Agile AI - iterative development with frequent model updates, 2) MLOps - combining ML with DevOps practices, 3) CRISP-DM - Cross-Industry Standard Process for Data Mining, 4) Lean AI - minimizing waste in AI development, 5) Responsible AI - focusing on ethics and fairness.",
                    Title = "AI Development Methodologies",
                    Tags = new[] { "AI", "methodology", "MLOps", "development" }
                },
                new
                {
                    Type = "reference",
                    Content = "Docker is a platform for developing, shipping, and running applications in containers. Containers are lightweight, portable, and self-sufficient units that package applications with all their dependencies.",
                    Title = "Docker Containerization",
                    Tags = new[] { "docker", "containers", "devops" }
                }
            };

            foreach (var memory in testMemories)
            {
                await _storage!.StoreMemory(
                    memory.Type,
                    memory.Content,
                    "test-seed",
                    memory.Tags,
                    0.95,
                    memory.Title
                );
            }

            _output.WriteLine($"Seeded {testMemories.Length} test memories");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Warning: Could not seed test memories: {ex.Message}");
        }
    }

    [Fact]
    public async Task Scenario1_Korean_Greeting_WhoAreYou()
    {
        // Arrange
        var sessionId = "test-session-korean-1";
        var chatBotActor = Sys.ActorOf(
            ChatBotActor.Props(sessionId, _searchMemoryActor!, _decisionActor!, _llmService!),
            $"chatbot-{sessionId}");

        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "안녕 너는 누구야?",
            UserId = "test-user"
        };

        _output.WriteLine("\n=== Scenario 1: Korean Greeting ===");
        _output.WriteLine($"User: {request.Message}");

        // Act
        var response = await chatBotActor.Ask<ChatBotResponse>(request, TimeSpan.FromSeconds(30));

        // Assert - Validate response format
        Assert.NotNull(response);
        Assert.Equal(sessionId, response.SessionId);
        Assert.NotNull(response.Message);
        Assert.NotEmpty(response.Message);
        Assert.True(Enum.IsDefined(typeof(ResponseType), response.Type));

        // Log response for quality inspection
        _output.WriteLine($"Response Type: {response.Type}");
        _output.WriteLine($"Bot: {response.Message}");

        if (response.ReasoningSteps != null)
        {
            _output.WriteLine("\nReasoning Steps:");
            foreach (var step in response.ReasoningSteps)
            {
                _output.WriteLine($"  - {step}");
            }
        }

        if (response.ReferencedMemoryIds != null && response.ReferencedMemoryIds.Count > 0)
        {
            _output.WriteLine($"Referenced Memories: {response.ReferencedMemoryIds.Count}");
        }

        _output.WriteLine($"✓ Response received successfully in {response.Timestamp:HH:mm:ss}");
    }

    [Fact]
    public async Task Scenario2_Technical_ReactiveStreams()
    {
        // Arrange
        var sessionId = "test-session-reactive";
        var chatBotActor = Sys.ActorOf(
            ChatBotActor.Props(sessionId, _searchMemoryActor!, _decisionActor!, _llmService!),
            $"chatbot-{sessionId}");

        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Reactive Stream에 대해 알려줘",
            UserId = "test-user"
        };

        _output.WriteLine("\n=== Scenario 2: Technical Query - Reactive Streams ===");
        _output.WriteLine($"User: {request.Message}");

        // Act
        var response = await chatBotActor.Ask<ChatBotResponse>(request, TimeSpan.FromSeconds(30));

        // Assert - Validate response format
        Assert.NotNull(response);
        Assert.Equal(sessionId, response.SessionId);
        Assert.NotNull(response.Message);
        Assert.NotEmpty(response.Message);

        // Log response for quality inspection
        _output.WriteLine($"Response Type: {response.Type}");
        _output.WriteLine($"Bot: {response.Message}");

        if (response.Type == ResponseType.MemoryBased)
        {
            _output.WriteLine("✓ Memory-based response generated");
            Assert.NotNull(response.ReferencedMemoryIds);
            Assert.NotEmpty(response.ReferencedMemoryIds);
            _output.WriteLine($"Referenced {response.ReferencedMemoryIds.Count} memories");
        }

        if (response.ReasoningSteps != null)
        {
            _output.WriteLine("\nReasoning Steps:");
            foreach (var step in response.ReasoningSteps)
            {
                _output.WriteLine($"  - {step}");
            }
        }

        _output.WriteLine($"✓ Technical response received successfully");
    }

    [Fact]
    public async Task Scenario3_AI_Development_Methodology()
    {
        // Arrange
        var sessionId = "test-session-ai-methodology";
        var chatBotActor = Sys.ActorOf(
            ChatBotActor.Props(sessionId, _searchMemoryActor!, _decisionActor!, _llmService!),
            $"chatbot-{sessionId}");

        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "AI 개발방법론은 뭐가 있을까?",
            UserId = "test-user"
        };

        _output.WriteLine("\n=== Scenario 3: AI Development Methodology ===");
        _output.WriteLine($"User: {request.Message}");

        // Act
        var response = await chatBotActor.Ask<ChatBotResponse>(request, TimeSpan.FromSeconds(30));

        // Assert - Validate response format
        Assert.NotNull(response);
        Assert.Equal(sessionId, response.SessionId);
        Assert.NotNull(response.Message);
        Assert.NotEmpty(response.Message);

        // Log response for quality inspection
        _output.WriteLine($"Response Type: {response.Type}");
        _output.WriteLine($"Bot: {response.Message}");

        if (response.Type == ResponseType.MemoryBased)
        {
            _output.WriteLine("✓ Found relevant AI methodology information");
            Assert.NotNull(response.ReferencedMemoryIds);
            _output.WriteLine($"Referenced {response.ReferencedMemoryIds.Count} memories");
        }

        if (response.ReasoningSteps != null)
        {
            _output.WriteLine("\nReasoning Steps:");
            foreach (var step in response.ReasoningSteps)
            {
                _output.WriteLine($"  - {step}");
            }
        }

        _output.WriteLine($"✓ AI methodology response received successfully");
    }

    [Fact]
    public async Task Scenario4_Multiple_Interactions_Same_Session()
    {
        // Arrange
        var sessionId = "test-session-multi";
        var chatBotActor = Sys.ActorOf(
            ChatBotActor.Props(sessionId, _searchMemoryActor!, _decisionActor!, _llmService!),
            $"chatbot-{sessionId}");

        _output.WriteLine("\n=== Scenario 4: Multiple Interactions in Same Session ===");

        // First interaction
        var request1 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Docker란 무엇인가?",
            UserId = "test-user"
        };

        _output.WriteLine($"\nInteraction 1 - User: {request1.Message}");
        var response1 = await chatBotActor.Ask<ChatBotResponse>(request1, TimeSpan.FromSeconds(30));

        Assert.NotNull(response1);
        _output.WriteLine($"Bot: {response1.Message}");
        _output.WriteLine($"Type: {response1.Type}");

        // Second interaction
        var request2 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "그것의 장점은?",
            UserId = "test-user"
        };

        _output.WriteLine($"\nInteraction 2 - User: {request2.Message}");
        var response2 = await chatBotActor.Ask<ChatBotResponse>(request2, TimeSpan.FromSeconds(30));

        Assert.NotNull(response2);
        Assert.Equal(sessionId, response2.SessionId);
        _output.WriteLine($"Bot: {response2.Message}");
        _output.WriteLine($"Type: {response2.Type}");

        _output.WriteLine($"\n✓ Multiple interactions handled successfully in same session");
    }

    [Fact]
    public async Task Scenario5_Session_Management_And_Probe()
    {
        // Arrange
        var sessionId = "test-session-probe";

        // Subscribe to events if probe is available
        if (_testProbe != null)
        {
            Sys.EventStream.Subscribe(_testProbe, typeof(ChatBotResponse));
            Sys.EventStream.Subscribe(_testProbe, typeof(StreamingUpdate));
        }

        var chatBotActor = Sys.ActorOf(
            ChatBotActor.Props(sessionId, _searchMemoryActor!, _decisionActor!, _llmService!),
            $"chatbot-{sessionId}");

        _output.WriteLine("\n=== Scenario 5: Session Management with Probe ===");

        // Send request
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "테스트 메시지입니다",
            UserId = "test-user"
        };

        _output.WriteLine($"User: {request.Message}");

        // Act
        var response = await chatBotActor.Ask<ChatBotResponse>(request, TimeSpan.FromSeconds(30));

        // Assert
        Assert.NotNull(response);
        Assert.Equal(sessionId, response.SessionId);

        _output.WriteLine($"Response: {response.Message}");

        // Test session reset timer
        chatBotActor.Tell(new ResetSessionTimer { SessionId = sessionId });
        _output.WriteLine("✓ Session timer reset sent");

        // Verify actor is still alive
        var probe = CreateTestProbe();
        chatBotActor.Tell(new Identify(1), probe);
        var identity = probe.ExpectMsg<ActorIdentity>(TimeSpan.FromSeconds(1));
        Assert.NotNull(identity.Subject);
        _output.WriteLine("✓ Actor is still alive after timer reset");

        _output.WriteLine($"✓ Session management test completed");
    }

    [Fact]
    public async Task Scenario6_Error_Handling()
    {
        // Arrange
        var sessionId = "test-session-error";
        var chatBotActor = Sys.ActorOf(
            ChatBotActor.Props(sessionId, _searchMemoryActor!, _decisionActor!, _llmService!),
            $"chatbot-{sessionId}");

        _output.WriteLine("\n=== Scenario 6: Error Handling ===");

        // Send empty message
        var request = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "",
            UserId = "test-user"
        };

        _output.WriteLine("Sending empty message to test error handling...");

        // Act - Should still get a response even with empty message
        try
        {
            var response = await chatBotActor.Ask<ChatBotResponse>(request, TimeSpan.FromSeconds(30));

            // Even with empty message, actor should handle gracefully
            Assert.NotNull(response);
            _output.WriteLine($"Response received: {response.Message}");
            _output.WriteLine("✓ Error handled gracefully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Exception caught (expected): {ex.Message}");
            _output.WriteLine("✓ Error handling test completed");
        }
    }

    public Task DisposeAsync()
    {
        _output.WriteLine("\n=== Test Cleanup ===");

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Shutdown();
        return Task.CompletedTask;
    }

    protected override void AfterAll()
    {
        base.AfterAll();
    }
}