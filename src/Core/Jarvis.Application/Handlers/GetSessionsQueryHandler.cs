using Jarvis.Application.DTOs;
using Jarvis.Application.Queries;
using Jarvis.Domain.Interfaces.Repositories;
using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Handlers;

public class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, Result<List<ConversationSummaryDto>>>
{
    private readonly IConversationRepository _repository;

    public GetSessionsQueryHandler(IConversationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<ConversationSummaryDto>>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _repository.GetHistoryAsync(request.Limit, cancellationToken);
        var summaries = conversations
            .Where(c => c.Messages.Count > 0)
            .Select(c => new ConversationSummaryDto(
                c.Id,
                c.Title ?? $"Conversa {c.StartedAt.ToLocalTime():dd/MM HH:mm}",
                c.StartedAt,
                c.Messages.Count))
            .ToList();
        return Result.Success(summaries);
    }
}
