using Jarvis.Application.Abstractions;
using Jarvis.Domain.ValueObjects;
using Jarvis.Infrastructure.Configuration;
using Jarvis.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Jarvis.Voice.SpeechToText;

public class WhisperSpeechToTextProvider : ISpeechToTextProvider
{
    private readonly AISettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhisperSpeechToTextProvider> _logger;

    public string Name => "Whisper";

    public WhisperSpeechToTextProvider(IOptions<JarvisSettings> settings, IHttpClientFactory httpClientFactory, ILogger<WhisperSpeechToTextProvider> logger)
    {
        _settings = settings.Value.AI;
        _httpClient = httpClientFactory.CreateClient("Whisper");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.OpenAIApiKey);
        _logger = logger;
    }

    public async Task<Result<string>> TranscribeAudioAsync(AudioData audio, CancellationToken cancellationToken = default)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            using var audioContent = new ByteArrayContent(audio.Data);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(audioContent, "file", $"audio.{audio.Format}");
            form.Add(new StringContent("whisper-1"), "model");

            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/audio/transcriptions", form, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);
            var text = doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
            return Result.Success(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Whisper transcription failed");
            return Result.Failure<string>($"Whisper error: {ex.Message}");
        }
    }

    public async IAsyncEnumerable<string> StreamTranscriptionAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Whisper doesn't support real-time streaming; delegate to Windows STT for live capture
        _logger.LogWarning("Whisper does not support real-time streaming. Use Windows STT for live capture.");
        yield break;
    }
}
