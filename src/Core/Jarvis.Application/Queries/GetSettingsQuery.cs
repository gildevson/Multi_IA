using Jarvis.Shared.Models;
using MediatR;

namespace Jarvis.Application.Queries;

public record GetSettingsQuery<T>(string Key) : IRequest<Result<T?>>;