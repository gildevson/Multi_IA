using Jarvis.Application.Abstractions;
using Jarvis.Domain.Entities;
using Jarvis.Domain.ValueObjects;
using Jarvis.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Jarvis.Vision.Analysis;

public class MultimodalImageAnalysisProvider : IImageAnalysisProvider
{
    private readonly IAIProvider _aiProvider;
    private readonly ILogger<MultimodalImageAnalysisProvider> _logger;

    public MultimodalImageAnalysisProvider(IAIProvider aiProvider, ILogger<MultimodalImageAnalysisProvider> logger)
    {
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<Result<string>> AnalyzeAsync(ImageData image, string prompt, CancellationToken cancellationToken = default)
    {
        if (!_aiProvider.SupportsVision)
        {
            _logger.LogWarning("Current AI provider '{Name}' does not support vision", _aiProvider.Name);
            return Result.Failure<string>($"Provider '{_aiProvider.Name}' does not support image analysis.");
        }

        try
        {
            var history = new[] { Jarvis.Domain.Entities.Message.CreateUserMessage(prompt) };
            var response = await _aiProvider.SendMessageWithVisionAsync(history, image, cancellationToken);
            return Result.Success(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image analysis failed");
            return Result.Failure<string>($"Image analysis error: {ex.Message}");
        }
    }
}
