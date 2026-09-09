using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Desktop.Services;

public class NavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TViewModel GetView<TViewModel>() where TViewModel : class
        => _serviceProvider.GetRequiredService<TViewModel>();
}
