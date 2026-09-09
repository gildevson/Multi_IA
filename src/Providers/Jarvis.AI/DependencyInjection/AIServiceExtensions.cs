using Jarvis.AI.Factory;
using Jarvis.AI.Providers.Claude;
using Jarvis.AI.Providers.Gemini;
using Jarvis.AI.Providers.Ollama;
using Jarvis.AI.Providers.OpenAI;
using Jarvis.Application.Abstractions;
using Jarvis.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jarvis.AI.DependencyInjection;

public static class AIServiceExtensions
{
    public static IServiceCollection AddAIProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("Claude");
        services.AddHttpClient("Ollama");
        services.AddHttpClient("Gemini");

        // Register all providers so AIProviderFactory can resolve them
        services.AddSingleton<OpenAIProvider>();
        services.AddSingleton<ClaudeProvider>();
        services.AddSingleton<OllamaProvider>();
        services.AddSingleton<GeminiProvider>();

        services.AddSingleton<AIProviderFactory>(sp => new AIProviderFactory(
            new IAIProvider[]
            {
                sp.GetRequiredService<OpenAIProvider>(),
                sp.GetRequiredService<ClaudeProvider>(),
                sp.GetRequiredService<OllamaProvider>(),
                sp.GetRequiredService<GeminiProvider>()
            },
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AIProviderFactory>>()));

        // Register the active provider based on configuration
        services.AddSingleton<IAIProvider>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<JarvisSettings>>().Value;
            var factory = sp.GetRequiredService<AIProviderFactory>();
            return factory.GetProvider(settings.AIProvider);
        });

        // Register vision provider — usa VisionProvider se configurado, senão cai no principal
        services.AddSingleton<IVisionAIProvider>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<JarvisSettings>>().Value;
            var factory = sp.GetRequiredService<AIProviderFactory>();
            var providerName = !string.IsNullOrEmpty(settings.AI.VisionProvider)
                ? settings.AI.VisionProvider
                : settings.AIProvider;
            var provider = factory.GetProvider(providerName);
            return new VisionAIProviderAdapter(provider);
        });

        return services;
    }
}
