using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using System.Diagnostics;
using System.Web;

namespace Jarvis.Tools.Browser;

public class WebSearchTool : ITool
{
    public string Name => "web_search";
    public string Description => "Opens a web search in the default browser using Google.";

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters = new()
        {
            new ToolParameter("query", "The search query to look up", "string", true)
        }
    };

    public Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = parameters.TryGetValue("query", out var q) ? q?.ToString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult(Result.Failure<ToolResult>("Search query is required."));

            var encoded = HttpUtility.UrlEncode(query);
            var url = $"https://www.google.com/search?q={encoded}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            return Task.FromResult(Result.Success(ToolResult.Ok($"Searching for: {query}")));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<ToolResult>($"Failed to open search: {ex.Message}"));
        }
    }
}
