using Jarvis.Domain.Entities;

namespace Jarvis.Domain.Events;

public record MessageReceivedEvent(
    Guid ConversationId,
    Message Message,
    DateTime OccurredAt)
{
    public static MessageReceivedEvent Create(Guid conversationId, Message message)
        => new(conversationId, message, DateTime.UtcNow);
}