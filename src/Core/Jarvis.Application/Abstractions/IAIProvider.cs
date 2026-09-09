using Jarvis.Application.DTOs;
using Jarvis.Application.Tools;
using Jarvis.Domain.Entities;
using Jarvis.Domain.ValueObjects;

namespace Jarvis.Application.Abstractions;

public interface IAIProvider
{
    string Name { get; }
    bool SupportsVision { get; }
    bool SupportsTools { get; }
    bool SupportsPdf { get; }

    Task<AIResponse> SendMessageAsync(
        IEnumerable<Message> history,
        CancellationToken cancellationToken = default);

    Task<AIResponse> SendMessageWithToolsAsync(
        IEnumerable<Message> history,
        IEnumerable<ToolDefinition> tools,
        CancellationToken cancellationToken = default);

    Task<AIResponse> SendMessageWithVisionAsync(
        IEnumerable<Message> history,
        ImageData image,
        CancellationToken cancellationToken = default);

    Task<AIResponse> SendMessageWithPdfAsync(
        IEnumerable<Message> history,
        PdfData pdf,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamMessageAsync(
        IEnumerable<Message> history,
        CancellationToken cancellationToken = default);
}