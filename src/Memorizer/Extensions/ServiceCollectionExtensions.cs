using Akka.Actor;
using Akka.Hosting;
using Memorizer.Actors;
using Memorizer.Services;
using Memorizer.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using Registrator.Net;

namespace Memorizer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMemorizer(
        this IServiceCollection services, bool initialize = true)
    {
        services.AddHttpClient(); // Add HttpClientFactory for service implementations

        // Add named HttpClient for LLM API Controller
        services.AddHttpClient("LLMClient", (sp, client) =>
        {
            var llmSettings = sp.GetRequiredService<LlmSettings>();
            if (llmSettings.ApiUrl != null)
            {
                client.BaseAddress = llmSettings.ApiUrl;
            }
            client.Timeout = llmSettings.Timeout;
        });

        services.AddEmbeddings();
        services.AddLlmServices();
        services.AddMultiModalServices();
        services.AddAskBotSettings();
        services.AddActorSystem();
        services.AddStorage();
        services.AddServerSettings();
        services.AddOAuthServices();
        services.AddGraphServices();
        if(initialize)
            services.AddHostedService<InitializationService>();
        services.AutoRegisterTypesInAssemblies(typeof(Storage).Assembly);
        return services;
    }

    public static IServiceCollection AddEmbeddings(
        this IServiceCollection services)
    {
        services
            .AddSingleton<EmbeddingSettings>(sp =>
                sp.GetRequiredService<IConfiguration>().GetSection("Embeddings").Get<EmbeddingSettings>() ??
                throw new ArgumentNullException("Embeddings Settings"));

        // Check if we should use OpenAI, Custom, or Ollama based on the Type setting
        services.AddSingleton<IEmbeddingService>(sp =>
        {
            var settings = sp.GetRequiredService<EmbeddingSettings>();
            var logger = sp.GetRequiredService<ILoggerFactory>();
            
            string apiType = settings.Type.ToLower();
            
            if (apiType.Equals("openai"))
            {
                return new OpenAIEmbeddingService(settings, logger.CreateLogger<OpenAIEmbeddingService>());
            }
            else if (apiType.Equals("custom"))
            {
                // Use Custom internal network API
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new CustomEmbeddingService(httpClient, settings, logger.CreateLogger<CustomEmbeddingService>());
            }
            else
            {
                // Default to Ollama service
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new OllamaEmbeddingService(httpClient, settings, logger.CreateLogger<OllamaEmbeddingService>());
            }
        });

        return services;
    }

    public static IServiceCollection AddLlmServices(
        this IServiceCollection services)
    {
        services
            .AddSingleton<LlmSettings>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var llmSettings = config.GetSection("LLM").Get<LlmSettings>();
                
                // If no settings, create default settings
                if (llmSettings == null)
                {
                    llmSettings = new LlmSettings
                    {
                        ApiUrl = new Uri("http://localhost:11434"), // Default Ollama URL
                        Model = "llama3"
                    };
                }
                
                return llmSettings;
            });

        // Check if we should use OpenAI, Custom, or Ollama based on the Type setting
        services.AddSingleton<ILlmService>(sp =>
        {
            var settings = sp.GetRequiredService<LlmSettings>();
            var logger = sp.GetRequiredService<ILoggerFactory>();
            
            string apiType = settings.Type.ToLower();
            
            if (apiType.Equals("openai"))
            {
                return new OpenAILlmService(settings, logger.CreateLogger<OpenAILlmService>());
            }
            else if (apiType.Equals("custom"))
            {
                // Use Custom internal network API
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new CustomLlmService(httpClient, settings, logger.CreateLogger<CustomLlmService>());
            }
            else
            {
                // Default to Ollama service
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new OllamaLlmService(httpClient, settings, logger.CreateLogger<OllamaLlmService>());
            }
            
        });

        return services;
    }

    public static IServiceCollection AddMultiModalServices(
        this IServiceCollection services)
    {
        services
            .AddSingleton<MultiModalSettings>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var multiModalSettings = config.GetSection("MultiModal").Get<MultiModalSettings>();

                // If no settings, create default settings
                if (multiModalSettings == null)
                {
                    multiModalSettings = new MultiModalSettings
                    {
                        ApiUrl = new Uri(Environment.GetEnvironmentVariable("MULTIMODAL_API_URL") ?? "http://localhost:1234"),
                        Model = Environment.GetEnvironmentVariable("MULTIMODAL_MODEL") ?? "qwen/qwen3-vl-8b"
                    };
                }
                else
                {
                    // Override with environment variables if present
                    var envApiUrl = Environment.GetEnvironmentVariable("MULTIMODAL_API_URL");
                    if (!string.IsNullOrEmpty(envApiUrl))
                    {
                        multiModalSettings.ApiUrl = new Uri(envApiUrl);
                    }

                    var envModel = Environment.GetEnvironmentVariable("MULTIMODAL_MODEL");
                    if (!string.IsNullOrEmpty(envModel))
                    {
                        multiModalSettings.Model = envModel;
                    }
                }

                return multiModalSettings;
            });

        // Check if we should use OpenAI or Custom based on the Type setting
        services.AddSingleton<IMultiModalService>(sp =>
        {
            var settings = sp.GetRequiredService<MultiModalSettings>();
            var logger = sp.GetRequiredService<ILoggerFactory>();

            string apiType = settings.Type.ToLower();

            if (apiType.Equals("openai"))
            {
                return new MultiModalOpenAIService(settings, logger.CreateLogger<MultiModalOpenAIService>());
            }
            else
            {
                // Default to Custom service (no API key required)
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new MultiModalCustomService(httpClient, settings, logger.CreateLogger<MultiModalCustomService>());
            }
        });

        return services;
    }

    public static IServiceCollection AddAskBotSettings(
        this IServiceCollection services)
    {
        services
            .AddSingleton<AskBotSettings>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var askBotSettings = config.GetSection("AskBot").Get<AskBotSettings>();

                // If no settings, create default settings
                if (askBotSettings == null)
                {
                    askBotSettings = new AskBotSettings
                    {
                        ImageStoragePath = Environment.GetEnvironmentVariable("ASKBOT_IMAGE_STORAGE_PATH") ?? "AskBotImages"
                    };
                }
                else
                {
                    // Override with environment variable if present
                    var envImageStoragePath = Environment.GetEnvironmentVariable("ASKBOT_IMAGE_STORAGE_PATH");
                    if (!string.IsNullOrEmpty(envImageStoragePath))
                    {
                        askBotSettings.ImageStoragePath = envImageStoragePath;
                    }
                }

                return askBotSettings;
            });

        return services;
    }

    public static IServiceCollection AddActorSystem(
        this IServiceCollection services)
    {
        services.AddAkka("Memorizer", (builder, provider) =>
        {
            builder.ConfigureLoggers(logger =>
            {
                logger.ClearLoggers(); // clear the default console logger
                logger.LogLevel = Akka.Event.LogLevel.InfoLevel;
                logger.AddLoggerFactory();
            });

            // Register actors with the ActorRegistry
            builder.WithActors((system, registry, resolver) =>
            {
                // Create and register the TitleGenerationActor
                var titleGenerationActorProps = resolver.Props<TitleGenerationActor>();
                var titleGenerationActor = system.ActorOf(titleGenerationActorProps, "title-generation");
                registry.Register<TitleGenerationActorKey>(titleGenerationActor);

                // Create and register the MetadataEmbeddingActor
                var metadataEmbeddingActorProps = resolver.Props<MetadataEmbeddingActor>();
                var metadataEmbeddingActor = system.ActorOf(metadataEmbeddingActorProps, "metadata-embedding");
                registry.Register<MetadataEmbeddingActorKey>(metadataEmbeddingActor);

                // Create and register the GraphSyncActor
                var graphSyncActorProps = resolver.Props<GraphSyncActor>();
                var graphSyncActor = system.ActorOf(graphSyncActorProps, "graph-sync");
                registry.Register<GraphSyncActorKey>(graphSyncActor);

                // Create and register the SearchMemoryActor for ASKBOT
                var searchMemoryActorProps = resolver.Props<SearchMemoryActor>();
                var searchMemoryActor = system.ActorOf(searchMemoryActorProps, "search-memory");
                registry.Register<SearchMemoryActorKey>(searchMemoryActor);

                // Create and register the DecisionActor for ASKBOT
                var decisionActorProps = resolver.Props<DecisionActor>();
                var decisionActor = system.ActorOf(decisionActorProps, "decision");
                registry.Register<DecisionActorKey>(decisionActor);
            });

            // TODO: Configure Akka.Persistence.Sql with PostgreSQL
            // This will be added once we have the proper configuration for tool performance tracking
        });

        return services;
    }

    public static IServiceCollection AddStorage(
        this IServiceCollection services)
    {
        services
            .AddSingleton(sp =>
            {
                string connectionString =
                    sp.GetRequiredService<IConfiguration>().GetConnectionString("Storage") ??
                    throw new ArgumentNullException("Storage Connection String");
                NpgsqlDataSourceBuilder sourceBuilder = new(connectionString);
                sourceBuilder.UseVector();
                return sourceBuilder.Build();
            });
        return services;
    }

    public static IServiceCollection AddServerSettings(
        this IServiceCollection services)
    {
        services.AddSingleton<ServerSettings>(sp =>
            sp.GetRequiredService<IConfiguration>().GetSection("Server").Get<ServerSettings>() ??
            new ServerSettings());

        return services;
    }

    public static IServiceCollection AddOAuthServices(
        this IServiceCollection services)
    {
        services.AddSingleton<OAuthSettings>(sp =>
            sp.GetRequiredService<IConfiguration>().GetSection("OAuth").Get<OAuthSettings>() ??
            new OAuthSettings());

        services.AddSingleton<IOAuthTokenService, OAuthTokenService>();

        return services;
    }
    
    public static IServiceCollection AddGraphServices(
        this IServiceCollection services)
    {
        // Configure Neo4j settings
        services.Configure<Neo4jSettings>(options =>
        {
            var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            config.GetSection(Neo4jSettings.SectionName).Bind(options);
        });
        
        // Add Neo4j driver factory as singleton
        services.AddSingleton<INeo4jDriverFactory, Neo4jDriverFactory>();
        
        // Add graph repository - Changed to Singleton for Actor compatibility
        services.AddSingleton<IGraphRepository, GraphRepository>();
        
        // Add graph sync service - Changed to Singleton for Actor compatibility
        services.AddSingleton<IGraphSyncService, GraphSyncService>();
        
        // Add graph search service - Changed to Singleton for Actor compatibility
        services.AddSingleton<IGraphSearchService, GraphSearchService>();
        
        // Register the GraphRelationshipActor with Akka
        services.AddSingleton(sp =>
        {
            var actorSystem = sp.GetRequiredService<ActorSystem>();
            var graphSyncService = sp.GetRequiredService<IGraphSyncService>();
            var llmService = sp.GetRequiredService<ILlmService>();
            var logger = sp.GetRequiredService<ILogger<GraphRelationshipActor>>();
            
            var props = GraphRelationshipActor.Props(graphSyncService, llmService, logger);
            return actorSystem.ActorOf(props, "graph-relationship-actor");
        });
        
        return services;
    }

    public static IServiceCollection AddHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Storage") ??
                                  throw new ArgumentNullException("Storage Connection String");

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

        return services;
    }
}