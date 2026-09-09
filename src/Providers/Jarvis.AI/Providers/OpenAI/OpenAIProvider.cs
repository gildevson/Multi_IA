using Jarvis.Application.Abstractions;
using Jarvis.Application.DTOs;
using Jarvis.Application.Tools;
using Jarvis.Domain.Entities;
using Jarvis.Domain.Enums;
using Jarvis.Domain.ValueObjects;
using Jarvis.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Runtime.CompilerServices;
using PDFtoImage;
using SkiaSharp;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace Jarvis.AI.Providers.OpenAI;

public class OpenAIProvider : IAIProvider
{
    private readonly JarvisSettings _jarvisSettings;
    private readonly AISettings _settings;
    private readonly ILogger<OpenAIProvider> _logger;
    private readonly Lazy<ChatClient> _chatClient;

    public string Name => "OpenAI";
    public bool SupportsVision => true;
    public bool SupportsTools => true;
    public bool SupportsPdf => true;

    public OpenAIProvider(IOptions<JarvisSettings> settings, ILogger<OpenAIProvider> logger)
    {
        _jarvisSettings = settings.Value;
        _settings = settings.Value.AI;
        _logger = logger;
        _chatClient = new Lazy<ChatClient>(() =>
        {
            var client = new OpenAIClient(_settings.OpenAIApiKey);
            return client.GetChatClient(_settings.OpenAIModel);
        });
    }

    public async Task<AIResponse> SendMessageAsync(IEnumerable<Message> history, CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(history);
        var options = new ChatCompletionOptions { MaxOutputTokenCount = _settings.MaxTokens };
        var completion = await _chatClient.Value.CompleteChatAsync(messages, options, cancellationToken);
        return AIResponse.FromText(completion.Value.Content[0].Text);
    }

    public async Task<AIResponse> SendMessageWithToolsAsync(IEnumerable<Message> history, IEnumerable<ToolDefinition> tools, CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(history);
        var chatTools = tools.Select(BuildChatTool).ToList();
        var options = new ChatCompletionOptions { MaxOutputTokenCount = _settings.MaxTokens };
        foreach (var tool in chatTools) options.Tools.Add(tool);

        var completion = await _chatClient.Value.CompleteChatAsync(messages, options, cancellationToken);
        var choice = completion.Value;

        if (choice.FinishReason == ChatFinishReason.ToolCalls)
        {
            var toolCall = choice.ToolCalls[0];
            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(toolCall.FunctionArguments.ToString()) ?? new();
            return AIResponse.FromToolCall(toolCall.Id, toolCall.FunctionName, args);
        }

        return AIResponse.FromText(choice.Content.Count > 0 ? choice.Content[0].Text : string.Empty);
    }

    public async Task<AIResponse> SendMessageWithVisionAsync(IEnumerable<Message> history, ImageData image, CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(history);
        var imageContent = ChatMessageContentPart.CreateImagePart(
            BinaryData.FromBytes(image.Data), image.MimeType);
        var lastUserMessage = messages.OfType<UserChatMessage>().LastOrDefault();
        if (lastUserMessage is not null)
        {
            var parts = new List<ChatMessageContentPart> { ChatMessageContentPart.CreateTextPart("What do you see?"), imageContent };
            messages.Remove(lastUserMessage);
            messages.Add(new UserChatMessage(parts));
        }

        var options = new ChatCompletionOptions { MaxOutputTokenCount = _settings.MaxTokens };
        var completion = await _chatClient.Value.CompleteChatAsync(messages, options, cancellationToken);
        return AIResponse.FromText(completion.Value.Content[0].Text);
    }

    public async Task<AIResponse> SendMessageWithPdfAsync(IEnumerable<Message> history, PdfData pdf, CancellationToken cancellationToken = default)
    {
        var pdfText = ExtractTextFromPdf(pdf.Data);
        var messages = BuildMessages(history);
        var lastUser = messages.OfType<UserChatMessage>().LastOrDefault();
        var rawQuestion = lastUser is not null ? string.Join("", lastUser.Content.Select(p => p.Text)) : "";
        var userQuestion = System.Text.RegularExpressions.Regex.Replace(rawQuestion, @"^\[PDF:[^\]]+\]\s*", "").Trim();

        // PDF baseado em imagem: sem texto → usa vision com imagens das páginas
        if (string.IsNullOrWhiteSpace(pdfText))
        {
            if (lastUser is not null) messages.Remove(lastUser);
            var pdfImages = Conversion.ToImages(pdf.Data).ToList();
            var parts = new List<ChatMessageContentPart>();
            var question = string.IsNullOrWhiteSpace(userQuestion)
                ? $"Descreva o conteúdo deste documento PDF ({pdf.FileName})."
                : userQuestion;
            parts.Add(ChatMessageContentPart.CreateTextPart(question));
            foreach (var bitmap in pdfImages)
            {
                using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 90);
                parts.Add(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(encoded.ToArray()), "image/png"));
            }
            messages.Add(new UserChatMessage(parts));
        }
        else
        {
            var userPrompt = string.IsNullOrWhiteSpace(userQuestion)
                ? $"Analise o seguinte documento PDF ({pdf.FileName}):\n\n{pdfText}"
                : $"{userQuestion}\n\n[Conteúdo do PDF: {pdf.FileName}]\n{pdfText}";
            if (lastUser is not null) messages.Remove(lastUser);
            messages.Add(new UserChatMessage(userPrompt));
        }

        var options = new ChatCompletionOptions { MaxOutputTokenCount = _settings.MaxTokens };
        var completion = await _chatClient.Value.CompleteChatAsync(messages, options, cancellationToken);
        return AIResponse.FromText(completion.Value.Content[0].Text);
    }

    private static string ExtractTextFromPdf(byte[] pdfBytes)
    {
        try
        {
            using var pdf = PdfDocument.Open(pdfBytes);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
                sb.AppendLine(page.Text);
            return sb.ToString().Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(IEnumerable<Message> history, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(history);
        var options = new ChatCompletionOptions { MaxOutputTokenCount = _settings.MaxTokens };
        var streaming = _chatClient.Value.CompleteChatStreamingAsync(messages, options, cancellationToken);

        await foreach (var update in streaming.WithCancellation(cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                    yield return part.Text;
            }
        }
    }

    private List<ChatMessage> BuildMessages(IEnumerable<Message> history)
    {
        var messages = new List<ChatMessage>();
        var systemMsg = history.FirstOrDefault(m => m.Role == MessageRole.System);

        if (systemMsg is not null)
            messages.Add(new SystemChatMessage(systemMsg.Content.Text));
        else
            messages.Add(new SystemChatMessage(_jarvisSettings.GetEffectiveSystemPrompt()));

        foreach (var msg in history.Where(m => m.Role != MessageRole.System))
        {
            messages.Add(msg.Role switch
            {
                MessageRole.User => new UserChatMessage(msg.Content.Text),
                MessageRole.Assistant => new AssistantChatMessage(msg.Content.Text),
                MessageRole.Tool => new ToolChatMessage(msg.ToolCallId ?? "tool", msg.Content.Text),
                _ => new UserChatMessage(msg.Content.Text)
            });
        }

        return messages;
    }

    private static ChatTool BuildChatTool(ToolDefinition tool)
    {
        var schema = JsonSerializer.Serialize(new
        {
            type = "object",
            properties = tool.Parameters.ToDictionary(
                p => p.Name,
                p => (object)new { type = p.Type, description = p.Description }),
            required = tool.Parameters.Where(p => p.Required).Select(p => p.Name).ToArray()
        });

        return ChatTool.CreateFunctionTool(tool.Name, tool.Description, BinaryData.FromString(schema));
    }
}