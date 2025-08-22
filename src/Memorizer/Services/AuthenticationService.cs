using Memorizer.Settings;
using Microsoft.Extensions.Configuration;

namespace Memorizer.Services;

public interface IAuthenticationService
{
    bool ValidateCredentials(string username, string password);
    bool ValidateApiKey(string apiKey);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ServerSettings _serverSettings;

    public AuthenticationService(IConfiguration configuration, ILogger<AuthenticationService> logger, ServerSettings serverSettings)
    {
        _configuration = configuration;
        _serverSettings = serverSettings;
        _logger = logger;
    }

    public bool ValidateCredentials(string username, string password)
    {
        var configuredUsername = _serverSettings.UserName ?? "admin";

        var configuredPassword = _serverSettings.Password ?? "admin123";

        _logger.LogDebug("Validating credentials for user: {Username}", username);

        return username == configuredUsername && password == configuredPassword;
    }

    public bool ValidateApiKey(string apiKey)
    {
        var configuredApiKey = _serverSettings.ApiKey ?? "default-api-key";
        return apiKey == configuredApiKey;
    }
}