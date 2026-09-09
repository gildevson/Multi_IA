using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using System.Diagnostics;
using System.Text;

namespace Jarvis.Tools.System;

public class ExecutePowerShellTool : ITool
{
    public string Name => "execute_powershell";
    public string Description => "Executes a PowerShell command and returns the output.";

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters = new()
        {
            new ToolParameter("command", "The PowerShell command to execute", "string", true),
            new ToolParameter("timeout_seconds", "Maximum execution time in seconds (default: 30)", "number", false, 30)
        }
    };

    public async Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = parameters.TryGetValue("command", out var c) ? c?.ToString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(command))
                return Result.Failure<ToolResult>("Command is required.");

            var timeout = 30;
            if (parameters.TryGetValue("timeout_seconds", out var t) && t is not null)
                int.TryParse(t.ToString(), out timeout);

            var psi = new ProcessStartInfo("powershell.exe")
            {
                Arguments = $"-NoProfile -NonInteractive -Command \"{command.Replace("\"", "\\\"")}\"",
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

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeout));

            await process.WaitForExitAsync(cts.Token);

            var output = stdout.ToString().Trim();
            var error = stderr.ToString().Trim();

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                return Result.Success(ToolResult.Fail($"Exit code {process.ExitCode}: {error}"));

            return Result.Success(ToolResult.Ok(string.IsNullOrEmpty(output) ? "Command executed with no output." : output));
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<ToolResult>("Command timed out.");
        }
        catch (Exception ex)
        {
            return Result.Failure<ToolResult>($"PowerShell execution failed: {ex.Message}");
        }
    }
}
