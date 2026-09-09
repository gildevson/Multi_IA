using Jarvis.Application.Abstractions;
using Jarvis.Domain.ValueObjects;
using Jarvis.Infrastructure.Configuration;
using Jarvis.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Jarvis.Voice.TextToSpeech;

public class PiperTextToSpeechProvider : ITextToSpeechProvider
{
    private readonly JarvisSettings _settings;
    private readonly ILogger<PiperTextToSpeechProvider> _logger;
    private WaveOutEvent? _currentOutput;
    private volatile bool _isSpeaking;

    public string Name => "Piper";
    public bool IsSpeaking => _isSpeaking;

    private VoiceSettings VoiceSettings => _settings.Voice;

    public PiperTextToSpeechProvider(IOptions<JarvisSettings> options, ILogger<PiperTextToSpeechProvider> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    private static string StripMarkdown(string text)
    {
        text = Regex.Replace(text, @"```[\s\S]*?```", "");
        text = Regex.Replace(text, @"\*{1,3}(.+?)\*{1,3}", "$1");
        text = Regex.Replace(text, @"_{1,3}(.+?)_{1,3}", "$1");
        text = Regex.Replace(text, @"^#{1,6}\s+", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"\[(.+?)\]\(.+?\)", "$1");
        text = Regex.Replace(text, @"`(.+?)`", "$1");
        text = Regex.Replace(text, @"^[-*_]{3,}$", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^\s*[-*+]\s+", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^\s*\d+\.\s+", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"[\*_~>|#]", "");
        text = Regex.Replace(text, @"\n{3,}", "\n\n").Trim();
        return text;
    }

    public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        text = StripMarkdown(text);
        var result = await SynthesizeAsync(text, cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("Piper synthesis failed: {Error}", result.Error);
            return;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"jarvis_piper_{Guid.NewGuid()}.wav");
        await File.WriteAllBytesAsync(tempFile, result.Value.Data, cancellationToken);

        _isSpeaking = true;
        _ = Task.Run(() =>
        {
            string? fileToDelete = tempFile;
            try
            {
                using var audioFile = new WaveFileReader(tempFile);
                using var output = new WaveOutEvent { DeviceNumber = AudioDeviceHelper.GetDeviceNumber(VoiceSettings.AudioOutputDevice) };
                _currentOutput = output;

                var finished = new ManualResetEventSlim(false);
                output.PlaybackStopped += (_, _) => finished.Set();
                output.Init(audioFile);
                output.Play();

                finished.Wait(); // espera terminar ou Stop() ser chamado
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Piper playback error");
            }
            finally
            {
                _currentOutput = null;
                _isSpeaking = false;
                if (fileToDelete is not null && File.Exists(fileToDelete))
                    try { File.Delete(fileToDelete); } catch { }
            }
        }, CancellationToken.None);
    }

    public void StopSpeaking()
    {
        var output = _currentOutput;
        _currentOutput = null;
        _isSpeaking = false;
        output?.Stop();
    }

    public async Task<Result<AudioData>> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var piperExe = Path.GetFullPath(VoiceSettings.PiperExePath);
            var modelPath = Path.GetFullPath(VoiceSettings.PiperModelPath);

            if (!File.Exists(piperExe))
                return Result.Failure<AudioData>($"Piper não encontrado em: {piperExe}");

            if (!File.Exists(modelPath))
                return Result.Failure<AudioData>($"Modelo não encontrado em: {modelPath}");

            var tempOutput = Path.Combine(Path.GetTempPath(), $"jarvis_piper_{Guid.NewGuid()}.wav");

            var psi = new ProcessStartInfo
            {
                FileName = piperExe,
                Arguments = $"--model \"{modelPath}\" --output_file \"{tempOutput}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            await process.StandardInput.WriteLineAsync(text);
            process.StandardInput.Close();

            await process.WaitForExitAsync(cancellationToken);

            if (!File.Exists(tempOutput))
                return Result.Failure<AudioData>("Piper não gerou o arquivo de áudio.");

            var bytes = await File.ReadAllBytesAsync(tempOutput, cancellationToken);
            File.Delete(tempOutput);

            _logger.LogDebug("Piper gerou {Bytes} bytes de áudio", bytes.Length);
            return Result.Success(AudioData.FromBytes(bytes, "wav"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Piper TTS failed");
            return Result.Failure<AudioData>($"Piper error: {ex.Message}");
        }
    }
}
