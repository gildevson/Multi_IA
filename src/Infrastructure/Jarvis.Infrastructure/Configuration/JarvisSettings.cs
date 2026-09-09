using Jarvis.Shared.Configuration;

namespace Jarvis.Infrastructure.Configuration;

public class JarvisSettings
{
    public const string SectionName = "JarvisSettings";

    public string AIProvider { get; set; } = "OpenAI";
    public string STTProvider { get; set; } = "Windows";
    public string TTSProvider { get; set; } = "Windows";
    public string DataFolder { get; set; } = "Data";
    public bool EnableVoiceByDefault { get; set; } = false;
    public string UserName { get; set; } = string.Empty;
    public AISettings AI { get; set; } = new();
    public VoiceSettings Voice { get; set; } = new();
    public VisionSettings Vision { get; set; } = new();
    public SearchSettings Search { get; set; } = new();
    public string ChatBackgroundImagePath { get; set; } = string.Empty;
    public double ChatBackgroundOpacity { get; set; } = 0.25;

    public string GetEffectiveSystemPrompt()
    {
        var prefix = new System.Text.StringBuilder();

        // Idioma — sempre no topo para modelos como qwen que ignoram o idioma da pergunta
        var lang = Voice.Language;
        if (!string.IsNullOrEmpty(lang))
        {
            var langName = lang switch
            {
                "pt-BR" => "português do Brasil",
                "en-US" => "English",
                "es-ES" => "español",
                "fr-FR" => "français",
                "de-DE" => "Deutsch",
                "it-IT" => "italiano",
                "ja-JP" => "日本語",
                "zh-CN" => "中文",
                _ => lang
            };
            prefix.AppendLine($"REGRA ABSOLUTA: Responda SEMPRE em {langName} ({lang}), independente do idioma da pergunta. NUNCA use outro idioma.");
            prefix.AppendLine();
        }

        // Nome do usuário
        if (!string.IsNullOrEmpty(UserName))
        {
            prefix.AppendLine($"INFORMAÇÃO OBRIGATÓRIA: O nome do usuário é {UserName}. " +
                              $"Quando perguntado 'qual é o meu nome' ou 'como me chamo', SEMPRE responda '{UserName}'. " +
                              $"Use o nome {UserName} ao se dirigir ao usuário.");
            prefix.AppendLine();
        }

        return prefix.Length > 0
            ? prefix.ToString() + AI.SystemPrompt
            : AI.SystemPrompt;
    }
}