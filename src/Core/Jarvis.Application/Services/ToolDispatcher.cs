using Jarvis.Application.Abstractions;
using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Jarvis.Application.Services;

public class ToolDispatcher
{
    private readonly IToolRegistry _registry;
    private readonly ILogger<ToolDispatcher> _logger;

    public ToolDispatcher(IToolRegistry registry, ILogger<ToolDispatcher> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task<Result<ToolResult>> DispatchAsync(
        string toolName,
        IDictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var tool = _registry.GetByName(toolName);

        if (tool is null)
        {
            _logger.LogWarning("Tool not found: {ToolName}", toolName);
            return Result.Failure<ToolResult>($"Tool ''{toolName}'' not found in registry.");
        }

        _logger.LogInformation("Executing tool: {ToolName} with {ParamCount} parameters",
            toolName, parameters.Count);

        try
        {
            var result = await tool.ExecuteAsync(parameters, cancellationToken);
            if (result.IsSuccess)
                _logger.LogInformation("Tool {ToolName} executed successfully", toolName);
            else
                _logger.LogWarning("Tool {ToolName} failed: {Error}", toolName, result.Error);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool {ToolName} threw an exception", toolName);
            return Result.Failure<ToolResult>($"Tool ''{toolName}'' threw an exception: {ex.Message}");
        }
    }
}