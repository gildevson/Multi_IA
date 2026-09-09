namespace Jarvis.Infrastructure.Configuration;

public class VoiceSettings
{
    public string STTProvider { get; set; } = "Windows";
    public string TTSProvider { get; set; } = "Windows";
    public string ElevenLabsApiKey { get; set; } = string.Empty;
    public string ElevenLabsVoiceId { get; set; } = "21m00Tcm4TlvDq8ikWAM";
    public float SpeechRate { get; set; } = 1.0f;
    public string Language { get; set; } = "pt-BR";
    public string VoiceName { get; set; } = string.Empty;
    public string SelectedMicrophone { get; set; } = string.Empty;
    public string WhisperModel { get; set; } = "Small";
    public string PiperExePath { get; set; } = "piper\\piper.exe";
    public string PiperModelPath { get; set; } = "piper\\pt_BR-faber-medium.onnx";
    public string AudioOutputDevice { get; set; } = string.Empty;
}