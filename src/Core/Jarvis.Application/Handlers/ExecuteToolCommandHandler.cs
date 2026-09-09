using Jarvis.Application.Commands;
using Jarvis.Application.Services;
using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Handlers;

public class ExecuteToolCommandHandler : IRequestHandler<ExecuteToolCommand, Result<ToolResult>>
{
    private readonly ToolDispatcher _toolDispatcher;

    public ExecuteToolCommandHandler(ToolDispatcher toolDispatcher)
    {
        _toolDispatcher = toolDispatcher;
    }

    public async Task<Result<ToolResult>> Handle(ExecuteToolCommand request, CancellationToken cancellationToken)
    {
        return await _toolDispatcher.DispatchAsync(request.ToolName, request.Parameters, cancellationToken);
    }
}