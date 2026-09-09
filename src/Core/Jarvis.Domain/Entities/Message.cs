using Jarvis.Domain.Enums;
using Jarvis.Domain.ValueObjects;

namespace Jarvis.Domain.Entities;

public class Message
{
    public Guid Id { get; private set; }
    public MessageRole Role { get; private set; }
    public MessageContent Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? ToolCallId { get; private set; }
    public string? ToolName { get; private set; }

    private Message() 
    { 
        Content = MessageContent.CreateText(string.Empty);
    }

    private Message(Guid id, MessageRole role, MessageContent content, string? toolCallId = null, string? toolName = null)
    {
        Id = id;
        Role = role;
        Content = content;
        CreatedAt = DateTime.UtcNow;
        ToolCallId = toolCallId;
        ToolName = toolName;
    }

    public static Message CreateUserMessage(string text)
        => new(Guid.NewGuid(), MessageRole.User, MessageContent.CreateText(text));

    public static Message CreateUserMessageWithImage(string text, ImageData image)
        => new(Guid.NewGuid(), MessageRole.User, MessageContent.CreateWithImage(text, image));

    public static Message CreateAssistantMessage(string text)
        => new(Guid.NewGuid(), MessageRole.Assistant, MessageContent.CreateText(text));

    public static Message CreateSystemMessage(string text)
        => new(Guid.NewGuid(), MessageRole.System, MessageContent.CreateText(text));

    public static Message CreateToolResultMessage(string toolCallId, string toolName, string result)
        => new(Guid.NewGuid(), MessageRole.Tool, MessageContent.CreateText(result), toolCallId, toolName);

    public static Message Reconstitute(Guid id, MessageRole role, MessageContent content, DateTime createdAt, string? toolCallId, string? toolName)
    {
        var msg = new Message();
        msg.Id = id;
        msg.Role = role;
        msg.Content = content;
        msg.CreatedAt = createdAt;
        msg.ToolCallId = toolCallId;
        msg.ToolName = toolName;
        return msg;
    }
}