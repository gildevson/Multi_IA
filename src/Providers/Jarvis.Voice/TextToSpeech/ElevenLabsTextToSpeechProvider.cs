using Jarvis.Application.Abstractions;
using Jarvis.Domain.ValueObjects;
using Jarvis.Infrastructure.Configuration;
using Jarvis.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Jarvis.Voice.TextToSpeech;

public class ElevenLabsTextToSpeechProvider : ITextToSpeechProvider
{
    private readonly VoiceSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ElevenLabsTextToSpeechProvider> _logger;

    public string Name => "ElevenLabs";
    public bool IsSpeaking => false; // ElevenLabs plays via shell process; stop not supported

    public ElevenLabsTextToSpeechProvider(IOptions<JarvisSettings> settings, IHttpClientFactory httpClientFactory, ILogger<ElevenLabsTextToSpeechProvider> logger)
    {
        _settings = settings.Value.Voice;
        _httpClient = httpClientFactory.CreateClient("ElevenLabs");
        _httpClient.DefaultRequestHeaders.Add("xi-api-key", _settings.ElevenLabsApiKey);
        _logger = logger;
    }

    public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await SynthesizeAsync(text, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("ElevenLabs synthesis failed: {Error}", result.Error);
            return;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"jarvis_{Guid.NewGuid()}.mp3");
        await File.WriteAllBytesAsync(tempFile, result.Value.Data, cancellationToken);

        try
        {
            using var process = Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
            process?.WaitForExit(10000);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public void StopSpeaking() { /* ElevenLabs plays via shell process */ }

    public async Task<Result<AudioData>> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.elevenlabs.io/v1/text-to-speech/{_settings.ElevenLabsVoiceId}";
            var payload = new
            {
                text,
                model_id = "eleven_monolingual_v1",
                voice_settings = new { stability = 0.5, similarity_boost = 0.75 }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var audioBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return Result.Success(AudioData.FromBytes(audioBytes, "mp3"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ElevenLabs API failed");
            return Result.Failure<AudioData>($"ElevenLabs error: {ex.Message}");
        }
    }
}
