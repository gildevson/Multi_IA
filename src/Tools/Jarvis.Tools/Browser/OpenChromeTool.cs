using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using System.Diagnostics;

namespace Jarvis.Tools.Browser;

public class OpenChromeTool : ITool
{
    public string Name => "open_chrome";
    public string Description => "Opens the Chrome browser, optionally navigating to a URL.";

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters = new()
        {
            new ToolParameter("url", "The URL to open in Chrome (optional)", "string", false)
        }
    };

    public Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = parameters.TryGetValue("url", out var u) ? u?.ToString() : null;
            var args = string.IsNullOrEmpty(url) ? string.Empty : url;

            var psi = new ProcessStartInfo("chrome.exe", args) { UseShellExecute = true };
            Process.Start(psi);

            return Task.FromResult(Result.Success(ToolResult.Ok(
                string.IsNullOrEmpty(url) ? "Chrome opened." : $"Chrome opened with URL: {url}")));
        }
        catch (Exception ex)
        {
            // Fallback: use default browser
            try
            {
                var url = parameters.TryGetValue("url", out var u) ? u?.ToString() : null;
                if (!string.IsNullOrEmpty(url))
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return Task.FromResult(Result.Success(ToolResult.Ok("Browser opened.")));
            }
            catch
            {
                return Task.FromResult(Result.Failure<ToolResult>($"Failed to open browser: {ex.Message}"));
            }
        }
    }
}
