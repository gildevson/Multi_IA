using Jarvis.Domain.Interfaces.Repositories;
using Jarvis.Domain.Interfaces.Services;
using Jarvis.Infrastructure.Configuration;
using Jarvis.Infrastructure.Events;
using Jarvis.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JarvisSettings>(configuration.GetSection(JarvisSettings.SectionName));

        services.AddSingleton<IConversationRepository, JsonConversationRepository>();
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}