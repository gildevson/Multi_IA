using FluentAssertions;
using Jarvis.Application.Abstractions;
using Xunit;
using Jarvis.Application.Services;
using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jarvis.Application.Tests.Services;

public class ToolDispatcherTests
{
    private readonly Mock<IToolRegistry> _registryMock = new();
    private readonly Mock<ILogger<ToolDispatcher>> _loggerMock = new();
    private readonly ToolDispatcher _sut;

    public ToolDispatcherTests()
    {
        _sut = new ToolDispatcher(_registryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DispatchAsync_WithValidTool_ShouldExecuteAndReturnResult()
    {
        var toolMock = new Mock<ITool>();
        toolMock.Setup(t => t.Name).Returns("test_tool");
        toolMock.Setup(t => t.ExecuteAsync(It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ToolResult.Ok("success")));

        _registryMock.Setup(r => r.GetByName("test_tool")).Returns(toolMock.Object);

        var result = await _sut.DispatchAsync("test_tool", new Dictionary<string, object>());

        result.IsSuccess.Should().BeTrue();
        result.Value.Output.Should().Be("success");
    }

    [Fact]
    public async Task DispatchAsync_WithInvalidToolName_ShouldReturnFailure()
    {
        _registryMock.Setup(r => r.GetByName(It.IsAny<string>())).Returns((ITool?)null);

        var result = await _sut.DispatchAsync("nonexistent_tool", new Dictionary<string, object>());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("nonexistent_tool");
    }

    [Fact]
    public async Task DispatchAsync_WhenToolThrows_ShouldReturnFailure()
    {
        var toolMock = new Mock<ITool>();
        toolMock.Setup(t => t.Name).Returns("broken_tool");
        toolMock.Setup(t => t.ExecuteAsync(It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Tool exploded"));

        _registryMock.Setup(r => r.GetByName("broken_tool")).Returns(toolMock.Object);

        var result = await _sut.DispatchAsync("broken_tool", new Dictionary<string, object>());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exception");
    }
}
