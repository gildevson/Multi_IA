using Jarvis.AI.DependencyInjection;
using Jarvis.Application.Behaviors;
using Jarvis.Application.Services;
using Jarvis.Desktop.DependencyInjection;
using Jarvis.Desktop.Views;
using Jarvis.Infrastructure.DependencyInjection;
using Jarvis.Infrastructure.Logging;
using Jarvis.Tools.DependencyInjection;
using Jarvis.Vision.DependencyInjection;
using Jarvis.Voice.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;

namespace Jarvis.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables("JARVIS_");
            })
            .UseSerilog((ctx, _, logConfig) =>
            {
                SerilogConfiguration.Configure(logConfig, ctx.Configuration);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(typeof(AssistantOrchestrator).Assembly);
                    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                });

                services.AddSingleton<ConversationService>();
                services.AddSingleton<ToolDispatcher>();
                services.AddSingleton<AssistantOrchestrator>();

                services.AddInfrastructure(ctx.Configuration);
                services.AddAIProviders(ctx.Configuration);
                services.AddVoiceProviders(ctx.Configuration);
                services.AddVisionProviders();
                // Registra SearchSettings como singleton para uso em Jarvis.Tools
                services.AddSingleton(sp =>
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Jarvis.Infrastructure.Configuration.JarvisSettings>>().Value.Search);
                services.AddTools();
                services.AddDesktopServices();
            })
            .Build();

        // Initialize tool registry
        _host.Services.GetRequiredService<ToolRegistryInitializer>();

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
