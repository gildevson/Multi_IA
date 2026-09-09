using Jarvis.Domain.Entities;

namespace Jarvis.Domain.Interfaces.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Conversation>> GetHistoryAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task ClearCurrentAsync(CancellationToken cancellationToken = default);
}