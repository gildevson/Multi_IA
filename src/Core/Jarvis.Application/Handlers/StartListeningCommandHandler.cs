using Jarvis.Application.Abstractions;
using Jarvis.Application.Commands;
using Jarvis.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Jarvis.Application.Handlers;

public class StartListeningCommandHandler : IRequestHandler<StartListeningCommand, Result<string>>
{
    private readonly ISpeechToTextProvider _sttProvider;
    private readonly ILogger<StartListeningCommandHandler> _logger;

    public StartListeningCommandHandler(ISpeechToTextProvider sttProvider, ILogger<StartListeningCommandHandler> logger)
    {
        _sttProvider = sttProvider;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(StartListeningCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting speech recognition with provider: {Provider}", _sttProvider.Name);

        var sb = new StringBuilder();

        await foreach (var chunk in _sttProvider.StreamTranscriptionAsync(cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                sb.Append(chunk);
                break; // Para após a primeira fala detectada
            }
        }

        var transcription = sb.ToString().Trim();

        if (string.IsNullOrEmpty(transcription))
            return Result.Failure<string>("No speech detected.");

        _logger.LogInformation("Transcribed: {Text}", transcription);
        return Result.Success(transcription);
    }
}