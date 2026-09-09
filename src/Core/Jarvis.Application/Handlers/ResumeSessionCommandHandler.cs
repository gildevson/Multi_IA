using Jarvis.Application.Commands;
using Jarvis.Application.Services;
using Jarvis.Domain.Interfaces.Repositories;
using MediatR;

namespace Jarvis.Application.Handlers;

public class ResumeSessionCommandHandler : IRequestHandler<ResumeSessionCommand>
{
    private readonly IConversationRepository _repository;
    private readonly ConversationService _conversationService;

    public ResumeSessionCommandHandler(IConversationRepository repository, ConversationService conversationService)
    {
        _repository = repository;
        _conversationService = conversationService;
    }

    public async Task Handle(ResumeSessionCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        if (conversation is null) return;
        await _conversationService.ResumeAsync(conversation, cancellationToken);
    }
}
