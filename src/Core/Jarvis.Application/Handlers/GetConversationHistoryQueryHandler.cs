using Jarvis.Application.DTOs;
using Jarvis.Application.Queries;
using Jarvis.Application.Services;
using Jarvis.Domain.Enums;
using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Handlers;

public class GetConversationHistoryQueryHandler : IRequestHandler<GetConversationHistoryQuery, Result<ConversationDto>>
{
    private readonly ConversationService _conversationService;

    public GetConversationHistoryQueryHandler(ConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    public async Task<Result<ConversationDto>> Handle(GetConversationHistoryQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetOrCreateCurrentAsync(cancellationToken);
        var messages = conversation.Messages
            .TakeLast(request.Limit)
            .Select(m => new MessageDto(m.Id, m.Role.ToString(), m.Content.Text, m.CreatedAt, m.ToolName))
            .ToList();

        var dto = new ConversationDto(conversation.Id, messages, conversation.Status.ToString(), conversation.StartedAt, conversation.Title);
        return Result.Success(dto);
    }
}