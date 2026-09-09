namespace Jarvis.Infrastructure.Configuration;

public class AISettings
{
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string OpenAIModel { get; set; } = "gpt-4o";
    public string ClaudeApiKey { get; set; } = string.Empty;
    public string ClaudeModel { get; set; } = "claude-opus-4-6";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "llama3.2";
    public string OllamaVisionModel { get; set; } = "llava";
    public string OllamaPdfModel { get; set; } = string.Empty;
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiModel { get; set; } = "gemini-2.0-flash";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
    public string SystemPrompt { get; set; } = Jarvis.Shared.Constants.JarvisConstants.SystemPromptDefault;
    public string VisionProvider { get; set; } = string.Empty;
    public string GeminiVisionModel { get; set; } = "gemini-2.0-flash";
    public string ClaudeVisionModel { get; set; } = "claude-haiku-4-5-20251001";
    public string OpenAIVisionModel { get; set; } = "gpt-4o";
}