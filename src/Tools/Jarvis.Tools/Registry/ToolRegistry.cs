using Jarvis.Application.Abstractions;
using Jarvis.Application.Tools;

namespace Jarvis.Tools.Registry;

public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ITool tool)
    {
        if (_tools.ContainsKey(tool.Name))
            throw new InvalidOperationException($"Tool '{tool.Name}' is already registered.");
        _tools[tool.Name] = tool;
    }

    public ITool? GetByName(string name)
        => _tools.TryGetValue(name, out var tool) ? tool : null;

    public IEnumerable<ToolDefinition> GetAllDefinitions()
        => _tools.Values.Select(t => t.GetDefinition());

    public IEnumerable<ITool> GetAll()
        => _tools.Values;

    public bool Exists(string name)
        => _tools.ContainsKey(name);
}
