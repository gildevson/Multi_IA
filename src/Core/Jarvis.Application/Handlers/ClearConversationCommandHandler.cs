using Jarvis.Application.Commands;
using Jarvis.Application.Services;
using MediatR;

namespace Jarvis.Application.Handlers;

public class ClearConversationCommandHandler : IRequestHandler<ClearConversationCommand>
{
    private readonly ConversationService _conversationService;

    public ClearConversationCommandHandler(ConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    public async Task Handle(ClearConversationCommand request, CancellationToken cancellationToken)
    {
        await _conversationService.ClearAsync(cancellationToken);
    }
}