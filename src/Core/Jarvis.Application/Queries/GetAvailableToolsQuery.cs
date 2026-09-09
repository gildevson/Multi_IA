using Jarvis.Application.DTOs;
using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Queries;

public record GetAvailableToolsQuery : IRequest<Result<IEnumerable<ToolDto>>>;