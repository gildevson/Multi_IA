using Jarvis.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Jarvis.AI.Factory;

public class AIProviderFactory
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly ILogger<AIProviderFactory> _logger;

    public AIProviderFactory(IEnumerable<IAIProvider> providers, ILogger<AIProviderFactory> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public IAIProvider GetProvider(string name)
    {
        var provider = _providers.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (provider is null)
        {
            _logger.LogWarning("AI provider '{Name}' not found. Falling back to first available.", name);
            provider = _providers.FirstOrDefault()
                ?? throw new InvalidOperationException("No AI providers registered.");
        }

        return provider;
    }

    public IEnumerable<string> GetAvailableProviderNames()
        => _providers.Select(p => p.Name);
}
