using Akka.Hosting;
using Memorizer.Actors;
using Memorizer.Services;
using Memorizer.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Registrator.Net;

namespace Memorizer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMemorizer(
        this IServiceCollection services, bool initialize = true)
    {
        services.AddHttpClient(); // Add HttpClientFactory for service implementations
        services.AddEmbeddings();
        services.AddLlmServices();
        services.AddActorSystem();
        services.AddStorage();
        services.AddServerSettings();
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

        // Check if we should use OpenAI or Ollama based on the API URL
        services.AddSingleton<IEmbeddingService>(sp =>
        {
            var settings = sp.GetRequiredService<EmbeddingSettings>();
            var logger = sp.GetRequiredService<ILoggerFactory>();
            
            // If the API URL contains "api.openai.com", use OpenAI service
            if (settings.ApiUrl.ToString().Contains("api.openai.com", StringComparison.OrdinalIgnoreCase))
            {
                return new OpenAIEmbeddingService(settings, logger.CreateLogger<OpenAIEmbeddingService>());
            }
            else
            {
                // Otherwise, use Ollama service
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new EmbeddingService(httpClient, settings, logger.CreateLogger<EmbeddingService>());
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

        // Check if we should use OpenAI or Ollama based on the API URL
        services.AddSingleton<ILlmService>(sp =>
        {
            var settings = sp.GetRequiredService<LlmSettings>();
            var logger = sp.GetRequiredService<ILoggerFactory>();
            
            // If the API URL contains "api.openai.com", use OpenAI service
            if (settings.ApiUrl.ToString().Contains("api.openai.com", StringComparison.OrdinalIgnoreCase))
            {
                return new OpenAILlmService(settings, logger.CreateLogger<OpenAILlmService>());
            }
            else
            {
                // Otherwise, use Ollama service
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = settings.ApiUrl;
                httpClient.Timeout = settings.Timeout;
                return new LlmService(httpClient, settings, logger.CreateLogger<LlmService>());
            }
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