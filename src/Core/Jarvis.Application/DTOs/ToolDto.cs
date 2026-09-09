using Jarvis.Application.Tools;

namespace Jarvis.Application.DTOs;

public record ToolDto(
    string Name,
    string Description,
    List<ToolParameter> Parameters);