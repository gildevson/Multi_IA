namespace Jarvis.Application.DTOs;

public record ConversationDto(
    Guid Id,
    List<MessageDto> Messages,
    string Status,
    DateTime StartedAt,
    string? Title = null);