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

namespace Memorizer.IntegrationTests.Actors;

/// <summary>
/// Integration tests for last response tracking feature
/// </summary>
public class LastResponseTests : TestKit, IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IStorage? _storage;
    private ILlmService? _llmService;
    private IActorRef? _searchMemoryActor;
    private IActorRef? _decisionActor;

    public LastResponseTests(ITestOutputHelper output)
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

        // Configure Storage
        services.AddSingleton(sp =>
        {
            string connectionString = _configuration.GetConnectionString("Storage")
                ?? throw new ArgumentNullException("Storage Connection String");
            var sourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            sourceBuilder.UseVector();
            return sourceBuilder.Build();
        });
        services.AddSingleton<IStorage, Storage>();

        // Configure Embedding settings and service
        services.AddSingleton<EmbeddingSettings>(sp =>
        {
            var settings = _configuration.GetSection("Embeddings").Get<EmbeddingSettings>()
                ?? throw new ArgumentNullException("Embeddings Settings");
            return settings;
        });

        services.AddSingleton<IEmbeddingService>(sp =>
        {
            var settings = sp.GetRequiredService<EmbeddingSettings>();
            var logger = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            string apiType = settings.Type.ToLower();

            if (apiType.Equals("custom"))
            {
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new CustomEmbeddingService(httpClient, settings, logger.CreateLogger<CustomEmbeddingService>());
            }
            else
            {
                // Default to Ollama
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new OllamaEmbeddingService(httpClient, settings, logger.CreateLogger<OllamaEmbeddingService>());
            }
        });

        // Configure LLM settings and service
        services.AddSingleton<LlmSettings>(sp =>
        {
            var settings = _configuration.GetSection("Llm").Get<LlmSettings>()
                ?? throw new ArgumentNullException("LLM Settings");
            return settings;
        });

        services.AddSingleton<ILlmService>(sp =>
        {
            var settings = sp.GetRequiredService<LlmSettings>();
            var logger = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            string apiType = settings.Type.ToLower();

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
        _output.WriteLine("Initializing LastResponseTests");
        _storage = _serviceProvider.GetRequiredService<IStorage>();
        _llmService = _serviceProvider.GetRequiredService<ILlmService>();

        // Create SearchMemoryActor
        _searchMemoryActor = Sys.ActorOf(SearchMemoryActor.Props(_storage, _llmService), "search-memory-actor");

        // Create DecisionActor
        _decisionActor = Sys.ActorOf(DecisionActor.Props(_llmService), "decision-actor");

        // Add test memories
        await AddTestMemories();
    }

    private async Task AddTestMemories()
    {
        var memory = new Memory
        {
            Id = Guid.NewGuid(),
            Type = "reference",
            Source = "test",
            Title = "SOLID Principles",
            Text = "SOLID is an acronym for five design principles: Single Responsibility (SRP), Open-Closed (OCP), Liskov Substitution (LSP), Interface Segregation (ISP), and Dependency Inversion (DIP). The Dependency Inversion Principle states that high-level modules should not depend on low-level modules; both should depend on abstractions.",
            Tags = new[] { "SOLID", "principles", "software", "design", "DIP" },
            Confidence = 1.0,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _storage!.StoreMemory(
                memory.Type,
                memory.Text!,
                memory.Source,
                memory.Tags,
                memory.Confidence,
                memory.Title);
            _output.WriteLine("Test memory added successfully");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to add test memory: {ex.Message}");
        }
    }

    [Fact]
    public async Task ChatBotActor_Should_Store_And_Reference_Last_Important_Response()
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

        // Act - First message with detailed response
        var request1 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "What are the SOLID principles in software engineering?",
            UserId = "test-user"
        };

        supervisor.Tell(request1, TestActor);
        var response1 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response1);
        _output.WriteLine($"First response (length: {response1.Message.Length}): {response1.Message}");

        // Wait a bit for last response to be updated
        await Task.Delay(1000);

        // Second message - unrelated topic
        var request2 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Let's talk about something else. What time is it?",
            UserId = "test-user"
        };

        supervisor.Tell(request2, TestActor);
        var response2 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response2);
        _output.WriteLine($"Second response: {response2.Message}");

        // Third message - reference back to SOLID principles
        var request3 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Going back to the principles we discussed, which one deals with dependency?",
            UserId = "test-user"
        };

        supervisor.Tell(request3, TestActor);
        var response3 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response3);
        _output.WriteLine($"Third response: {response3.Message}");

        // Assert - Should reference DIP (Dependency Inversion Principle)
        Assert.Contains(new[] { "Dependency", "DIP", "Inversion", "dependency", "의존" },
            keyword => response3.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ChatBotActor_Should_Selectively_Include_Last_Response()
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

        // Act - Send a short greeting (should not be stored as important)
        var request1 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Hi there!",
            UserId = "test-user"
        };

        supervisor.Tell(request1, TestActor);
        var response1 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response1);
        _output.WriteLine($"Greeting response (length: {response1.Message.Length}): {response1.Message}");

        // Send a detailed technical question
        var request2 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Explain the concept of dependency injection in detail",
            UserId = "test-user"
        };

        supervisor.Tell(request2, TestActor);
        var response2 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response2);
        _output.WriteLine($"Technical response (length: {response2.Message.Length}): {response2.Message}");

        // Wait for processing
        await Task.Delay(1000);

        // Follow-up question that could reference the technical response
        var request3 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "How does that relate to the SOLID principles?",
            UserId = "test-user"
        };

        supervisor.Tell(request3, TestActor);
        var response3 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response3);
        _output.WriteLine($"Follow-up response: {response3.Message}");

        // Should show understanding of the context
        Assert.Contains(new[] { "dependency", "injection", "SOLID", "principle", "의존", "주입" },
            keyword => response3.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ChatBotActor_Should_Handle_Memory_Based_Response_Storage()
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

        // Act - Query that should trigger memory search
        var request1 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Tell me about SOLID principles",
            UserId = "test-user"
        };

        supervisor.Tell(request1, TestActor);
        var response1 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response1);
        _output.WriteLine($"Memory-based response type: {response1.Type}");
        _output.WriteLine($"Response: {response1.Message}");

        // Memory-based responses should be prioritized for storage
        if (response1.Type == ResponseType.MemoryBased)
        {
            _output.WriteLine("Response was memory-based, should be stored as important");
        }

        // Follow-up question
        var request2 = new UserChatRequest
        {
            SessionId = sessionId,
            Message = "Which principle is about dependencies?",
            UserId = "test-user"
        };

        supervisor.Tell(request2, TestActor);
        var response2 = ExpectMsg<ChatBotResponse>(TimeSpan.FromSeconds(30));
        Assert.NotNull(response2);
        _output.WriteLine($"Follow-up response: {response2.Message}");

        // Should reference DIP from stored context
        Assert.Contains(new[] { "Dependency", "DIP", "Inversion" },
            keyword => response2.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        await Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}