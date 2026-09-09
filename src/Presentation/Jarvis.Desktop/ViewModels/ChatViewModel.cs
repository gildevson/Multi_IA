using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Commands;
using Jarvis.Application.DTOs;
using Jarvis.Application.Queries;
using Jarvis.Desktop.Messages;
using Jarvis.Desktop.Views;
using Jarvis.Infrastructure.Configuration;
using Jarvis.Domain.ValueObjects;
using Jarvis.Infrastructure.Configuration;
using Jarvis.Tools.Browser;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Jarvis.Desktop.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatViewModel> _logger;
    private readonly ITextToSpeechProvider _ttsProvider;
    private readonly WebFetchSearchTool _webSearchTool;
    private CancellationTokenSource? _listenCts;
    private CancellationTokenSource? _sendCts;

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private bool _isListening;

    [ObservableProperty]
    private bool _isThinking;

    [ObservableProperty]
    private bool _voiceEnabled;

    [ObservableProperty]
    private bool _isSpeaking;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Imagem pendente para envio
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingImage))]
    private string? _pendingImagePath;

    [ObservableProperty]
    private string? _pendingImageBase64;

    [ObservableProperty]
    private string? _pendingImageMimeType;

    public bool HasPendingImage => PendingImagePath is not null;

    // PDF pendente para envio
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingPdf))]
    private string? _pendingPdfPath;

    [ObservableProperty]
    private string? _pendingPdfBase64;

    [ObservableProperty]
    private string? _pendingPdfFileName;

    public bool HasPendingPdf => PendingPdfPath is not null;

    public string CurrentModel { get; }
    public string DisplayUserName { get; }

    private readonly JarvisSettings _jarvisSettings;
    public string ChatBackgroundImagePath => _jarvisSettings.ChatBackgroundImagePath;
    public double ChatBackgroundOpacity => _jarvisSettings.ChatBackgroundOpacity > 0 ? _jarvisSettings.ChatBackgroundOpacity : 0.25;

    public ObservableCollection<MessageDto> Messages { get; } = new();

    public ChatViewModel(IMediator mediator, ILogger<ChatViewModel> logger, IAIProvider aiProvider, ITextToSpeechProvider ttsProvider, IOptions<JarvisSettings> settings, WebFetchSearchTool webSearchTool)
    {
        _mediator = mediator;
        _logger = logger;
        _ttsProvider = ttsProvider;
        _webSearchTool = webSearchTool;
        _jarvisSettings = settings.Value;
        var s = settings.Value;

        WeakReferenceMessenger.Default.Register<SettingsSavedMessage>(this, (_, _) =>
        {
            OnPropertyChanged(nameof(ChatBackgroundImagePath));
            OnPropertyChanged(nameof(ChatBackgroundOpacity));
        });
        var model = s.AIProvider switch
        {
            "OpenAI" => s.AI.OpenAIModel,
            "Claude" => s.AI.ClaudeModel,
            "Ollama" => s.AI.OllamaModel,
            "Gemini" => s.AI.GeminiModel,
            _ => "Unknown"
        };
        CurrentModel = $"{aiProvider.Name} · {model}";
        DisplayUserName = string.IsNullOrEmpty(s.UserName) ? "User" : s.UserName;
        _voiceEnabled = s.EnableVoiceByDefault;
        _ = LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var result = await _mediator.Send(new GetConversationHistoryQuery());
            if (result.IsSuccess)
            {
                foreach (var msg in result.Value.Messages)
                    Messages.Add(msg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load conversation history");
        }

        if (Messages.Count == 0)
        {
            Messages.Add(new MessageDto(
                Guid.NewGuid(),
                "Assistant",
                "Olá! Sou **J.A.R.V.I.S.** — Just A Rather Very Intelligent System.\n\nEstou pronto para ajudá-lo. Você pode digitar, usar o microfone 🎤 ou anexar uma imagem 📎.",
                DateTime.Now));
        }
    }

    [RelayCommand]
    private void AttachImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecionar imagem",
            Filter = "Imagens|*.png;*.jpg;*.jpeg;*.gif;*.webp|Todos os arquivos|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        var path = dialog.FileName;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var mime = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            _                 => "image/png"
        };

        var bytes = File.ReadAllBytes(path);
        PendingImagePath = path;
        PendingImageBase64 = Convert.ToBase64String(bytes);
        PendingImageMimeType = mime;
        StatusMessage = $"Imagem anexada: {Path.GetFileName(path)}";
    }

    [RelayCommand]
    private void RemoveImage()
    {
        PendingImagePath = null;
        PendingImageBase64 = null;
        PendingImageMimeType = null;
        StatusMessage = "Ready";
    }

    [RelayCommand]
    private void AttachPdf()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecionar PDF",
            Filter = "PDF|*.pdf|Todos os arquivos|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        var path = dialog.FileName;
        var bytes = File.ReadAllBytes(path);
        PendingPdfPath = path;
        PendingPdfBase64 = Convert.ToBase64String(bytes);
        PendingPdfFileName = Path.GetFileName(path);
        StatusMessage = $"PDF anexado: {PendingPdfFileName}";
    }

    [RelayCommand]
    private void RemovePdf()
    {
        PendingPdfPath = null;
        PendingPdfBase64 = null;
        PendingPdfFileName = null;
        StatusMessage = "Ready";
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        var text = UserInput.Trim();
        var hasImage = HasPendingImage;
        var hasPdf = HasPendingPdf;

        if (string.IsNullOrEmpty(text) && !hasImage && !hasPdf) return;

        string displayText;
        if (hasPdf) displayText = string.IsNullOrEmpty(text) ? $"📄 {PendingPdfFileName}" : $"📄 {PendingPdfFileName} — {text}";
        else displayText = string.IsNullOrEmpty(text) ? "📎 Imagem" : text;

        UserInput = string.Empty;
        IsThinking = true;
        _sendCts = new CancellationTokenSource();
        var ct = _sendCts.Token;

        // Captura imagem antes de limpar
        var imageBase64 = PendingImageBase64;
        var imageMime = PendingImageMimeType;
        ImageData? imageData = null;
        if (hasImage && imageBase64 is not null && imageMime is not null)
        {
            imageData = ImageData.FromBytes(Convert.FromBase64String(imageBase64), imageMime);
            PendingImagePath = null;
            PendingImageBase64 = null;
            PendingImageMimeType = null;
        }

        // Captura PDF antes de limpar
        PdfData? pdfData = null;
        if (hasPdf && PendingPdfBase64 is not null && PendingPdfFileName is not null)
        {
            pdfData = PdfData.FromBytes(Convert.FromBase64String(PendingPdfBase64), PendingPdfFileName);
            PendingPdfPath = null;
            PendingPdfBase64 = null;
            PendingPdfFileName = null;
        }

        Messages.Add(new MessageDto(Guid.NewGuid(), DisplayUserName, displayText, DateTime.Now,
            ImageBase64: imageBase64, ImageMimeType: imageMime));

        try
        {
            // Se a pergunta precisa de dados atuais e não tem imagem/pdf, busca na web primeiro
            string? aiContextPrompt = null;
            if (!hasImage && !hasPdf && !string.IsNullOrEmpty(text) && NeedsWebSearch(text))
            {
                StatusMessage = "🔍 Buscando na internet...";
                try
                {
                    var searchQuery = BuildSearchQuery(text);
                    var searchResult = await _webSearchTool.ExecuteAsync(
                        new Dictionary<string, object> { ["query"] = searchQuery }, ct);

                    if (searchResult.IsSuccess
                        && !string.IsNullOrEmpty(searchResult.Value.Output)
                        && !searchResult.Value.Output.StartsWith("Nenhum resultado"))
                        aiContextPrompt = $"{searchResult.Value.Output}\n\nCom base nesses resultados, responda em português: {text}";
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Web search failed, proceeding without search results");
                }
            }

            StatusMessage = "J.A.R.V.I.S. is thinking...";
            var result = await _mediator.Send(new SendMessageCommand(text, VoiceEnabled, imageData, pdfData, aiContextPrompt), ct);

            if (result.IsSuccess)
            {
                if (!string.IsNullOrEmpty(result.Value.Content))
                    Messages.Add(new MessageDto(Guid.NewGuid(), "Assistant", result.Value.Content, DateTime.Now));
                StatusMessage = "Ready";

                // Atualiza a lista de sessões na sidebar
                WeakReferenceMessenger.Default.Send(new SessionsChangedMessage());

                // Notificação se a janela não estiver em foco
                if (!(System.Windows.Application.Current.MainWindow?.IsActive ?? true))
                    ToastWindow.ShowToast("Resposta pronta! Clique para ver.");

                if (VoiceEnabled)
                {
                    IsSpeaking = true;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(400);
                        while (_ttsProvider.IsSpeaking)
                            await Task.Delay(250);
                        IsSpeaking = false;
                    });
                }
            }
            else
            {
                Messages.Add(new MessageDto(Guid.NewGuid(), "Assistant", $"Error: {result.Error}", DateTime.Now));
                StatusMessage = "Error occurred";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelado";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessage failed");
            Messages.Add(new MessageDto(Guid.NewGuid(), "Assistant", $"Unexpected error: {ex.Message}", DateTime.Now));
            StatusMessage = "Error";
        }
        finally
        {
            IsThinking = false;
            _sendCts?.Dispose();
            _sendCts = null;
        }
    }

    private static bool NeedsWebSearch(string text)
    {
        // Normaliza removendo acentos para comparação
        var lower = RemoveAccents(text.ToLowerInvariant());

        string[] keywords =
        [
            // Tempo atual
            "hoje", "agora", "atual", "atualmente", "recente", "recentemente",
            "esta semana", "esse mes", "este mes", "esse ano", "este ano",
            "ontem", "amanha", "semana passada", "mes passado",
            // Dados em tempo real
            "preco", "cotacao", "cambio", "dolar", "euro", "bitcoin", "cripto",
            "bolsa", "acao", "mercado", "inflacao", "juros", "selic",
            // Notícias e eventos
            "noticia", "noticias", "aconteceu", "o que foi", "ultimo",
            "quem ganhou", "resultado", "placar", "jogo", "partida",
            "eleicao", "presidente", "governo", "politica",
            // Lançamentos
            "lancamento", "novo modelo", "nova versao", "atualizacao",
            // Clima
            "clima", "temperatura", "chuva", "previsao do tempo",
            // Bancos e empresas brasileiras (frequentemente com notícias recentes)
            "banco", "financeira", "corretora", "investimento", "fundo",
            "empresa", "startup", "falencia", "concordata", "liquidacao",
            "cvm", "bacen", "banco central", "receita federal",
            // Perguntas sobre pessoas/entidades específicas
            "quem e", "quem foi", "o que e", "o que foi",
            "oque e", "oque foi", "oque voce sabe", "o que voce sabe",
            "me fale sobre", "me conta sobre", "fale sobre",
            "noticias sobre", "novidades sobre", "o que aconteceu com",
            // Tecnologia e produtos recentes
            "iphone", "samsung", "nvidia", "amd", "intel",
            // Esportes
            "campeonato", "copa", "olimpiadas", "brasileirao", "serie a",
            "classificacao", "tabela"
        ];

        return keywords.Any(k => lower.Contains(k));
    }

    private static string BuildSearchQuery(string text)
    {
        var lower = RemoveAccents(text.ToLowerInvariant()).Trim();
        var hoje = DateTime.Now.ToString("yyyy");

        // Pedido genérico de notícias → busca notícias do Brasil de hoje
        if (lower is "noticias" or "as noticias" or "ultimas noticias" or "novidades"
            || lower == "me de as noticias" || lower == "quais as noticias de hoje")
            return $"últimas notícias brasil {hoje}";

        // Padrão 1: "noticias de/sobre X" (notícia no início)
        var noticiaMatch = System.Text.RegularExpressions.Regex.Match(lower,
            @"noticias?\w*\s+(?:de|sobre|do|da|dos|das)\s+(.+)");
        if (noticiaMatch.Success)
        {
            var tema = CleanQuery(noticiaMatch.Groups[1].Value);
            return $"notícias {tema} {hoje}";
        }

        // Padrão 2: "X teria/tem/tinha noticias" (tema no início, notícia no fim)
        var noticiaFimMatch = System.Text.RegularExpressions.Regex.Match(lower,
            @"^(.+?)\s+(?:teria|tem|tinha|ter|teve)\s+(?:alguma[s]?\s+)?noticias?\w*");
        if (noticiaFimMatch.Success)
        {
            var tema = CleanQuery(noticiaFimMatch.Groups[1].Value);
            return $"notícias {tema} {hoje}";
        }

        // Padrão 3: "X alguma noticia" ou "X noticias" no final da frase
        var noticiaFinalMatch = System.Text.RegularExpressions.Regex.Match(lower,
            @"^(.+?)\s+(?:alguma[s]?\s+)?noticias?\w*\s*\??$");
        if (noticiaFinalMatch.Success)
        {
            var tema = CleanQuery(noticiaFinalMatch.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(tema))
                return $"notícias {tema} {hoje}";
        }

        // Padrão 4: "me fale sobre X", "o que você sabe sobre X"
        var sobreMatch = System.Text.RegularExpressions.Regex.Match(lower,
            @"(?:sobre|a respeito de|fale de|sabe sobre|sabe de)\s+(.+)");
        if (sobreMatch.Success)
        {
            var tema = CleanQuery(sobreMatch.Groups[1].Value);
            return $"{tema} {hoje}";
        }

        // Perguntas muito curtas (1-2 palavras) → adiciona contexto
        if (text.Split(' ').Length <= 2 && NeedsWebSearch(text))
            return $"{text} brasil {hoje}";

        // Query longa: remove palavras de preenchimento e envia limpa
        return CleanLongQuery(text, hoje);
    }

    // Remove palavras de preenchimento e ruído da query
    private static string CleanQuery(string query)
    {
        var fillers = new[] { "teria", "alguma", "algumas", "algum", "alguns", "no brasil",
            "entende", "sabe", "né", "ne", "hein", "mesmo", "hoje em dia", "atualmente" };
        var result = query.Trim('?', ' ', '!', 'w', 'W');
        foreach (var f in fillers)
            result = System.Text.RegularExpressions.Regex.Replace(result, $@"\b{f}\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        return System.Text.RegularExpressions.Regex.Replace(result, @"\s{2,}", " ").Trim();
    }

    private static string CleanLongQuery(string text, string ano)
    {
        // Para queries longas, remove filler words comuns e trunca em 60 chars
        var lower = RemoveAccents(text.ToLowerInvariant());
        var fillers = new[] { "teria", "alguma", "algum", "entende", "sabe", "né", "voce sabe",
            "me diga", "me fale", "me conta", "quero saber", "gostaria de saber", "pode me dizer" };
        foreach (var f in fillers)
            lower = System.Text.RegularExpressions.Regex.Replace(lower, $@"\b{f}\b", " ");
        lower = System.Text.RegularExpressions.Regex.Replace(lower, @"\s{2,}", " ").Trim('?', ' ', '!');
        if (lower.Length > 80) lower = lower[..80];
        return $"{lower} {ano}";
    }

    private static string RemoveAccents(string text)
    {
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private bool CanSendMessage() => !IsThinking;

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SearchAndSendAsync()
    {
        var query = UserInput.Trim();
        if (string.IsNullOrEmpty(query)) return;

        UserInput = string.Empty;
        IsThinking = true;
        _sendCts = new CancellationTokenSource();
        var ct = _sendCts.Token;
        StatusMessage = "Pesquisando na internet...";

        Messages.Add(new MessageDto(Guid.NewGuid(), DisplayUserName, $"🔍 {query}", DateTime.Now));

        try
        {
            var searchResult = await _webSearchTool.ExecuteAsync(
                new Dictionary<string, object> { ["query"] = query }, ct);

            string? aiContext = searchResult.IsSuccess && !searchResult.Value.Output.StartsWith("Nenhum resultado")
                ? $"{searchResult.Value.Output}\n\nCom base nesses resultados, responda em português: {query}"
                : null;

            var result = await _mediator.Send(new SendMessageCommand(query, VoiceEnabled, null, null, aiContext), ct);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Value.Content))
                Messages.Add(new MessageDto(Guid.NewGuid(), "Assistant", result.Value.Content, DateTime.Now));
            else if (!result.IsSuccess)
                Messages.Add(new MessageDto(Guid.NewGuid(), "Assistant", $"Erro: {result.Error}", DateTime.Now));

            WeakReferenceMessenger.Default.Send(new SessionsChangedMessage());
            if (!(System.Windows.Application.Current.MainWindow?.IsActive ?? true))
                ToastWindow.ShowToast("Resposta pronta! Clique para ver.");

            StatusMessage = "Ready";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelado";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchAndSend failed");
            Messages.Add(new MessageDto(Guid.NewGuid(), "Assistant", $"Erro na busca: {ex.Message}", DateTime.Now));
            StatusMessage = "Error";
        }
        finally
        {
            IsThinking = false;
            _sendCts?.Dispose();
            _sendCts = null;
        }
    }

    [RelayCommand]
    private async Task StartListeningAsync()
    {
        if (IsListening)
        {
            _listenCts?.Cancel();
            return;
        }

        IsListening = true;
        StatusMessage = "Ouvindo... (clique 🎤 novamente para parar)";
        _listenCts = new CancellationTokenSource();
        await Task.Yield();

        try
        {
            var result = await _mediator.Send(new StartListeningCommand(), _listenCts.Token);
            if (result.IsSuccess && !string.IsNullOrEmpty(result.Value))
            {
                UserInput = result.Value;
                await SendMessageAsync();
            }
            else
            {
                StatusMessage = "Nenhuma fala detectada";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Escuta cancelada";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Listening failed");
            StatusMessage = "Erro ao ouvir";
        }
        finally
        {
            IsListening = false;
            _listenCts?.Dispose();
            _listenCts = null;
        }
    }

    [RelayCommand]
    private void CancelResponse()
    {
        _sendCts?.Cancel();
        StatusMessage = "Cancelando...";
    }

    [RelayCommand]
    private void StopSpeaking()
    {
        _ttsProvider.StopSpeaking();
        IsSpeaking = false;
        StatusMessage = "Ready";
    }

    [RelayCommand]
    private void ToggleVoice()
    {
        VoiceEnabled = !VoiceEnabled;
        StatusMessage = VoiceEnabled ? "Voz ativada 🔊" : "Voz desativada 🔇";
    }

    [RelayCommand]
    private async Task ClearConversationAsync()
    {
        await _mediator.Send(new ClearConversationCommand());
        Messages.Clear();
        PendingImagePath = null;
        PendingImageBase64 = null;
        PendingImageMimeType = null;
        PendingPdfPath = null;
        PendingPdfBase64 = null;
        PendingPdfFileName = null;
        StatusMessage = "Conversation cleared";
    }
}
