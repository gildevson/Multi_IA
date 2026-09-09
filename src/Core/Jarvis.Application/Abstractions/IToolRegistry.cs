using Jarvis.Application.Tools;

namespace Jarvis.Application.Abstractions;

public interface IToolRegistry
{
    void Register(ITool tool);
    ITool? GetByName(string name);
    IEnumerable<ToolDefinition> GetAllDefinitions();
    IEnumerable<ITool> GetAll();
    bool Exists(string name);
}