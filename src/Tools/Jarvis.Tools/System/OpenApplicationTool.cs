using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using System.Diagnostics;

namespace Jarvis.Tools.System;

public class OpenApplicationTool : ITool
{
    public string Name => "open_application";
    public string Description => "Opens an application on Windows by its name or executable path.";

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters = new()
        {
            new ToolParameter("application_name", "The name or path of the application to open (e.g., 'notepad', 'calc', 'chrome')", "string", true)
        }
    };

    public Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var appName = parameters.TryGetValue("application_name", out var v) ? v?.ToString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(appName))
                return Task.FromResult(Result.Failure<ToolResult>("Application name is required."));

            Process.Start(new ProcessStartInfo(appName) { UseShellExecute = true });
            return Task.FromResult(Result.Success(ToolResult.Ok($"Application '{appName}' opened successfully.")));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<ToolResult>($"Failed to open application: {ex.Message}"));
        }
    }
}
