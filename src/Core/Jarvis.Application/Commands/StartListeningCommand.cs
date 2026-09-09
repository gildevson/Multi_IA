using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Commands;

public record StartListeningCommand : IRequest<Result<string>>;