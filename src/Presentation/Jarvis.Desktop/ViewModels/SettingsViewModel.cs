using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Jarvis.Desktop.Messages;
using Jarvis.Infrastructure.Configuration;
using Jarvis.Voice.SpeechToText;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using NAudio.Wave;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Speech.Synthesis;
using System.Text.Json;

namespace Jarvis.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly JarvisSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;

    [ObservableProperty] private string _userName;
    [ObservableProperty] private string _aiProvider;
    [ObservableProperty] private string _openAIApiKey;
    [ObservableProperty] private string _openAIModel;
    [ObservableProperty] private string _claudeApiKey;
    [ObservableProperty] private string _claudeModel;
    [ObservableProperty] private string _ollamaBaseUrl;
    [ObservableProperty] private string _ollamaModel;
    [ObservableProperty] private string _ollamaVisionModel;
    [ObservableProperty] private string _ollamaPdfModel;
    [ObservableProperty] private string _systemPrompt;
    [ObservableProperty] private string _sttProvider;
    [ObservableProperty] private string _ttsProvider;
    [ObservableProperty] private string _selectedMicrophone;
    [ObservableProperty] private string _whisperModel;
    [ObservableProperty] private string _selectedVoiceName;
    [ObservableProperty] private string _selectedLanguage;
    [ObservableProperty] private string _piperExePath;
    [ObservableProperty] private string _piperModelPath;
    [ObservableProperty] private string _visionProvider;
    [ObservableProperty] private string _geminiVisionModel;
    [ObservableProperty] private string _claudeVisionModel;
    [ObservableProperty] private string _openAIVisionModel;
    [ObservableProperty] private bool _enableVoiceByDefault;
    [ObservableProperty] private string _searchProvider;
    [ObservableProperty] private string _googleApiKey;
    [ObservableProperty] private string _googleSearchEngineId;
    [ObservableProperty] private string _braveApiKey;
    [ObservableProperty] private string _chatBackgroundImagePath;
    [ObservableProperty] private double _chatBackgroundOpacity;
    [ObservableProperty] private string _selectedAudioOutputDevice;

    public List<string> SearchProviders { get; } = new() { "DuckDuckGo", "Google", "Brave" };
    [ObservableProperty] private string _saveStatus = string.Empty;
    [ObservableProperty] private bool _isLoadingModels;

    public List<string> AIProviders { get; } = new() { "OpenAI", "Claude", "Ollama", "Gemini" };
    public List<string> VisionProviders { get; } = new() { "(mesmo do chat)", "Gemini", "Claude", "OpenAI", "Ollama" };
    public List<string> GeminiVisionModels { get; } = new() { "gemini-2.0-flash", "gemini-2.0-flash-lite", "gemini-1.5-flash", "gemini-1.5-flash-8b" };
    public List<string> ClaudeVisionModels { get; } = new() { "claude-haiku-4-5-20251001", "claude-sonnet-4-6", "claude-opus-4-6" };
    public List<string> OpenAIVisionModels { get; } = new() { "gpt-4o-mini", "gpt-4o" };
    public List<string> OllamaVisionModels { get; } = new() { "minicpm-v", "llava", "llava:13b", "llava:34b", "moondream", "bakllava" };
    public List<string> STTProviders { get; } = new() { "Windows", "Whisper" };
    public List<string> TTSProviders { get; } = new() { "Windows", "Piper", "ElevenLabs" };
    public List<string> WhisperModels { get; } = new() { "Tiny (75MB)", "Base (142MB)", "Small (466MB)", "Medium (1.5GB)", "Large (3GB)" };
    public List<string> Microphones { get; } = new();
    public List<string> AudioOutputDevices { get; } = new();
    public List<string> WindowsVoices { get; } = new();
    public List<string> Languages { get; } = new() { "pt-BR", "en-US", "es-ES", "fr-FR", "de-DE", "it-IT", "ja-JP", "zh-CN" };
    public ObservableCollection<string> OllamaModels { get; }

    public SettingsViewModel(IOptions<JarvisSettings> settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _userName = _settings.UserName;
        _enableVoiceByDefault = _settings.EnableVoiceByDefault;
        _aiProvider = _settings.AIProvider;
        _openAIApiKey = _settings.AI.OpenAIApiKey;
        _openAIModel = _settings.AI.OpenAIModel;
        _claudeApiKey = _settings.AI.ClaudeApiKey;
        _claudeModel = _settings.AI.ClaudeModel;
        _ollamaBaseUrl = _settings.AI.OllamaBaseUrl;
        _ollamaModel = _settings.AI.OllamaModel;

        // Inicializa lista com valores salvos ANTES do binding
        var initialModels = new List<string>();
        if (!string.IsNullOrEmpty(_settings.AI.OllamaModel))
            initialModels.Add(_settings.AI.OllamaModel);
        if (!string.IsNullOrEmpty(_settings.AI.OllamaVisionModel) && !initialModels.Contains(_settings.AI.OllamaVisionModel))
            initialModels.Add(_settings.AI.OllamaVisionModel);
        if (!string.IsNullOrEmpty(_settings.AI.OllamaPdfModel) && !initialModels.Contains(_settings.AI.OllamaPdfModel))
            initialModels.Add(_settings.AI.OllamaPdfModel);
        OllamaModels = new ObservableCollection<string>(initialModels);
        _systemPrompt = _settings.AI.SystemPrompt;
        _sttProvider = _settings.Voice.STTProvider;
        _ttsProvider = _settings.Voice.TTSProvider;
        _selectedMicrophone = _settings.Voice.SelectedMicrophone;
        _whisperModel = _settings.Voice.WhisperModel;
        _selectedLanguage = _settings.Voice.Language;
        _piperExePath = _settings.Voice.PiperExePath;
        _piperModelPath = _settings.Voice.PiperModelPath;
        _visionProvider = string.IsNullOrEmpty(_settings.AI.VisionProvider) ? "(mesmo do chat)" : _settings.AI.VisionProvider;
        _geminiVisionModel = _settings.AI.GeminiVisionModel;
        _claudeVisionModel = _settings.AI.ClaudeVisionModel;
        _openAIVisionModel = _settings.AI.OpenAIVisionModel;
        _ollamaVisionModel = _settings.AI.OllamaVisionModel;
        _ollamaPdfModel = _settings.AI.OllamaPdfModel;
        _searchProvider = string.IsNullOrEmpty(_settings.Search.Provider) ? "DuckDuckGo" : _settings.Search.Provider;
        _googleApiKey = _settings.Search.GoogleApiKey;
        _googleSearchEngineId = _settings.Search.GoogleSearchEngineId;
        _braveApiKey = _settings.Search.BraveApiKey;
        _chatBackgroundImagePath = _settings.ChatBackgroundImagePath;
        _chatBackgroundOpacity = _settings.ChatBackgroundOpacity > 0 ? _settings.ChatBackgroundOpacity : 0.25;

        AudioOutputDevices.Add("Padrão do sistema");
        for (int i = 0; i < WaveOut.DeviceCount; i++)
            AudioOutputDevices.Add(WaveOut.GetCapabilities(i).ProductName);
        _selectedAudioOutputDevice = !string.IsNullOrEmpty(_settings.Voice.AudioOutputDevice)
            ? _settings.Voice.AudioOutputDevice
            : "Padrão do sistema";

        using var synth = new SpeechSynthesizer();
        foreach (var voice in synth.GetInstalledVoices())
            WindowsVoices.Add(voice.VoiceInfo.Name);
        _selectedVoiceName = !string.IsNullOrEmpty(_settings.Voice.VoiceName)
            ? _settings.Voice.VoiceName
            : WindowsVoices.FirstOrDefault() ?? string.Empty;

        Microphones.AddRange(WindowsSpeechToTextProvider.GetAvailableMicrophones());
        if (string.IsNullOrEmpty(_selectedMicrophone) && Microphones.Count > 0)
            _selectedMicrophone = Microphones[0];

        _ = LoadOllamaModelsAsync();
    }

    [RelayCommand]
    private void SelectBackgroundImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecionar imagem de fundo",
            Filter = "Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|Todos os arquivos|*.*"
        };
        if (dialog.ShowDialog() == true)
            ChatBackgroundImagePath = dialog.FileName;
    }

    [RelayCommand]
    private void ClearBackgroundImage() => ChatBackgroundImagePath = string.Empty;

    [RelayCommand]
    private void SelectOllamaModel(string model) => OllamaModel = model;

    [RelayCommand]
    private void SelectOllamaVisionModel(string model) => OllamaVisionModel = model;

    [RelayCommand]
    private void SelectOllamaPdfModel(string model) => OllamaPdfModel = model;

    [RelayCommand]
    private async Task LoadOllamaModelsAsync()
    {
        IsLoadingModels = true;
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var url = OllamaBaseUrl.TrimEnd('/') + "/api/tags";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("models", out var models)) return;

            // Adiciona apenas os que ainda não estão na lista
            foreach (var model in models.EnumerateArray())
            {
                var name = model.GetProperty("name").GetString();
                if (!string.IsNullOrEmpty(name) && !OllamaModels.Contains(name))
                    OllamaModels.Add(name);
            }

            // Força atualização do SelectedItem após lista carregar
            var currentModel = OllamaModel;
            var currentVision = OllamaVisionModel;
            var currentPdf = OllamaPdfModel;
            OllamaModel = string.Empty;
            OllamaVisionModel = string.Empty;
            OllamaPdfModel = string.Empty;
            OllamaModel = currentModel;
            OllamaVisionModel = currentVision;
            OllamaPdfModel = currentPdf;
        }
        catch { /* Ollama pode não estar rodando */ }
        finally { IsLoadingModels = false; }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settings.UserName = UserName;
        _settings.EnableVoiceByDefault = EnableVoiceByDefault;
        _settings.AIProvider = AiProvider;
        _settings.AI.OpenAIApiKey = OpenAIApiKey;
        _settings.AI.OpenAIModel = OpenAIModel;
        _settings.AI.ClaudeApiKey = ClaudeApiKey;
        _settings.AI.ClaudeModel = ClaudeModel;
        _settings.AI.OllamaBaseUrl = OllamaBaseUrl;
        _settings.AI.OllamaModel = OllamaModel;
        _settings.AI.OllamaVisionModel = OllamaVisionModel;
        _settings.AI.OllamaPdfModel = OllamaPdfModel;
        _settings.AI.SystemPrompt = SystemPrompt;
        _settings.Voice.STTProvider = SttProvider;
        _settings.Voice.TTSProvider = TtsProvider;
        _settings.Voice.SelectedMicrophone = SelectedMicrophone;
        _settings.Voice.WhisperModel = WhisperModel;
        _settings.Voice.VoiceName = SelectedVoiceName;
        _settings.Voice.Language = SelectedLanguage;
        _settings.Voice.PiperExePath = PiperExePath;
        _settings.Voice.PiperModelPath = PiperModelPath;
        _settings.Voice.AudioOutputDevice = SelectedAudioOutputDevice == "Padrão do sistema"
            ? string.Empty : SelectedAudioOutputDevice;
        _settings.AI.VisionProvider = VisionProvider == "(mesmo do chat)" ? string.Empty : VisionProvider;
        _settings.AI.GeminiVisionModel = GeminiVisionModel;
        _settings.AI.ClaudeVisionModel = ClaudeVisionModel;
        _settings.AI.OpenAIVisionModel = OpenAIVisionModel;
        _settings.AI.OllamaVisionModel = OllamaVisionModel;
        _settings.Search.Provider = SearchProvider;
        _settings.Search.GoogleApiKey = GoogleApiKey;
        _settings.Search.GoogleSearchEngineId = GoogleSearchEngineId;
        _settings.Search.BraveApiKey = BraveApiKey;
        _settings.ChatBackgroundImagePath = ChatBackgroundImagePath;
        _settings.ChatBackgroundOpacity = ChatBackgroundOpacity;
        PersistToFile();
        WeakReferenceMessenger.Default.Send(new SettingsSavedMessage());
        SaveStatus = "✔ Salvo! Limpe a conversa (🗑) para aplicar nome e system prompt.";
    }

    private void PersistToFile()
    {
        try
        {
            var appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var filePath = Path.Combine(appFolder, "appsettings.user.json");

            var jarvisSection = new Dictionary<string, object>
            {
                ["UserName"] = _settings.UserName,
                ["AIProvider"] = _settings.AIProvider,
                ["STTProvider"] = _settings.STTProvider,
                ["TTSProvider"] = _settings.TTSProvider,
                ["DataFolder"] = _settings.DataFolder,
                ["EnableVoiceByDefault"] = _settings.EnableVoiceByDefault,
                ["AI"] = new Dictionary<string, object>
                {
                    ["OpenAIApiKey"] = _settings.AI.OpenAIApiKey,
                    ["OpenAIModel"] = _settings.AI.OpenAIModel,
                    ["ClaudeApiKey"] = _settings.AI.ClaudeApiKey,
                    ["ClaudeModel"] = _settings.AI.ClaudeModel,
                    ["OllamaBaseUrl"] = _settings.AI.OllamaBaseUrl,
                    ["OllamaModel"] = _settings.AI.OllamaModel,
                    ["OllamaVisionModel"] = _settings.AI.OllamaVisionModel,
                    ["OllamaPdfModel"] = _settings.AI.OllamaPdfModel,
                    ["GeminiApiKey"] = _settings.AI.GeminiApiKey,
                    ["GeminiModel"] = _settings.AI.GeminiModel,
                    ["MaxTokens"] = _settings.AI.MaxTokens,
                    ["Temperature"] = _settings.AI.Temperature,
                    ["SystemPrompt"] = _settings.AI.SystemPrompt,
                    ["VisionProvider"] = _settings.AI.VisionProvider,
                    ["GeminiVisionModel"] = _settings.AI.GeminiVisionModel,
                    ["ClaudeVisionModel"] = _settings.AI.ClaudeVisionModel,
                    ["OpenAIVisionModel"] = _settings.AI.OpenAIVisionModel
                },
                ["Voice"] = new Dictionary<string, object>
                {
                    ["STTProvider"] = _settings.Voice.STTProvider,
                    ["TTSProvider"] = _settings.Voice.TTSProvider,
                    ["ElevenLabsApiKey"] = _settings.Voice.ElevenLabsApiKey,
                    ["ElevenLabsVoiceId"] = _settings.Voice.ElevenLabsVoiceId,
                    ["SpeechRate"] = _settings.Voice.SpeechRate,
                    ["Language"] = _settings.Voice.Language,
                    ["VoiceName"] = _settings.Voice.VoiceName,
                    ["SelectedMicrophone"] = _settings.Voice.SelectedMicrophone,
                    ["WhisperModel"] = _settings.Voice.WhisperModel,
                    ["PiperExePath"] = _settings.Voice.PiperExePath,
                    ["PiperModelPath"] = _settings.Voice.PiperModelPath,
                    ["AudioOutputDevice"] = _settings.Voice.AudioOutputDevice
                },
                ["Vision"] = new Dictionary<string, object>
                {
                    ["EnableVision"] = _settings.Vision.EnableVision,
                    ["CaptureQuality"] = _settings.Vision.CaptureQuality,
                    ["CaptureFormat"] = _settings.Vision.CaptureFormat
                },
                ["Search"] = new Dictionary<string, object>
                {
                    ["Provider"] = _settings.Search.Provider,
                    ["GoogleApiKey"] = _settings.Search.GoogleApiKey,
                    ["GoogleSearchEngineId"] = _settings.Search.GoogleSearchEngineId,
                    ["BraveApiKey"] = _settings.Search.BraveApiKey
                },
                ["ChatBackgroundImagePath"] = _settings.ChatBackgroundImagePath,
                ["ChatBackgroundOpacity"] = _settings.ChatBackgroundOpacity
            };

            var finalRoot = new Dictionary<string, object> { ["JarvisSettings"] = jarvisSection };
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(finalRoot, options);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            SaveStatus = $"Erro ao salvar: {ex.Message}";
        }
    }
}
