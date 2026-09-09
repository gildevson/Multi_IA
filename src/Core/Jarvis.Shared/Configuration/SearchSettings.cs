namespace Jarvis.Shared.Configuration;

public class SearchSettings
{
    public string Provider { get; set; } = "DuckDuckGo";
    public string BraveApiKey { get; set; } = string.Empty;
    public string GoogleApiKey { get; set; } = string.Empty;
    public string GoogleSearchEngineId { get; set; } = string.Empty;
}
