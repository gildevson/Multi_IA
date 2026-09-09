using Jarvis.Application.Commands;
using Jarvis.Domain.Interfaces.Repositories;
using MediatR;

namespace Jarvis.Application.Handlers;

public class DeleteSessionCommandHandler : IRequestHandler<DeleteSessionCommand>
{
    private readonly IConversationRepository _repository;

    public DeleteSessionCommandHandler(IConversationRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
        => await _repository.DeleteByIdAsync(request.SessionId, cancellationToken);
}
