using Jarvis.Application.Abstractions;
using Jarvis.Domain.ValueObjects;
using Jarvis.Infrastructure.Configuration;
using Jarvis.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;

namespace Jarvis.Voice.SpeechToText;

public class WindowsSpeechToTextProvider : ISpeechToTextProvider, IDisposable
{
    private readonly VoiceSettings _settings;
    private readonly ILogger<WindowsSpeechToTextProvider> _logger;
    private WhisperFactory? _whisperFactory;
    private bool _modelLoaded;

    public string Name => "Windows";

    public static IReadOnlyList<string> GetAvailableMicrophones()
    {
        var mics = new List<string>();
        for (int i = 0; i < WaveIn.DeviceCount; i++)
            mics.Add(WaveIn.GetCapabilities(i).ProductName);
        return mics;
    }

    private string ModelPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Jarvis", $"ggml-{_settings.WhisperModel.ToLower().Split(' ')[0]}.bin");

    private GgmlType GgmlType => _settings.WhisperModel.ToLower() switch
    {
        var m when m.StartsWith("tiny")   => GgmlType.Tiny,
        var m when m.StartsWith("base")   => GgmlType.Base,
        var m when m.StartsWith("medium") => GgmlType.Medium,
        var m when m.StartsWith("large")  => GgmlType.LargeV3,
        _                                  => GgmlType.Small
    };

    public WindowsSpeechToTextProvider(IOptions<JarvisSettings> settings, ILogger<WindowsSpeechToTextProvider> logger)
    {
        _settings = settings.Value.Voice;
        _logger = logger;
        _ = EnsureModelAsync();
    }

    private async Task EnsureModelAsync()
    {
        try
        {
            // Remove modelo tiny antigo se existir
            if (!File.Exists(ModelPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ModelPath)!);
                _logger.LogInformation("Baixando modelo Whisper {Model}...", _settings.WhisperModel);
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType);
                using var fileStream = File.Create(ModelPath);
                await modelStream.CopyToAsync(fileStream);
                _logger.LogInformation("Modelo Whisper baixado em {Path}", ModelPath);
            }

            _whisperFactory = WhisperFactory.FromPath(ModelPath);
            _modelLoaded = true;
            _logger.LogInformation("Whisper pronto para português.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao carregar modelo Whisper");
        }
    }

    public async Task<Result<string>> TranscribeAudioAsync(AudioData audio, CancellationToken cancellationToken = default)
    {
        var text = await TranscribeWavAsync(audio.Data, cancellationToken);
        return string.IsNullOrWhiteSpace(text)
            ? Result.Failure<string>("Nenhuma fala detectada.")
            : Result.Success(text);
    }

    public async IAsyncEnumerable<string> StreamTranscriptionAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var wavData = await RecordUntilSilenceAsync(cancellationToken);
            if (wavData is null || cancellationToken.IsCancellationRequested)
                yield break;

            var text = await TranscribeWavAsync(wavData, cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
                yield return text;
        }
    }

    private async Task<byte[]?> RecordUntilSilenceAsync(CancellationToken cancellationToken)
    {
        var deviceIndex = GetDeviceIndex(_settings.SelectedMicrophone);
        _logger.LogInformation("Gravando microfone (device {Index}, nome='{Name}')...", deviceIndex, _settings.SelectedMicrophone);

        var format = new WaveFormat(16000, 1);

        using var ms = new MemoryStream();
        using var writer = new WaveFileWriter(ms, format);
        using var waveIn = new WaveInEvent { DeviceNumber = deviceIndex, WaveFormat = format };

        var silenceStart = DateTime.UtcNow;
        var hasSound = false;
        const int silenceThreshold = 300; // reduzido para detectar vozes mais suaves

        waveIn.DataAvailable += (_, e) =>
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
            var maxSample = 0;
            for (var i = 0; i + 1 < e.BytesRecorded; i += 2)
                maxSample = Math.Max(maxSample, Math.Abs((int)BitConverter.ToInt16(e.Buffer, i)));

            if (maxSample > silenceThreshold)
            {
                hasSound = true;
                silenceStart = DateTime.UtcNow;
            }
        };

        try
        {
            waveIn.StartRecording();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao iniciar gravação no device {Index}", deviceIndex);
            return null;
        }

        var speechTimeout = DateTime.UtcNow.AddSeconds(10);
        while (!hasSound && DateTime.UtcNow < speechTimeout && !cancellationToken.IsCancellationRequested)
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);

        if (!hasSound)
        {
            waveIn.StopRecording();
            return null;
        }

        var maxDuration = DateTime.UtcNow.AddSeconds(30);
        while ((DateTime.UtcNow - silenceStart).TotalSeconds < 1.5 &&
               DateTime.UtcNow < maxDuration &&
               !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        waveIn.StopRecording();
        await writer.FlushAsync(cancellationToken);
        return ms.ToArray();
    }

    private async Task<string> TranscribeWavAsync(byte[] wavData, CancellationToken cancellationToken)
    {
        if (!_modelLoaded || _whisperFactory is null)
        {
            _logger.LogWarning("Modelo Whisper ainda não carregado.");
            return string.Empty;
        }

        try
        {
            using var processor = _whisperFactory.CreateBuilder()
                .WithLanguage("pt")
                .Build();

            using var ms = new MemoryStream(wavData);
            var sb = new System.Text.StringBuilder();
            await foreach (var segment in processor.ProcessAsync(ms, cancellationToken))
                sb.Append(segment.Text);

            return StripWhisperNoiseTags(sb.ToString().Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na transcrição Whisper");
            return string.Empty;
        }
    }

    // Remove tags de ruído/ambiente do Whisper: [Música], [Ruído], [Aplausos], etc.
    private static string StripWhisperNoiseTags(string text)
        => System.Text.RegularExpressions.Regex.Replace(text, @"\[.*?\]", "").Trim();

    private static int GetDeviceIndex(string microphoneName)
    {
        if (string.IsNullOrEmpty(microphoneName)) return 0;
        for (var i = 0; i < WaveIn.DeviceCount; i++)
            if (WaveIn.GetCapabilities(i).ProductName.Contains(microphoneName, StringComparison.OrdinalIgnoreCase))
                return i;
        return 0;
    }

    public void Dispose()
    {
        _whisperFactory?.Dispose();
    }
}
