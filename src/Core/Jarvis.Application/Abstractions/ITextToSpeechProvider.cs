using Jarvis.Domain.ValueObjects;
using Jarvis.Shared.Models;

namespace Jarvis.Application.Abstractions;

public interface ITextToSpeechProvider
{
    string Name { get; }
    bool IsSpeaking { get; }

    Task SpeakAsync(
        string text,
        CancellationToken cancellationToken = default);

    Task<Result<AudioData>> SynthesizeAsync(
        string text,
        CancellationToken cancellationToken = default);

    void StopSpeaking();
}