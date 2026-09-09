namespace Jarvis.Domain.Events;

public record ConversationStartedEvent(
    Guid ConversationId,
    DateTime StartedAt)
{
    public static ConversationStartedEvent Create(Guid conversationId)
        => new(conversationId, DateTime.UtcNow);
}