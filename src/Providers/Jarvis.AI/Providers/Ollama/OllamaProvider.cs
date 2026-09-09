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
using PDFtoImage;
using SkiaSharp;
using UglyToad.PdfPig;

namespace Jarvis.AI.Providers.Ollama;

public class OllamaProvider : IAIProvider
{
    private readonly JarvisSettings _jarvisSettings;
    private readonly AISettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaProvider> _logger;

    public string Name => "Ollama";
    public bool SupportsVision => !string.IsNullOrEmpty(_settings.OllamaVisionModel);
    public bool SupportsTools => false;
    public bool SupportsPdf => true;

    public OllamaProvider(IOptions<JarvisSettings> settings, IHttpClientFactory httpClientFactory, ILogger<OllamaProvider> logger)
    {
        _jarvisSettings = settings.Value;
        _settings = settings.Value.AI;
        _httpClient = httpClientFactory.CreateClient("Ollama");
        _httpClient.BaseAddress = new Uri(_settings.OllamaBaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _logger = logger;
    }

    public async Task<AIResponse> SendMessageAsync(IEnumerable<Message> history, CancellationToken cancellationToken = default)
    {
        var (system, messages) = BuildMessages(history);
        var payload = new { model = _settings.OllamaModel, system, messages, stream = false };
        _logger.LogInformation("Ollama usando modelo de texto: {Model}", _settings.OllamaModel);
        var response = await PostAsync("/api/chat", payload, cancellationToken);
        var content = response?.RootElement.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        return AIResponse.FromText(content);
    }

    public Task<AIResponse> SendMessageWithToolsAsync(IEnumerable<Message> history, IEnumerable<ToolDefinition> tools, CancellationToken cancellationToken = default)
        => SendMessageAsync(history, cancellationToken);

    public async Task<AIResponse> SendMessageWithVisionAsync(IEnumerable<Message> history, ImageData image, CancellationToken cancellationToken = default)
    {
        var visionModel = _settings.OllamaVisionModel;
        _logger.LogInformation("Ollama usando modelo de visão: {Model}", visionModel);

        var list = history.ToList();
        var systemPrompt = list.FirstOrDefault(m => m.Role == MessageRole.System)?.Content.Text
                           ?? _jarvisSettings.GetEffectiveSystemPrompt();

        // Para visão, usa apenas a última mensagem do usuário — o histórico de texto confunde modelos de visão
        var lastUserMessage = list.LastOrDefault(m => m.Role == MessageRole.User)?.Content.Text ?? string.Empty;
        var prompt = string.IsNullOrEmpty(systemPrompt)
            ? lastUserMessage
            : $"{systemPrompt}\n\n{lastUserMessage}";

        // /api/generate funciona de forma mais confiável com llava para visão
        var payload = new
        {
            model = visionModel,
            prompt,
            images = new[] { image.ToBase64() },
            stream = false,
            options = new { num_ctx = 8192 }
        };

        var response = await PostAsync("/api/generate", payload, cancellationToken);
        var content = response?.RootElement.GetProperty("response").GetString() ?? string.Empty;
        return AIResponse.FromText(content);
    }

    public async Task<AIResponse> SendMessageWithPdfAsync(IEnumerable<Message> history, PdfData pdf, CancellationToken cancellationToken = default)
    {
        var lastUserText = history.Where(m => m.Role == MessageRole.User).LastOrDefault()?.Content.Text ?? "";
        // Remove o prefixo [PDF: filename] que foi adicionado pelo ConversationService
        var userQuestion = System.Text.RegularExpressions.Regex.Replace(lastUserText, @"^\[PDF:[^\]]+\]\s*", "").Trim();

        var (pdfText, hasRealText) = ExtractTextFromPdf(pdf.Data);

        // PDF baseado em imagem: sem texto extraível (só marcadores de página) → usa modelo de visão
        if (!hasRealText && !string.IsNullOrEmpty(_settings.OllamaVisionModel))
        {
            _logger.LogInformation("PDF sem texto extraível — usando visão para analisar as páginas");
            return await AnalyzePdfWithVisionAsync(pdf, userQuestion, cancellationToken);
        }

        var (system, messages) = BuildMessages(history);
        if (messages.Count > 0) messages.RemoveAt(messages.Count - 1);

        var prompt = string.IsNullOrWhiteSpace(userQuestion)
            ? $"Analise o seguinte documento PDF ({pdf.FileName}):\n\n{pdfText}"
            : $"{userQuestion}\n\n[Conteúdo do PDF: {pdf.FileName}]\n{pdfText}";

        messages.Add(new { role = "user", content = prompt });

        var pdfModel = !string.IsNullOrEmpty(_settings.OllamaPdfModel) ? _settings.OllamaPdfModel : _settings.OllamaModel;
        var payload = new { model = pdfModel, system, messages, stream = false };
        var response = await PostAsync("/api/chat", payload, cancellationToken);
        var content = response?.RootElement.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        return AIResponse.FromText(content);
    }

    private async Task<AIResponse> AnalyzePdfWithVisionAsync(PdfData pdf, string userQuestion, CancellationToken cancellationToken)
    {
        // Converte cada página do PDF em imagem e envia pro modelo de visão
        var images = Conversion.ToImages(pdf.Data).ToList();
        var sb = new StringBuilder();
        var question = string.IsNullOrWhiteSpace(userQuestion)
            ? $"Descreva o conteúdo deste documento PDF ({pdf.FileName})."
            : userQuestion;

        for (int i = 0; i < images.Count; i++)
        {
            _logger.LogInformation("Analisando página {Page}/{Total} do PDF com visão", i + 1, images.Count);

            using var bitmap = images[i];
            using var imageData = bitmap.Encode(SKEncodedImageFormat.Png, 90);
            var imageBase64 = Convert.ToBase64String(imageData.ToArray());

            var pagePrompt = images.Count == 1
                ? question
                : $"Página {i + 1} de {images.Count} do PDF '{pdf.FileName}'. {question}";

            var payload = new
            {
                model = _settings.OllamaVisionModel,
                system = _jarvisSettings.GetEffectiveSystemPrompt(),
                prompt = pagePrompt,
                images = new[] { imageBase64 },
                stream = false
            };

            var response = await PostAsync("/api/generate", payload, cancellationToken);
            var pageContent = response?.RootElement.GetProperty("response").GetString() ?? string.Empty;

            if (images.Count > 1)
                sb.AppendLine($"**Página {i + 1}:** {pageContent}");
            else
                sb.Append(pageContent);
        }

        return AIResponse.FromText(sb.ToString());
    }

    private static (string text, bool hasRealText) ExtractTextFromPdf(byte[] pdfBytes)
    {
        try
        {
            using var pdf = PdfDocument.Open(pdfBytes);
            var sb = new StringBuilder();
            bool hasRealText = false;
            foreach (var page in pdf.GetPages())
            {
                var pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText))
                    hasRealText = true;
                sb.AppendLine($"=== PÁGINA {page.Number} de {pdf.NumberOfPages} ===");
                sb.AppendLine(pageText);
                sb.AppendLine();
            }
            return (sb.ToString().Trim(), hasRealText);
        }
        catch
        {
            return (string.Empty, false);
        }
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(IEnumerable<Message> history, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (system, messages) = BuildMessages(history);
        var payload = new { model = _settings.OllamaModel, system, messages, stream = true };
        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var resp = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;
            string? text = null;
            JsonDocument? doc = null;
            try { doc = JsonDocument.Parse(line); } catch { }
            if (doc is not null)
                text = doc.RootElement.GetProperty("message").GetProperty("content").GetString();
            if (!string.IsNullOrEmpty(text)) yield return text;
        }
    }

    private (string system, List<object> messages) BuildMessages(IEnumerable<Message> history)
    {
        var list = history.ToList();
        var system = list.FirstOrDefault(m => m.Role == MessageRole.System)?.Content.Text
                     ?? _jarvisSettings.GetEffectiveSystemPrompt();
        var messages = new List<object>();
        // Inclui system como primeira mensagem do array (qwen3 e outros modelos respeitam melhor assim)
        if (!string.IsNullOrEmpty(system))
            messages.Add(new { role = "system", content = system });
        messages.AddRange(list
            .Where(m => m.Role != MessageRole.System)
            .Select(m => (object)new { role = m.Role.ToString().ToLower(), content = m.Content.Text }));
        return (system, messages);
    }

    private async Task<JsonDocument?> PostAsync(string endpoint, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Ollama {Endpoint} retornou {Status}: {Body}", endpoint, (int)response.StatusCode, errorBody);
            throw new HttpRequestException($"Ollama {(int)response.StatusCode}: {errorBody}");
        }
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }
}
