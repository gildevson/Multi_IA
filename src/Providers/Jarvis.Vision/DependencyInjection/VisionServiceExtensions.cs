using Jarvis.Application.Abstractions;
using Jarvis.Vision.Analysis;
using Jarvis.Vision.Capture;
using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Vision.DependencyInjection;

public static class VisionServiceExtensions
{
    public static IServiceCollection AddVisionProviders(this IServiceCollection services)
    {
        services.AddSingleton<IScreenCaptureProvider, ScreenCaptureProvider>();
        services.AddSingleton<IImageAnalysisProvider, MultimodalImageAnalysisProvider>();
        return services;
    }
}
