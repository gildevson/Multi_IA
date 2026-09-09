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

namespace Jarvis.AI.Providers.Claude;

public class ClaudeProvider : IAIProvider
{
    private readonly JarvisSettings _jarvisSettings;
    private readonly AISettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeProvider> _logger;

    private const string BaseUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    public string Name => "Claude";
    public bool SupportsVision => true;
    public bool SupportsTools => true;
    public bool SupportsPdf => true;

    public ClaudeProvider(IOptions<JarvisSettings> settings, IHttpClientFactory httpClientFactory, ILogger<ClaudeProvider> logger)
    {
        _jarvisSettings = settings.Value;
        _settings = settings.Value.AI;
        _httpClient = httpClientFactory.CreateClient("Claude");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ClaudeApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _logger = logger;
    }

    public async Task<AIResponse> SendMessageAsync(IEnumerable<Message> history, CancellationToken cancellationToken = default)
    {
        var (systemMsg, messages) = BuildMessages(history);
        var payload = new { model = _settings.ClaudeModel, max_tokens = _settings.MaxTokens, system = systemMsg, messages };
        return await PostAndParseAsync(payload, cancellationToken);
    }

    public async Task<AIResponse> SendMessageWithToolsAsync(IEnumerable<Message> history, IEnumerable<ToolDefinition> tools, CancellationToken cancellationToken = default)
    {
        var (systemMsg, messages) = BuildMessages(history);
        var claudeTools = tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            input_schema = new
            {
                type = "object",
                properties = t.Parameters.ToDictionary(
                    p => p.Name,
                    p => (object)new { type = p.Type, description = p.Description }),
                required = t.Parameters.Where(p => p.Required).Select(p => p.Name).ToArray()
            }
        }).ToList();

        var payload = new { model = _settings.ClaudeModel, max_tokens = _settings.MaxTokens, system = systemMsg, messages, tools = claudeTools };
        return await PostAndParseAsync(payload, cancellationToken);
    }

    public async Task<AIResponse> SendMessageWithVisionAsync(IEnumerable<Message> history, ImageData image, CancellationToken cancellationToken = default)
    {
        var (systemMsg, messages) = BuildMessages(history);
        var lastContent = messages.Count > 0 ? messages[^1].content?.ToString() ?? "What do you see?" : "What do you see?";
        if (messages.Count > 0) messages.RemoveAt(messages.Count - 1);

        messages.Add(new
        {
            role = "user",
            content = (object)new object[]
            {
                new { type = "image", source = new { type = "base64", media_type = image.MimeType, data = image.ToBase64() } },
                new { type = "text", text = lastContent }
            }
        });

        var payload = new { model = _settings.ClaudeModel, max_tokens = _settings.MaxTokens, system = systemMsg, messages };
        return await PostAndParseAsync(payload, cancellationToken);
    }

    public async Task<AIResponse> SendMessageWithPdfAsync(IEnumerable<Message> history, PdfData pdf, CancellationToken cancellationToken = default)
    {
        var (systemMsg, messages) = BuildMessages(history);
        var lastContent = messages.Count > 0 ? messages[^1].content?.ToString() ?? "Analise este documento." : "Analise este documento.";
        if (messages.Count > 0) messages.RemoveAt(messages.Count - 1);

        messages.Add(new
        {
            role = "user",
            content = (object)new object[]
            {
                new { type = "document", source = new { type = "base64", media_type = "application/pdf", data = pdf.ToBase64() } },
                new { type = "text", text = lastContent }
            }
        });

        var payload = new { model = _settings.ClaudeModel, max_tokens = _settings.MaxTokens, system = systemMsg, messages };
        return await PostAndParseAsync(payload, cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(IEnumerable<Message> history, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (systemMsg, messages) = BuildMessages(history);
        var payload = new { model = _settings.ClaudeModel, max_tokens = _settings.MaxTokens, system = systemMsg, messages, stream = true };
        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data:")) continue;
            var data = line["data:".Length..].Trim();
            if (data == "[DONE]") break;
            string? yieldText = null;
            JsonDocument? doc = null;
            try { doc = JsonDocument.Parse(data); } catch { }
            if (doc is not null &&
                doc.RootElement.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var textEl))
                yieldText = textEl.GetString();
            if (!string.IsNullOrEmpty(yieldText)) yield return yieldText;
        }
    }

    private (string system, List<dynamic> messages) BuildMessages(IEnumerable<Message> history)
    {
        var msgs = history.ToList();
        var system = msgs.FirstOrDefault(m => m.Role == MessageRole.System)?.Content.Text ?? _jarvisSettings.GetEffectiveSystemPrompt();
        var messages = msgs
            .Where(m => m.Role != MessageRole.System)
            .Select(m => (dynamic)new
            {
                role = m.Role == MessageRole.Assistant ? "assistant" : "user",
                content = m.Content.Text
            })
            .ToList();
        return (system, messages);
    }

    private async Task<AIResponse> PostAndParseAsync(object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(BaseUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("content", out var contentArray))
            return AIResponse.FromText(string.Empty);

        foreach (var item in contentArray.EnumerateArray())
        {
            var type = item.GetProperty("type").GetString();
            if (type == "text")
                return AIResponse.FromText(item.GetProperty("text").GetString() ?? string.Empty);

            if (type == "tool_use")
            {
                var toolName = item.GetProperty("name").GetString() ?? string.Empty;
                var toolId = item.GetProperty("id").GetString() ?? string.Empty;
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(item.GetProperty("input").GetRawText()) ?? new();
                return AIResponse.FromToolCall(toolId, toolName, args);
            }
        }

        return AIResponse.FromText(string.Empty);
    }
}
