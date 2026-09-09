namespace Jarvis.Domain.Exceptions;

public class ToolExecutionException : DomainException
{
    public string ToolName { get; }

    public ToolExecutionException(string toolName, string message)
        : base($"Tool '{toolName}' failed: {message}")
    {
        ToolName = toolName;
    }

    public ToolExecutionException(string toolName, string message, Exception inner)
        : base($"Tool '{toolName}' failed: {message}", inner)
    {
        ToolName = toolName;
    }
}