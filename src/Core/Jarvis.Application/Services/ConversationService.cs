using Jarvis.Domain.Entities;
using Jarvis.Domain.Enums;
using Jarvis.Domain.Events;
using Jarvis.Domain.Interfaces.Repositories;
using Jarvis.Domain.Interfaces.Services;
using Jarvis.Domain.ValueObjects;
using Jarvis.Application.Tools;
using Microsoft.Extensions.Logging;

namespace Jarvis.Application.Services;

public class ConversationService
{
    private readonly IConversationRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<ConversationService> _logger;
    private Conversation? _currentConversation;

    public ConversationService(
        IConversationRepository repository,
        IDomainEventDispatcher eventDispatcher,
        ILogger<ConversationService> logger)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<Conversation> GetOrCreateCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (_currentConversation is not null)
            return _currentConversation;

        _currentConversation = await _repository.GetCurrentAsync(cancellationToken);

        if (_currentConversation is null)
        {
            _currentConversation = new Conversation();
            await _repository.SaveAsync(_currentConversation, cancellationToken);
            await _eventDispatcher.DispatchAsync(
                ConversationStartedEvent.Create(_currentConversation.Id), cancellationToken);
            _logger.LogInformation("New conversation started: {ConversationId}", _currentConversation.Id);
        }

        return _currentConversation;
    }

    public async Task AddUserMessageAsync(string content, CancellationToken cancellationToken = default)
    {
        var conversation = await GetOrCreateCurrentAsync(cancellationToken);

        // Auto-título: usa a primeira mensagem do usuário como título da conversa
        if (conversation.Title is null && !conversation.Messages.Any(m => m.Role == MessageRole.User))
        {
            var title = content.Length > 45 ? content[..45] + "…" : content;
            conversation.SetTitle(title);
        }

        var message = Message.CreateUserMessage(content);
        conversation.AddMessage(message);
        await _repository.SaveAsync(conversation, cancellationToken);
        await _eventDispatcher.DispatchAsync(
            MessageReceivedEvent.Create(conversation.Id, message), cancellationToken);
        _logger.LogDebug("User message added to conversation {ConversationId}", conversation.Id);
    }

    public async Task AddUserMessageWithPdfAsync(string content, PdfData pdf, CancellationToken cancellationToken = default)
    {
        var conversation = await GetOrCreateCurrentAsync(cancellationToken);
        var text = string.IsNullOrWhiteSpace(content)
            ? $"[PDF: {pdf.FileName}]"
            : $"[PDF: {pdf.FileName}] {content}";
        var message = Message.CreateUserMessage(text);
        conversation.AddMessage(message);
        await _repository.SaveAsync(conversation, cancellationToken);
        await _eventDispatcher.DispatchAsync(
            MessageReceivedEvent.Create(conversation.Id, message), cancellationToken);
        _logger.LogDebug("User message with PDF added to conversation {ConversationId}", conversation.Id);
    }

    public async Task AddUserMessageWithImageAsync(string content, ImageData image, CancellationToken cancellationToken = default)
    {
        var conversation = await GetOrCreateCurrentAsync(cancellationToken);
        var message = Message.CreateUserMessageWithImage(content, image);
        conversation.AddMessage(message);
        await _repository.SaveAsync(conversation, cancellationToken);
        await _eventDispatcher.DispatchAsync(
            MessageReceivedEvent.Create(conversation.Id, message), cancellationToken);
        _logger.LogDebug("User message with image added to conversation {ConversationId}", conversation.Id);
    }

    public async Task AddAssistantMessageAsync(string content, CancellationToken cancellationToken = default)
    {
        var conversation = await GetOrCreateCurrentAsync(cancellationToken);
        var message = Message.CreateAssistantMessage(content);
        conversation.AddMessage(message);
        await _repository.SaveAsync(conversation, cancellationToken);
        _logger.LogDebug("Assistant message added to conversation {ConversationId}", conversation.Id);
    }

    public async Task AddToolResultAsync(string toolCallId, string toolName, ToolResult result, CancellationToken cancellationToken = default)
    {
        var conversation = await GetOrCreateCurrentAsync(cancellationToken);
        var output = result.Success ? result.Output ?? "Done" : $"Error: {result.Error}";
        var message = Message.CreateToolResultMessage(toolCallId, toolName, output);
        conversation.AddMessage(message);
        await _repository.SaveAsync(conversation, cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var conversation = await GetOrCreateCurrentAsync(cancellationToken);
        return conversation.Messages;
    }

    public async Task ResumeAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _currentConversation = conversation;
        await _repository.SaveAsync(conversation, cancellationToken);
        _logger.LogInformation("Resumed conversation: {ConversationId}", conversation.Id);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _currentConversation = null;
        await _repository.ClearCurrentAsync(cancellationToken);
        _logger.LogInformation("Conversation cleared");
    }
}