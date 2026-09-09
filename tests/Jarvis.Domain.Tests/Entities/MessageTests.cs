using FluentAssertions;
using Jarvis.Domain.Entities;
using Jarvis.Domain.Enums;
using Xunit;

namespace Jarvis.Domain.Tests.Entities;

public class MessageTests
{
    [Fact]
    public void CreateUserMessage_ShouldSetUserRole()
    {
        var message = Message.CreateUserMessage("Hello");
        message.Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void CreateUserMessage_ShouldSetContent()
    {
        var message = Message.CreateUserMessage("Hello World");
        message.Content.Text.Should().Be("Hello World");
    }

    [Fact]
    public void CreateAssistantMessage_ShouldSetAssistantRole()
    {
        var message = Message.CreateAssistantMessage("Response");
        message.Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void CreateSystemMessage_ShouldSetSystemRole()
    {
        var message = Message.CreateSystemMessage("System instructions");
        message.Role.Should().Be(MessageRole.System);
    }

    [Fact]
    public void CreateToolResultMessage_ShouldSetToolRole()
    {
        var message = Message.CreateToolResultMessage("call123", "my_tool", "result");
        message.Role.Should().Be(MessageRole.Tool);
    }

    [Fact]
    public void CreateToolResultMessage_ShouldSetToolCallId()
    {
        var message = Message.CreateToolResultMessage("call123", "my_tool", "result");
        message.ToolCallId.Should().Be("call123");
    }

    [Fact]
    public void CreateToolResultMessage_ShouldSetToolName()
    {
        var message = Message.CreateToolResultMessage("call123", "my_tool", "result");
        message.ToolName.Should().Be("my_tool");
    }

    [Fact]
    public void NewMessage_ShouldHaveNonEmptyId()
    {
        var message = Message.CreateUserMessage("Test");
        message.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void NewMessage_ShouldHaveCreatedAtSet()
    {
        var message = Message.CreateUserMessage("Test");
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
