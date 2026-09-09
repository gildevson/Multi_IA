using Jarvis.Domain.ValueObjects;
using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Commands;

public record CaptureScreenCommand : IRequest<Result<ImageData>>;