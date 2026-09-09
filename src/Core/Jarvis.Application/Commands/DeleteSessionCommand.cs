using MediatR;

namespace Jarvis.Application.Commands;

public record DeleteSessionCommand(Guid SessionId) : IRequest;
