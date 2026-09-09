using Jarvis.Domain.Enums;

namespace Jarvis.Domain.Entities;

public class Conversation
{
    private readonly List<Message> _messages = new();

    public Guid Id { get; private set; }
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();
    public ConversationStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public string? Title { get; private set; }
    public string? SystemPrompt { get; private set; }

    private Conversation() { }

    public Conversation(string? systemPrompt = null)
    {
        Id = Guid.NewGuid();
        Status = ConversationStatus.Active;
        StartedAt = DateTime.UtcNow;
        SystemPrompt = systemPrompt;
    }

    public void AddMessage(Message message)
    {
        if (Status != ConversationStatus.Active)
            throw new InvalidOperationException("Cannot add messages to an inactive conversation.");
        _messages.Add(message);
    }

    public void End()
    {
        Status = ConversationStatus.Ended;
        EndedAt = DateTime.UtcNow;
    }

    public void SetTitle(string title)
    {
        Title = title;
    }

    public string GetSystemPrompt() =>
        SystemPrompt ?? "You are J.A.R.V.I.S., an advanced AI assistant. Be helpful, precise, and concise.";

    public static Conversation Reconstitute(
        Guid id,
        IEnumerable<Message> messages,
        ConversationStatus status,
        DateTime startedAt,
        DateTime? endedAt,
        string? title,
        string? systemPrompt)
    {
        var conv = new Conversation();
        conv.Id = id;
        conv.Status = ConversationStatus.Active;
        conv.StartedAt = startedAt;
        conv.EndedAt = endedAt;
        conv.Title = title;
        conv.SystemPrompt = systemPrompt;
        foreach (var msg in messages)
            conv._messages.Add(msg);
        conv.Status = status;
        return conv;
    }
}