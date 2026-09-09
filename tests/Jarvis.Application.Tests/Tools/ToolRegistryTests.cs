using FluentAssertions;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Tools;
using Jarvis.Shared.Models;
using Moq;
using Xunit;

namespace Jarvis.Application.Tests.Tools;

// In-memory implementation of IToolRegistry for testing
public class InMemoryToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ITool tool)
    {
        if (_tools.ContainsKey(tool.Name))
            throw new InvalidOperationException($"Tool '{tool.Name}' is already registered.");
        _tools[tool.Name] = tool;
    }

    public ITool? GetByName(string name) => _tools.TryGetValue(name, out var t) ? t : null;
    public IEnumerable<ToolDefinition> GetAllDefinitions() => _tools.Values.Select(t => t.GetDefinition());
    public IEnumerable<ITool> GetAll() => _tools.Values;
    public bool Exists(string name) => _tools.ContainsKey(name);
}

public class ToolRegistryTests
{
    private readonly InMemoryToolRegistry _sut = new();

    private static Mock<ITool> CreateMockTool(string name, string description = "Test tool")
    {
        var mock = new Mock<ITool>();
        mock.Setup(t => t.Name).Returns(name);
        mock.Setup(t => t.Description).Returns(description);
        mock.Setup(t => t.GetDefinition()).Returns(new ToolDefinition
        {
            Name = name,
            Description = description,
            Parameters = new List<ToolParameter>()
        });
        return mock;
    }

    [Fact]
    public void Register_ShouldAddTool()
    {
        var tool = CreateMockTool("my_tool");
        _sut.Register(tool.Object);
        _sut.Exists("my_tool").Should().BeTrue();
    }

    [Fact]
    public void Register_DuplicateName_ShouldThrow()
    {
        var tool1 = CreateMockTool("dupe_tool");
        var tool2 = CreateMockTool("dupe_tool");
        _sut.Register(tool1.Object);

        var act = () => _sut.Register(tool2.Object);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetByName_WithRegisteredTool_ShouldReturnTool()
    {
        var tool = CreateMockTool("find_me");
        _sut.Register(tool.Object);

        var result = _sut.GetByName("find_me");
        result.Should().NotBeNull();
        result!.Name.Should().Be("find_me");
    }

    [Fact]
    public void GetByName_WithUnregisteredTool_ShouldReturnNull()
    {
        var result = _sut.GetByName("ghost_tool");
        result.Should().BeNull();
    }

    [Fact]
    public void GetByName_IsCaseInsensitive()
    {
        var tool = CreateMockTool("my_tool");
        _sut.Register(tool.Object);

        _sut.GetByName("MY_TOOL").Should().NotBeNull();
        _sut.GetByName("My_Tool").Should().NotBeNull();
    }

    [Fact]
    public void GetAllDefinitions_ShouldReturnAllRegistered()
    {
        _sut.Register(CreateMockTool("tool_a").Object);
        _sut.Register(CreateMockTool("tool_b").Object);
        _sut.Register(CreateMockTool("tool_c").Object);

        _sut.GetAllDefinitions().Should().HaveCount(3);
    }

    [Fact]
    public void GetAll_ShouldReturnAllTools()
    {
        _sut.Register(CreateMockTool("tool_x").Object);
        _sut.Register(CreateMockTool("tool_y").Object);

        _sut.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void Exists_WithRegisteredTool_ShouldReturnTrue()
    {
        _sut.Register(CreateMockTool("exists_tool").Object);
        _sut.Exists("exists_tool").Should().BeTrue();
    }

    [Fact]
    public void Exists_WithUnregisteredTool_ShouldReturnFalse()
    {
        _sut.Exists("no_such_tool").Should().BeFalse();
    }
}
