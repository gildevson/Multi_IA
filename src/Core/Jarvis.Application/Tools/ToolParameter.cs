namespace Jarvis.Application.Tools;

public record ToolParameter(
    string Name,
    string Description,
    string Type,
    bool Required,
    object? DefaultValue = null);