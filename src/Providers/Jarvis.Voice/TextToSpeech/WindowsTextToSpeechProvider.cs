using Jarvis.Application.Abstractions;
using Jarvis.Domain.ValueObjects;
using Jarvis.Infrastructure.Configuration;
using Jarvis.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using System.Speech.Synthesis;

namespace Jarvis.Voice.TextToSpeech;

public class WindowsTextToSpeechProvider : ITextToSpeechProvider, IDisposable
{
    private readonly JarvisSettings _settings;
    private readonly SpeechSynthesizer _synthesizer;
    private readonly ILogger<WindowsTextToSpeechProvider> _logger;
    private WaveOutEvent? _currentOutput;
    private volatile bool _isSpeaking;

    public string Name => "Windows";
    public bool IsSpeaking => _isSpeaking;

    private VoiceSettings VoiceSettings => _settings.Voice;

    public WindowsTextToSpeechProvider(IOptions<JarvisSettings> options, ILogger<WindowsTextToSpeechProvider> logger)
    {
        _settings = options.Value;
        _synthesizer = new SpeechSynthesizer();
        _logger = logger;
    }

    private void ApplyVoiceSettings()
    {
        var settings = VoiceSettings;
        _synthesizer.Rate = (int)(settings.SpeechRate * 2 - 2);
        if (!string.IsNullOrEmpty(settings.VoiceName))
        {
            try { _synthesizer.SelectVoice(settings.VoiceName); }
            catch (Exception ex) { _logger.LogWarning(ex, "Voz '{Voice}' não encontrada", settings.VoiceName); }
        }
        else if (!string.IsNullOrEmpty(settings.Language))
        {
            try { _synthesizer.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new System.Globalization.CultureInfo(settings.Language)); }
            catch { }
        }
    }

    public Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        _isSpeaking = true;
        _ = Task.Run(() =>
        {
            try
            {
                // Sintetiza para bytes em memória
                ApplyVoiceSettings();
                using var ms = new MemoryStream();
                _synthesizer.SetOutputToWaveStream(ms);
                _synthesizer.Speak(text);
                _synthesizer.SetOutputToDefaultAudioDevice();
                ms.Position = 0;

                if (!_isSpeaking) return; // parado antes de tocar

                // Toca com NAudio — stop confiável
                using var audioFile = new WaveFileReader(ms);
                using var output = new WaveOutEvent { DeviceNumber = AudioDeviceHelper.GetDeviceNumber(VoiceSettings.AudioOutputDevice) };
                _currentOutput = output;

                var finished = new ManualResetEventSlim(false);
                output.PlaybackStopped += (_, _) => finished.Set();
                output.Init(audioFile);
                output.Play();
                finished.Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Windows TTS playback error");
            }
            finally
            {
                _currentOutput = null;
                _isSpeaking = false;
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    public Task<Result<AudioData>> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyVoiceSettings();
            using var ms = new MemoryStream();
            _synthesizer.SetOutputToWaveStream(ms);
            _synthesizer.Speak(text);
            _synthesizer.SetOutputToDefaultAudioDevice();
            return Task.FromResult(Result.Success(AudioData.FromBytes(ms.ToArray())));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windows TTS synthesis failed");
            return Task.FromResult(Result.Failure<AudioData>($"TTS error: {ex.Message}"));
        }
    }

    public void StopSpeaking()
    {
        _isSpeaking = false;
        var output = _currentOutput;
        _currentOutput = null;
        output?.Stop();
    }

    public void Dispose()
    {
        _currentOutput?.Stop();
        _synthesizer?.Dispose();
    }
}
