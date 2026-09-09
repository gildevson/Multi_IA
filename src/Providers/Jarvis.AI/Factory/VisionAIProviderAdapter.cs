using Jarvis.Application.Abstractions;
using Jarvis.Application.DTOs;
using Jarvis.Application.Tools;
using Jarvis.Domain.Entities;
using Jarvis.Domain.ValueObjects;

namespace Jarvis.AI.Factory;

public class VisionAIProviderAdapter : IVisionAIProvider
{
    private readonly IAIProvider _inner;

    public VisionAIProviderAdapter(IAIProvider inner) => _inner = inner;

    public string Name => _inner.Name;
    public bool SupportsVision => _inner.SupportsVision;
    public bool SupportsTools => _inner.SupportsTools;
    public bool SupportsPdf => _inner.SupportsPdf;

    public Task<AIResponse> SendMessageAsync(IEnumerable<Message> history, CancellationToken cancellationToken = default)
        => _inner.SendMessageAsync(history, cancellationToken);

    public Task<AIResponse> SendMessageWithToolsAsync(IEnumerable<Message> history, IEnumerable<ToolDefinition> tools, CancellationToken cancellationToken = default)
        => _inner.SendMessageWithToolsAsync(history, tools, cancellationToken);

    public Task<AIResponse> SendMessageWithVisionAsync(IEnumerable<Message> history, ImageData image, CancellationToken cancellationToken = default)
        => _inner.SendMessageWithVisionAsync(history, image, cancellationToken);

    public Task<AIResponse> SendMessageWithPdfAsync(IEnumerable<Message> history, PdfData pdf, CancellationToken cancellationToken = default)
        => _inner.SendMessageWithPdfAsync(history, pdf, cancellationToken);

    public IAsyncEnumerable<string> StreamMessageAsync(IEnumerable<Message> history, CancellationToken cancellationToken = default)
        => _inner.StreamMessageAsync(history, cancellationToken);
}
