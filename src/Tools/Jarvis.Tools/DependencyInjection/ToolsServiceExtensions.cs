using Jarvis.Application.Abstractions;
using Jarvis.Application.Tools;
using Jarvis.Tools.Browser;
using Jarvis.Tools.Development;
using Jarvis.Tools.Registry;
using Jarvis.Tools.System;
using Jarvis.Tools.Vision;
using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Tools.DependencyInjection;

public static class ToolsServiceExtensions
{
    public static IServiceCollection AddTools(this IServiceCollection services)
    {
        services.AddHttpClient("WebSearch", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Jarvis-App/1.0");
        });

        services.AddSingleton<IToolRegistry, ToolRegistry>();

        // System tools
        services.AddSingleton<ITool, OpenApplicationTool>();
        services.AddSingleton<ITool, ReadFileTool>();
        services.AddSingleton<ITool, WriteFileTool>();
        services.AddSingleton<ITool, ExecutePowerShellTool>();

        // Browser tools
        services.AddSingleton<ITool, OpenChromeTool>();
        services.AddSingleton<ITool, WebSearchTool>();
        services.AddSingleton<WebFetchSearchTool>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var searchSettings = sp.GetRequiredService<Jarvis.Shared.Configuration.SearchSettings>();
            return new WebFetchSearchTool(factory, searchSettings);
        });
        services.AddSingleton<ITool>(sp => sp.GetRequiredService<WebFetchSearchTool>());

        // Development tools
        services.AddSingleton<ITool, OpenVisualStudioTool>();
        services.AddSingleton<ITool, GitTool>();

        // Vision tools
        services.AddSingleton<ITool, CaptureScreenTool>();

        // Populate registry after all tools are registered
        services.AddSingleton<ToolRegistryInitializer>();

        return services;
    }
}

public class ToolRegistryInitializer
{
    public ToolRegistryInitializer(IToolRegistry registry, IEnumerable<ITool> tools)
    {
        foreach (var tool in tools)
        {
            if (!registry.Exists(tool.Name))
                registry.Register(tool);
        }
    }
}
