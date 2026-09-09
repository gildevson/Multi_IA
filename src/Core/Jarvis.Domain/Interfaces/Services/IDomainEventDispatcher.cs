namespace Jarvis.Domain.Interfaces.Services;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(object domainEvent, CancellationToken cancellationToken = default);
}