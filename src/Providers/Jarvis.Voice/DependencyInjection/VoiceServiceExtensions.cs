using Jarvis.Application.Abstractions;
using Jarvis.Infrastructure.Configuration;
using Jarvis.Voice.SpeechToText;
using Jarvis.Voice.TextToSpeech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jarvis.Voice.DependencyInjection;

public static class VoiceServiceExtensions
{
    public static IServiceCollection AddVoiceProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("Whisper");
        services.AddHttpClient("ElevenLabs");

        services.AddSingleton<WindowsSpeechToTextProvider>();
        services.AddSingleton<WhisperSpeechToTextProvider>();
        services.AddSingleton<WindowsTextToSpeechProvider>();
        services.AddSingleton<ElevenLabsTextToSpeechProvider>();
        services.AddSingleton<PiperTextToSpeechProvider>();

        services.AddSingleton<ISpeechToTextProvider>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<JarvisSettings>>().Value;
            return settings.Voice.STTProvider switch
            {
                "Whisper" => sp.GetRequiredService<WhisperSpeechToTextProvider>(),
                _ => sp.GetRequiredService<WindowsSpeechToTextProvider>()
            };
        });

        services.AddSingleton<ITextToSpeechProvider>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<JarvisSettings>>().Value;
            return settings.Voice.TTSProvider switch
            {
                "ElevenLabs" => sp.GetRequiredService<ElevenLabsTextToSpeechProvider>(),
                "Piper" => sp.GetRequiredService<PiperTextToSpeechProvider>(),
                _ => sp.GetRequiredService<WindowsTextToSpeechProvider>()
            };
        });

        return services;
    }
}
