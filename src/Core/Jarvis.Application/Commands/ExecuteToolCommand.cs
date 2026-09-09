using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Commands;

public record ExecuteToolCommand(
    string ToolName,
    Dictionary<string, object> Parameters) : IRequest<Result<ToolResult>>;