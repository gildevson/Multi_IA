using Jarvis.Application.Abstractions;
using Jarvis.Application.DTOs;
using Jarvis.Domain.Entities;
using Jarvis.Domain.Enums;
using Jarvis.Domain.Events;
using Jarvis.Domain.Interfaces.Services;
using Jarvis.Domain.ValueObjects;
using Jarvis.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Jarvis.Application.Services;

public class AssistantOrchestrator
{
    private readonly IAIProvider _aiProvider;
    private readonly IVisionAIProvider _visionProvider;
    private readonly IToolRegistry _toolRegistry;
    private readonly ITextToSpeechProvider _ttsProvider;
    private readonly ConversationService _conversationService;
    private readonly ToolDispatcher _toolDispatcher;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<AssistantOrchestrator> _logger;

    public AssistantOrchestrator(
        IAIProvider aiProvider,
        IVisionAIProvider visionProvider,
        IToolRegistry toolRegistry,
        ITextToSpeechProvider ttsProvider,
        ConversationService conversationService,
        ToolDispatcher toolDispatcher,
        IDomainEventDispatcher eventDispatcher,
        ILogger<AssistantOrchestrator> logger)
    {
        _aiProvider = aiProvider;
        _visionProvider = visionProvider;
        _toolRegistry = toolRegistry;
        _ttsProvider = ttsProvider;
        _conversationService = conversationService;
        _toolDispatcher = toolDispatcher;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<Result<AIResponse>> ProcessUserInputWithPdfAsync(
        string userInput,
        PdfData pdf,
        bool useVoiceResponse = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing user input with PDF: {FileName}", pdf.FileName);

        await _conversationService.AddUserMessageWithPdfAsync(userInput, pdf, cancellationToken);

        if (!_aiProvider.SupportsPdf)
            return Result.Failure<AIResponse>($"O provider '{_aiProvider.Name}' não suporta leitura de PDF.");

        var history = await _conversationService.GetHistoryAsync(cancellationToken);

        AIResponse aiResponse;
        try
        {
            aiResponse = await _aiProvider.SendMessageWithPdfAsync(history, pdf, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI provider failed processing PDF");
            return Result.Failure<AIResponse>($"AI provider error: {ex.Message}");
        }

        if (!string.IsNullOrEmpty(aiResponse.Content))
        {
            await _conversationService.AddAssistantMessageAsync(aiResponse.Content, cancellationToken);
            if (useVoiceResponse)
            {
                try { await _ttsProvider.SpeakAsync(aiResponse.Content, cancellationToken); }
                catch (Exception ex) { _logger.LogWarning(ex, "TTS failed"); }
            }
        }

        return Result.Success(aiResponse);
    }

    public async Task<Result<AIResponse>> ProcessUserInputWithImageAsync(
        string userInput,
        ImageData image,
        bool useVoiceResponse = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing user input with image");

        await _conversationService.AddUserMessageWithImageAsync(userInput, image, cancellationToken);

        if (!_visionProvider.SupportsVision)
            return Result.Failure<AIResponse>($"O provider de visão '{_visionProvider.Name}' não suporta análise de imagens. Configure um Vision Provider em Settings.");

        var history = await _conversationService.GetHistoryAsync(cancellationToken);

        AIResponse aiResponse;
        try
        {
            aiResponse = await _visionProvider.SendMessageWithVisionAsync(history, image, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI provider failed processing image");
            return Result.Failure<AIResponse>($"AI provider error: {ex.Message}");
        }

        if (!string.IsNullOrEmpty(aiResponse.Content))
        {
            await _conversationService.AddAssistantMessageAsync(aiResponse.Content, cancellationToken);
            if (useVoiceResponse)
            {
                try { await _ttsProvider.SpeakAsync(aiResponse.Content, cancellationToken); }
                catch (Exception ex) { _logger.LogWarning(ex, "TTS failed"); }
            }
        }

        return Result.Success(aiResponse);
    }

    public async Task<Result<AIResponse>> ProcessUserInputAsync(
        string userInput,
        bool useVoiceResponse = false,
        CancellationToken cancellationToken = default,
        string? aiContextPrompt = null)
    {
        _logger.LogInformation("Processing user input: {Input}", userInput.Length > 100
            ? userInput[..100] + "..."
            : userInput);

        // Salva no histórico apenas o texto original do usuário
        await _conversationService.AddUserMessageAsync(userInput, cancellationToken);

        IEnumerable<Message> history = await _conversationService.GetHistoryAsync(cancellationToken);

        // Se tem contexto de busca, substitui a última mensagem do usuário SOMENTE para a IA
        // sem persistir — o histórico gravado continua com o texto original
        if (!string.IsNullOrEmpty(aiContextPrompt))
        {
            var historyList = history.ToList();
            var lastUserIdx = historyList.FindLastIndex(m => m.Role == MessageRole.User);
            if (lastUserIdx >= 0)
                historyList[lastUserIdx] = Message.CreateUserMessage(aiContextPrompt);
            history = historyList;
        }
        var toolDefinitions = _toolRegistry.GetAllDefinitions().ToList();

        AIResponse aiResponse;

        try
        {
            if (toolDefinitions.Count > 0 && _aiProvider.SupportsTools)
            {
                aiResponse = await _aiProvider.SendMessageWithToolsAsync(history, toolDefinitions, cancellationToken);
            }
            else
            {
                aiResponse = await _aiProvider.SendMessageAsync(history, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI provider failed");
            return Result.Failure<AIResponse>($"AI provider error: {ex.Message}");
        }

        // Handle tool call loop
        int maxToolIterations = 5;
        int iteration = 0;

        while (aiResponse.HasToolCall && iteration < maxToolIterations)
        {
            iteration++;
            _logger.LogInformation("AI requested tool: {ToolName} (iteration {Iteration})",
                aiResponse.ToolName, iteration);

            var toolArgs = aiResponse.ToolArguments ?? new Dictionary<string, object>();
            var toolResult = await _toolDispatcher.DispatchAsync(
                aiResponse.ToolName!, toolArgs, cancellationToken);

            var resultText = toolResult.IsSuccess
                ? toolResult.Value.Output ?? "Tool executed successfully"
                : $"Tool error: {toolResult.Error}";

            await _conversationService.AddToolResultAsync(
                aiResponse.ToolCallId ?? Guid.NewGuid().ToString(),
                aiResponse.ToolName!,
                toolResult.IsSuccess ? toolResult.Value : new Application.Tools.ToolResult(false, Error: toolResult.Error),
                cancellationToken);

            history = (await _conversationService.GetHistoryAsync(cancellationToken));

            try
            {
                aiResponse = toolDefinitions.Count > 0 && _aiProvider.SupportsTools
                    ? await _aiProvider.SendMessageWithToolsAsync(history, toolDefinitions, cancellationToken)
                    : await _aiProvider.SendMessageAsync(history, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI provider failed after tool execution");
                return Result.Failure<AIResponse>($"AI provider error after tool: {ex.Message}");
            }
        }

        if (!string.IsNullOrEmpty(aiResponse.Content))
        {
            await _conversationService.AddAssistantMessageAsync(aiResponse.Content, cancellationToken);

            if (useVoiceResponse)
            {
                try
                {
                    await _ttsProvider.SpeakAsync(aiResponse.Content, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "TTS failed, response will be text-only");
                }
            }
        }

        _logger.LogInformation("Request processed successfully");
        return Result.Success(aiResponse);
    }
}