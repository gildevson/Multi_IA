namespace Jarvis.Application.DTOs;

public record MessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt,
    string? ToolName = null,
    string? ImageBase64 = null,
    string? ImageMimeType = null);