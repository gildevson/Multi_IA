namespace Jarvis.Application.Tools;

public record ToolResult(
    bool Success,
    string? Output = null,
    string? Error = null,
    Dictionary<string, object>? Data = null)
{
    public static ToolResult Ok(string output) => new(true, Output: output);
    public static ToolResult Ok(string output, Dictionary<string, object> data) => new(true, Output: output, Data: data);
    public static ToolResult Fail(string error) => new(false, Error: error);
}