using Jarvis.Application.Abstractions;
using Jarvis.Application.Commands;
using Jarvis.Domain.ValueObjects;
using Jarvis.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Jarvis.Application.Handlers;

public class CaptureScreenCommandHandler : IRequestHandler<CaptureScreenCommand, Result<ImageData>>
{
    private readonly IScreenCaptureProvider _captureProvider;
    private readonly ILogger<CaptureScreenCommandHandler> _logger;

    public CaptureScreenCommandHandler(IScreenCaptureProvider captureProvider, ILogger<CaptureScreenCommandHandler> logger)
    {
        _captureProvider = captureProvider;
        _logger = logger;
    }

    public async Task<Result<ImageData>> Handle(CaptureScreenCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Capturing screen");
        return await _captureProvider.CaptureFullScreenAsync(cancellationToken);
    }
}