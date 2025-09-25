using Memorizer.Models;

namespace Memorizer.Services;

public interface ICustomScriptService
{
    Task<string> GetActiveScriptsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomScript>> GetAllScriptsAsync(CancellationToken cancellationToken = default);
    Task<CustomScript?> GetScriptByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<CustomScript> CreateOrUpdateScriptAsync(string name, string scriptContent, bool isActive, CancellationToken cancellationToken = default);
    Task<bool> DeleteScriptAsync(string name, CancellationToken cancellationToken = default);
}