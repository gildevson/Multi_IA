using Jarvis.Domain.Entities;
using Jarvis.Domain.Enums;
using Jarvis.Domain.Interfaces.Repositories;
using Jarvis.Domain.ValueObjects;
using Jarvis.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jarvis.Infrastructure.Repositories;

public class JsonConversationRepository : IConversationRepository
{
    private readonly string _conversationsFolder;
    private readonly string _currentFilePath;
    private readonly ILogger<JsonConversationRepository> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonConversationRepository(IOptions<JarvisSettings> settings, ILogger<JsonConversationRepository> logger)
    {
        var dataFolder = settings.Value.DataFolder;
        _conversationsFolder = Path.Combine(dataFolder, "conversations");
        _currentFilePath = Path.Combine(_conversationsFolder, "current.json");
        _logger = logger;
        Directory.CreateDirectory(_conversationsFolder);
    }

    // ── Persistence DTOs ────────────────────────────────────────────────────

    private record ConversationModel(
        Guid Id,
        List<MessageModel> Messages,
        string Status,
        DateTime StartedAt,
        DateTime? EndedAt,
        string? Title,
        string? SystemPrompt);

    private record MessageModel(
        Guid Id,
        string Role,
        string Text,
        bool HasImage,
        DateTime CreatedAt,
        string? ToolCallId,
        string? ToolName);

    private static ConversationModel ToModel(Conversation c) => new(
        c.Id,
        c.Messages.Select(ToModel).ToList(),
        c.Status.ToString(),
        c.StartedAt,
        c.EndedAt,
        c.Title,
        c.SystemPrompt);

    private static MessageModel ToModel(Message m) => new(
        m.Id,
        m.Role.ToString(),
        m.Content.Text,
        m.Content.HasImage,
        m.CreatedAt,
        m.ToolCallId,
        m.ToolName);

    private static Conversation FromModel(ConversationModel model)
    {
        var messages = model.Messages.Select(FromModel);
        var status = Enum.TryParse<ConversationStatus>(model.Status, out var s)
            ? s
            : ConversationStatus.Active;
        return Conversation.Reconstitute(
            model.Id, messages, status,
            model.StartedAt, model.EndedAt,
            model.Title, model.SystemPrompt);
    }

    private static Message FromModel(MessageModel model)
    {
        var role = Enum.TryParse<MessageRole>(model.Role, out var r)
            ? r
            : MessageRole.User;
        var content = MessageContent.Reconstitute(model.Text, model.HasImage, null);
        return Message.Reconstitute(
            model.Id, role, content,
            model.CreatedAt, model.ToolCallId, model.ToolName);
    }

    // ── Repository methods ──────────────────────────────────────────────────

    public async Task<Conversation?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_currentFilePath)) return null;
        return await ReadFromFileAsync(_currentFilePath, cancellationToken);
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_conversationsFolder, $"{id}.json");
        if (!File.Exists(filePath)) return null;
        return await ReadFromFileAsync(filePath, cancellationToken);
    }

    public async Task SaveAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        try
        {
            var model = ToModel(conversation);
            var json = JsonSerializer.Serialize(model, JsonOptions);
            await File.WriteAllTextAsync(_currentFilePath, json, cancellationToken);

            var archivePath = Path.Combine(_conversationsFolder, $"{conversation.Id}.json");
            await File.WriteAllTextAsync(archivePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save conversation {ConversationId}", conversation.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Conversation>> GetHistoryAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var files = Directory.GetFiles(_conversationsFolder, "*.json")
            .Where(f => !f.EndsWith("current.json"))
            .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
            .Take(limit * 3); // extra para compensar arquivos inválidos

        var conversations = new List<Conversation>();
        foreach (var file in files)
        {
            var conv = await ReadFromFileAsync(file, cancellationToken);
            if (conv is null)
            {
                // arquivo no formato antigo ou corrompido — remove silenciosamente
                TryDeleteFile(file);
                continue;
            }
            conversations.Add(conv);
            if (conversations.Count >= limit) break;
        }
        return conversations;
    }

    private void TryDeleteFile(string path)
    {
        try { File.Delete(path); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not delete stale session file {File}", path); }
    }

    public Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_conversationsFolder, $"{id}.json");
        TryDeleteFile(filePath);

        // Se era a sessão atual, limpa também
        try
        {
            if (File.Exists(_currentFilePath))
            {
                var json = File.ReadAllText(_currentFilePath);
                var model = JsonSerializer.Deserialize<ConversationModel>(json, JsonOptions);
                if (model?.Id == id)
                    File.Delete(_currentFilePath);
            }
        }
        catch { }

        return Task.CompletedTask;
    }

    public Task ClearCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_currentFilePath))
            File.Delete(_currentFilePath);
        return Task.CompletedTask;
    }

    private async Task<Conversation?> ReadFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var model = JsonSerializer.Deserialize<ConversationModel>(json, JsonOptions);
            return model is null ? null : FromModel(model);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read conversation from {File}", filePath);
            return null;
        }
    }
}
