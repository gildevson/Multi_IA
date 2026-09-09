using System.Text.Json;

namespace Jarvis.Application.Tools;

public class ToolDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<ToolParameter> Parameters { get; init; } = new();

    public Dictionary<string, object> ToOpenAIFunction()
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in Parameters)
        {
            properties[param.Name] = new Dictionary<string, object>
            {
                ["type"] = param.Type,
                ["description"] = param.Description
            };

            if (param.Required)
                required.Add(param.Name);
        }

        return new Dictionary<string, object>
        {
            ["type"] = "function",
            ["function"] = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = properties,
                    ["required"] = required
                }
            }
        };
    }
}