using Jarvis.Desktop.Services;
using Jarvis.Desktop.ViewModels;
using Jarvis.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Desktop.DependencyInjection;

public static class DesktopServiceExtensions
{
    public static IServiceCollection AddDesktopServices(this IServiceCollection services)
    {
        services.AddSingleton<NavigationService>();

        services.AddTransient<ChatViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ToolsViewModel>();
        services.AddTransient<LogsViewModel>();
        services.AddTransient<MainViewModel>();

        services.AddTransient<MainWindow>();

        return services;
    }
}
