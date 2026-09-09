using Jarvis.Application.Tools;
using Jarvis.Shared.Models;

namespace Jarvis.Tools.System;

public class WriteFileTool : ITool
{
    public string Name => "write_file";
    public string Description => "Writes content to a file on the filesystem.";

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters = new()
        {
            new ToolParameter("path", "The full path to the file to write", "string", true),
            new ToolParameter("content", "The content to write to the file", "string", true),
            new ToolParameter("append", "Whether to append to the file instead of overwriting (default: false)", "boolean", false, false)
        }
    };

    public async Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = parameters.TryGetValue("path", out var p) ? p?.ToString() ?? string.Empty : string.Empty;
            var content = parameters.TryGetValue("content", out var c) ? c?.ToString() ?? string.Empty : string.Empty;

            if (string.IsNullOrWhiteSpace(path))
                return Result.Failure<ToolResult>("File path is required.");

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var append = parameters.TryGetValue("append", out var a) && a?.ToString()?.ToLower() == "true";

            if (append)
                await File.AppendAllTextAsync(path, content, cancellationToken);
            else
                await File.WriteAllTextAsync(path, content, cancellationToken);

            return Result.Success(ToolResult.Ok($"File written successfully: {path}"));
        }
        catch (Exception ex)
        {
            return Result.Failure<ToolResult>($"Failed to write file: {ex.Message}");
        }
    }
}
