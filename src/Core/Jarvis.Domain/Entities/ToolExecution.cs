using Jarvis.Domain.Enums;

namespace Jarvis.Domain.Entities;

public class ToolExecution
{
    public Guid Id { get; private set; }
    public string ToolName { get; private set; }
    public Dictionary<string, object> Parameters { get; private set; }
    public string? Result { get; private set; }
    public string? Error { get; private set; }
    public ToolExecutionStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public ToolExecution(string toolName, Dictionary<string, object> parameters)
    {
        Id = Guid.NewGuid();
        ToolName = toolName;
        Parameters = parameters;
        Status = ToolExecutionStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(string result)
    {
        Result = result;
        Status = ToolExecutionStatus.Succeeded;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string error)
    {
        Error = error;
        Status = ToolExecutionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }

    public TimeSpan? Duration => CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt
        : null;
}