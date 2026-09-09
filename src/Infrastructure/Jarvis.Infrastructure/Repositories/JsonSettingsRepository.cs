using Jarvis.Domain.Interfaces.Repositories;
using Jarvis.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Jarvis.Infrastructure.Repositories;

public class JsonSettingsRepository : ISettingsRepository
{
    private readonly string _settingsFilePath;
    private readonly ILogger<JsonSettingsRepository> _logger;
    private Dictionary<string, JsonElement> _data = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonSettingsRepository(IOptions<JarvisSettings> settings, ILogger<JsonSettingsRepository> logger)
    {
        var dataFolder = settings.Value.DataFolder;
        Directory.CreateDirectory(dataFolder);
        _settingsFilePath = Path.Combine(dataFolder, "settings.json");
        _logger = logger;
        LoadData();
    }

    private void LoadData()
    {
        if (!File.Exists(_settingsFilePath)) return;
        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            _data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
        }
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_data.TryGetValue(key, out var element))
            return Task.FromResult(default(T));

        try
        {
            var value = element.Deserialize<T>(JsonOptions);
            return Task.FromResult(value);
        }
        catch
        {
            return Task.FromResult(default(T));
        }
    }

    public async Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.SerializeToElement(value, JsonOptions);
        _data[key] = json;
        var fullJson = JsonSerializer.Serialize(_data, JsonOptions);
        await File.WriteAllTextAsync(_settingsFilePath, fullJson, cancellationToken);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_data.ContainsKey(key));
}