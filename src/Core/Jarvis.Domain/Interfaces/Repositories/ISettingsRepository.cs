namespace Jarvis.Domain.Interfaces.Repositories;

public interface ISettingsRepository
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}