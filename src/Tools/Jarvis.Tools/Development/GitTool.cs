using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using System.Diagnostics;
using System.Text;

namespace Jarvis.Tools.Development;

public class GitTool : ITool
{
    public string Name => "git_command";
    public string Description => "Executes a git command in a specified directory and returns the output.";

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters = new()
        {
            new ToolParameter("command", "The git subcommand to run (e.g., 'status', 'log --oneline -10', 'diff')", "string", true),
            new ToolParameter("working_directory", "The directory where the git command should run (defaults to current directory)", "string", false)
        }
    };

    public async Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = parameters.TryGetValue("command", out var c) ? c?.ToString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(command))
                return Result.Failure<ToolResult>("Git command is required.");

            var workDir = parameters.TryGetValue("working_directory", out var wd) ? wd?.ToString() : null;
            if (!string.IsNullOrEmpty(workDir) && !Directory.Exists(workDir))
                return Result.Failure<ToolResult>($"Directory not found: {workDir}");

            var psi = new ProcessStartInfo("git")
            {
                Arguments = command,
                WorkingDirectory = workDir ?? Directory.GetCurrentDirectory(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken);

            var output = stdout.ToString().Trim();
            var error = stderr.ToString().Trim();

            if (process.ExitCode != 0)
                return Result.Success(ToolResult.Fail($"git {command} failed (exit {process.ExitCode}): {error}"));

            return Result.Success(ToolResult.Ok(string.IsNullOrEmpty(output) ? "Git command executed." : output));
        }
        catch (Exception ex)
        {
            return Result.Failure<ToolResult>($"Git command failed: {ex.Message}");
        }
    }
}
