using FluentAssertions;
using Jarvis.Domain.Entities;
using Jarvis.Domain.Enums;
using Xunit;

namespace Jarvis.Domain.Tests.Entities;

public class ConversationTests
{
    [Fact]
    public void CreateConversation_ShouldHaveEmptyMessages()
    {
        var conversation = new Conversation();
        conversation.Messages.Should().BeEmpty();
    }

    [Fact]
    public void CreateConversation_ShouldHaveActiveStatus()
    {
        var conversation = new Conversation();
        conversation.Status.Should().Be(ConversationStatus.Active);
    }

    [Fact]
    public void AddMessage_ShouldAddToCollection()
    {
        var conversation = new Conversation();
        var message = Message.CreateUserMessage("Hello");

        conversation.AddMessage(message);

        conversation.Messages.Should().HaveCount(1);
        conversation.Messages[0].Should().Be(message);
    }

    [Fact]
    public void AddMessage_UserMessage_ShouldHaveUserRole()
    {
        var conversation = new Conversation();
        var message = Message.CreateUserMessage("Test");

        conversation.AddMessage(message);

        conversation.Messages[0].Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void AddMessage_AssistantMessage_ShouldHaveAssistantRole()
    {
        var conversation = new Conversation();
        var message = Message.CreateAssistantMessage("Response");

        conversation.AddMessage(message);

        conversation.Messages[0].Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void End_ShouldSetStatusToEnded()
    {
        var conversation = new Conversation();
        conversation.End();
        conversation.Status.Should().Be(ConversationStatus.Ended);
    }

    [Fact]
    public void End_ShouldSetEndedAt()
    {
        var conversation = new Conversation();
        conversation.End();
        conversation.EndedAt.Should().NotBeNull();
        conversation.EndedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetTitle_ShouldUpdateTitle()
    {
        var conversation = new Conversation();
        conversation.SetTitle("My Conversation");
        conversation.Title.Should().Be("My Conversation");
    }

    [Fact]
    public void GetSystemPrompt_WithCustomPrompt_ShouldReturnCustom()
    {
        var conversation = new Conversation("Custom prompt");
        conversation.GetSystemPrompt().Should().Be("Custom prompt");
    }

    [Fact]
    public void GetSystemPrompt_WithoutCustomPrompt_ShouldReturnDefault()
    {
        var conversation = new Conversation();
        conversation.GetSystemPrompt().Should().NotBeNullOrEmpty();
        conversation.GetSystemPrompt().Should().Contain("J.A.R.V.I.S.");
    }

    [Fact]
    public void AddMessage_AfterEnd_ShouldThrowException()
    {
        var conversation = new Conversation();
        conversation.End();

        var act = () => conversation.AddMessage(Message.CreateUserMessage("Late message"));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddMultipleMessages_ShouldPreserveOrder()
    {
        var conversation = new Conversation();
        var msg1 = Message.CreateUserMessage("First");
        var msg2 = Message.CreateAssistantMessage("Second");
        var msg3 = Message.CreateUserMessage("Third");

        conversation.AddMessage(msg1);
        conversation.AddMessage(msg2);
        conversation.AddMessage(msg3);

        conversation.Messages.Should().HaveCount(3);
        conversation.Messages[0].Content.Text.Should().Be("First");
        conversation.Messages[2].Content.Text.Should().Be("Third");
    }
}
