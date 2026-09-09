using Jarvis.Domain.ValueObjects;
using Jarvis.Shared.Models;

namespace Jarvis.Application.Abstractions;

public interface IImageAnalysisProvider
{
    Task<Result<string>> AnalyzeAsync(
        ImageData image,
        string prompt,
        CancellationToken cancellationToken = default);
}