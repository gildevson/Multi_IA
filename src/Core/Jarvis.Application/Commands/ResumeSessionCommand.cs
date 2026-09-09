using MediatR;

namespace Jarvis.Application.Commands;

public record ResumeSessionCommand(Guid SessionId) : IRequest;
