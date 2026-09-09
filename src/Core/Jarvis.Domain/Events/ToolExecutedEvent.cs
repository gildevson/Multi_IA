using Jarvis.Domain.Entities;

namespace Jarvis.Domain.Events;

public record ToolExecutedEvent(
    Guid ConversationId,
    ToolExecution Execution,
    DateTime OccurredAt)
{
    public static ToolExecutedEvent Create(Guid conversationId, ToolExecution execution)
        => new(conversationId, execution, DateTime.UtcNow);
}