using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using System.Diagnostics;

namespace Jarvis.Tools.Development;

public class OpenVisualStudioTool : ITool
{
    public string Name => "open_visual_studio";
    public string Description => "Opens Visual Studio, optionally opening a specific solution file.";

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters = new()
        {
            new ToolParameter("solution_path", "Optional path to a .sln file to open", "string", false)
        }
    };

    public Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var solutionPath = parameters.TryGetValue("solution_path", out var s) ? s?.ToString() : null;

            ProcessStartInfo psi;
            if (!string.IsNullOrEmpty(solutionPath) && File.Exists(solutionPath))
            {
                psi = new ProcessStartInfo(solutionPath) { UseShellExecute = true };
            }
            else
            {
                psi = new ProcessStartInfo("devenv.exe") { UseShellExecute = true };
            }

            Process.Start(psi);
            return Task.FromResult(Result.Success(ToolResult.Ok(
                string.IsNullOrEmpty(solutionPath)
                    ? "Visual Studio opened."
                    : $"Visual Studio opened with: {solutionPath}")));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<ToolResult>($"Failed to open Visual Studio: {ex.Message}"));
        }
    }
}
