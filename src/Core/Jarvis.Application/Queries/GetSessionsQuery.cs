using Jarvis.Application.DTOs;
using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Queries;

public record GetSessionsQuery(int Limit = 20) : IRequest<Result<List<ConversationSummaryDto>>>;
