using Jarvis.Application.Abstractions;
using Jarvis.Application.DTOs;
using Jarvis.Application.Queries;
using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Handlers;

public class GetAvailableToolsQueryHandler : IRequestHandler<GetAvailableToolsQuery, Result<IEnumerable<ToolDto>>>
{
    private readonly IToolRegistry _toolRegistry;

    public GetAvailableToolsQueryHandler(IToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    public Task<Result<IEnumerable<ToolDto>>> Handle(GetAvailableToolsQuery request, CancellationToken cancellationToken)
    {
        var tools = _toolRegistry.GetAll()
            .Select(t => new ToolDto(t.Name, t.Description, t.GetDefinition().Parameters))
            .AsEnumerable();

        return Task.FromResult(Result.Success(tools));
    }
}