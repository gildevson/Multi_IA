namespace Jarvis.Shared.Constants;

public static class JarvisConstants
{
    public const string DefaultAIProvider = "OpenAI";
    public const string DefaultSTTProvider = "Windows";
    public const string DefaultTTSProvider = "Windows";
    public const string SettingsFileName = "settings.json";
    public const string ConversationsFolder = "conversations";
    public const string LogsFolder = "logs";

    public const string SystemPromptDefault =
        "You are J.A.R.V.I.S. (Just A Rather Very Intelligent System), " +
        "an advanced AI assistant inspired by Tony Stark'\''s AI. " +
        "You are helpful, precise, and slightly formal but with subtle wit. " +
        "You have access to various tools to help the user with tasks on their computer. " +
        "Always be concise and direct in your responses. " +
        "When using tools, explain what you are doing briefly.";

    public static class ProviderNames
    {
        public const string OpenAI = "OpenAI";
        public const string Claude = "Claude";
        public const string Ollama = "Ollama";
        public const string Gemini = "Gemini";
        public const string Windows = "Windows";
        public const string Whisper = "Whisper";
        public const string ElevenLabs = "ElevenLabs";
    }

    public static class ToolNames
    {
        public const string OpenApplication = "open_application";
        public const string ReadFile = "read_file";
        public const string WriteFile = "write_file";
        public const string ExecutePowerShell = "execute_powershell";
        public const string OpenChrome = "open_chrome";
        public const string WebSearch = "web_search";
        public const string OpenVisualStudio = "open_visual_studio";
        public const string GitCommand = "git_command";
        public const string CaptureScreen = "capture_screen";
    }
}