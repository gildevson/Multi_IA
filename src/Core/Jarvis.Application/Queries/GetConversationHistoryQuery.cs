using Jarvis.Application.DTOs;
using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Queries;

public record GetConversationHistoryQuery(int Limit = 50) : IRequest<Result<ConversationDto>>;