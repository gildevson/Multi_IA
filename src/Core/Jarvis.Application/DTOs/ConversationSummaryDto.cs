namespace Jarvis.Application.DTOs;

public record ConversationSummaryDto(
    Guid Id,
    string Title,
    DateTime StartedAt,
    int MessageCount);
