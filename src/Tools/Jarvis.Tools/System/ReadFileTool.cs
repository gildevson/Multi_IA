using Jarvis.Application.Tools;
using Jarvis.Shared.Models;

namespace Jarvis.Tools.System;

public class ReadFileTool : ITool
{
    public string Name => "read_file";
    public string Description => "Reads the content of a file from the filesystem.";

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters = new()
        {
            new ToolParameter("path", "The full path to the file to read", "string", true),
            new ToolParameter("max_lines", "Maximum number of lines to read (default: 100)", "number", false, 100)
        }
    };

    public async Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = parameters.TryGetValue("path", out var p) ? p?.ToString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                return Result.Failure<ToolResult>("File path is required.");

            if (!File.Exists(path))
                return Result.Failure<ToolResult>($"File not found: {path}");

            var maxLines = 100;
            if (parameters.TryGetValue("max_lines", out var ml) && ml is not null)
                int.TryParse(ml.ToString(), out maxLines);

            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
            var content = string.Join(Environment.NewLine, lines.Take(maxLines));

            if (lines.Length > maxLines)
                content += $"\n\n[Truncated: showing {maxLines} of {lines.Length} lines]";

            return Result.Success(ToolResult.Ok(content));
        }
        catch (Exception ex)
        {
            return Result.Failure<ToolResult>($"Failed to read file: {ex.Message}");
        }
    }
}
