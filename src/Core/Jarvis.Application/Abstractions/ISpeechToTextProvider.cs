using Jarvis.Domain.ValueObjects;
using Jarvis.Shared.Models;

namespace Jarvis.Application.Abstractions;

public interface ISpeechToTextProvider
{
    string Name { get; }

    Task<Result<string>> TranscribeAudioAsync(
        AudioData audio,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamTranscriptionAsync(
        CancellationToken cancellationToken = default);
}