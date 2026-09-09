namespace Jarvis.Application.DTOs;

public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public bool HasToolCall { get; set; }
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
    public Dictionary<string, object>? ToolArguments { get; set; }
    public bool IsStreaming { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }

    public static AIResponse FromText(string content) => new() { Content = content };

    public static AIResponse FromToolCall(string toolCallId, string toolName, Dictionary<string, object> args) =>
        new()
        {
            HasToolCall = true,
            ToolCallId = toolCallId,
            ToolName = toolName,
            ToolArguments = args
        };
}