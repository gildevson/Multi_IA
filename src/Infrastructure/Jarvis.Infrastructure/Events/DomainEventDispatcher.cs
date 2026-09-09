using Jarvis.Domain.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jarvis.Infrastructure.Events;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(object domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        _logger.LogDebug("Dispatching domain event: {EventType}", eventType.Name);

        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null) continue;
            try
            {
                var method = handlerType.GetMethod("HandleAsync");
                if (method is not null)
                {
                    var task = (Task?)method.Invoke(handler, new[] { domainEvent, cancellationToken });
                    if (task is not null)
                        await task;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in domain event handler for {EventType}", eventType.Name);
            }
        }
    }
}

public interface IDomainEventHandler<in TEvent>
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}