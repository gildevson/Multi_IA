using Jarvis.Shared.Models;

namespace Jarvis.Application.Tools;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    ToolDefinition GetDefinition();
    Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default);
}