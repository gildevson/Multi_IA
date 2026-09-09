using Jarvis.Application.Commands;
using Jarvis.Application.DTOs;
using Jarvis.Application.Services;
using Jarvis.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Jarvis.Application.Handlers;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<AIResponse>>
{
    private readonly AssistantOrchestrator _orchestrator;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(AssistantOrchestrator orchestrator, ILogger<SendMessageCommandHandler> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<Result<AIResponse>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserInput) && request.Image is null && request.Pdf is null)
            return Result.Failure<AIResponse>("User input cannot be empty.");

        if (request.Pdf is not null)
            return await _orchestrator.ProcessUserInputWithPdfAsync(
                request.UserInput,
                request.Pdf,
                request.UseVoiceResponse,
                cancellationToken);

        if (request.Image is not null)
            return await _orchestrator.ProcessUserInputWithImageAsync(
                request.UserInput,
                request.Image,
                request.UseVoiceResponse,
                cancellationToken);

        return await _orchestrator.ProcessUserInputAsync(
            request.UserInput,
            request.UseVoiceResponse,
            cancellationToken,
            request.AiContextPrompt);
    }
}