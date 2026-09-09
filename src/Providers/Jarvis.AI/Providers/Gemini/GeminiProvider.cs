using Jarvis.Application.Abstractions;
using Jarvis.Application.DTOs;
using Jarvis.Application.Tools;
using Jarvis.Domain.Entities;
using Jarvis.Domain.Enums;
using Jarvis.Domain.ValueObjects;
using Jarvis.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace Jarvis.AI.Providers.Gemini;

public class GeminiProvider : IAIProvider
{
    private readonly JarvisSettings _jarvisSettings;
    private readonly AISettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiProvider> _logger;

    public string Name => "Gemini";
    public bool SupportsVision => true;
    public bool SupportsTools => true;
    public bool SupportsPdf => true;

    public GeminiProvider(IOptions<JarvisSettings> settings, IHttpClientFactory httpClientFactory, ILogger<GeminiProvider> logger)
    {
        _jarvisSettings = settings.Value;
        _settings = settings.Value.AI;
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _logger = logger;
    }

    public async Task<AIResponse> SendMessageAsync(IEnumerable<Message> history, CancellationToken cancellationToken = default)
    {
        var contents = BuildContents(history);
        var systemPrompt = _jarvisSettings.GetEffectiveSystemPrompt();
        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents
        };
        var responseText = await PostAsync(payload, cancellationToken);
        return AIResponse.FromText(responseText);
    }

    public Task<AIResponse> SendMessageWithToolsAsync(IEnumerable<Message> history, IEnumerable<ToolDefinition> tools, CancellationToken cancellationToken = default)
        => SendMessageAsync(history, cancellationToken);

    public async Task<AIResponse> SendMessageWithVisionAsync(IEnumerable<Message> history, ImageData image, CancellationToken cancellationToken = default)
    {
        var contents = BuildContents(history);
        var imagePart = new { inline_data = new { mime_type = image.MimeType, data = image.ToBase64() } };
        if (contents.Count > 0)
        {
            var lastParts = new List<object> { imagePart, new { text = "What do you see in this image?" } };
            contents[^1] = new { role = "user", parts = lastParts.ToArray() };
        }

        var systemPrompt = _jarvisSettings.GetEffectiveSystemPrompt();
        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents
        };
        var responseText = await PostAsync(payload, cancellationToken, isVision: true);
        return AIResponse.FromText(responseText);
    }

    public async Task<AIResponse> SendMessageWithPdfAsync(IEnumerable<Message> history, PdfData pdf, CancellationToken cancellationToken = default)
    {
        // Gemini suporta PDF nativo via inline_data
        var contents = BuildContents(history);
        var lastUserText = history.Where(m => m.Role == MessageRole.User).LastOrDefault()?.Content.Text ?? "Analise este documento.";

        if (contents.Count > 0) contents.RemoveAt(contents.Count - 1);

        contents.Add(new
        {
            role = "user",
            parts = new object[]
            {
                new { inline_data = new { mime_type = "application/pdf", data = pdf.ToBase64() } },
                new { text = lastUserText }
            }
        });

        var systemPrompt = _jarvisSettings.GetEffectiveSystemPrompt();
        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents
        };
        var responseText = await PostAsync(payload, cancellationToken);
        return AIResponse.FromText(responseText);
    }

    private static string ExtractTextFromPdf(byte[] pdfBytes)
    {
        using var pdf = PdfDocument.Open(pdfBytes);
        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString();
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(IEnumerable<Message> history, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text = await SendMessageAsync(history, cancellationToken);
        yield return text.Content;
    }

    private List<object> BuildContents(IEnumerable<Message> history)
    {
        return history
            .Where(m => m.Role != MessageRole.System)
            .Select(m => (object)new
            {
                role = m.Role == MessageRole.Assistant ? "model" : "user",
                parts = new[] { new { text = m.Content.Text } }
            })
            .ToList();
    }

    private async Task<string> PostAsync(object payload, CancellationToken cancellationToken, bool isVision = false)
    {
        var model = isVision && !string.IsNullOrEmpty(_settings.GeminiVisionModel)
            ? _settings.GeminiVisionModel
            : _settings.GeminiModel;
        var url = $"v1beta/models/{model}:generateContent?key={_settings.GeminiApiKey}";
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }
}
