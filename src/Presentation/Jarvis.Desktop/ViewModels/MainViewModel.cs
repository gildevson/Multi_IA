using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Jarvis.Application.Commands;
using Jarvis.Application.DTOs;
using Jarvis.Application.Queries;
using Jarvis.Desktop.Messages;
using Jarvis.Desktop.Services;
using MediatR;
using System.Collections.ObjectModel;
using System.Windows;

namespace Jarvis.Desktop.ViewModels;

public class SessionGroup
{
    public string Label { get; set; } = "";
    public List<ConversationSummaryDto> Sessions { get; set; } = new();
}

public partial class MainViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly IMediator _mediator;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private bool _isConnected = true;

    [ObservableProperty]
    private bool _isLoadingSession;

    [ObservableProperty]
    private Guid? _loadingSessionId;

    public ObservableCollection<SessionGroup> GroupedSessions { get; } = new();

    public MainViewModel(NavigationService navigationService, ChatViewModel chatViewModel, IMediator mediator)
    {
        _navigationService = navigationService;
        _mediator = mediator;
        CurrentView = chatViewModel;
        _ = LoadSessionsAsync();

        // Atualiza a sidebar toda vez que uma resposta chegar (enviado pelo ChatViewModel)
        WeakReferenceMessenger.Default.Register<SessionsChangedMessage>(this, (_, _) =>
            System.Windows.Application.Current.Dispatcher.InvokeAsync(LoadSessionsAsync));
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            var result = await _mediator.Send(new GetSessionsQuery());
            if (!result.IsSuccess) return;

            var now = DateTime.Now;
            var today = now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            var todayList    = new List<ConversationSummaryDto>();
            var weekList     = new List<ConversationSummaryDto>();
            var olderList    = new List<ConversationSummaryDto>();

            foreach (var s in result.Value)
            {
                var date = s.StartedAt.ToLocalTime().Date;
                if (date == today)              todayList.Add(s);
                else if (date >= weekStart)     weekList.Add(s);
                else                            olderList.Add(s);
            }

            GroupedSessions.Clear();
            if (todayList.Any())  GroupedSessions.Add(new SessionGroup { Label = "Hoje",         Sessions = todayList });
            if (weekList.Any())   GroupedSessions.Add(new SessionGroup { Label = "Esta semana",  Sessions = weekList });
            if (olderList.Any())  GroupedSessions.Add(new SessionGroup { Label = "Mais antigo",  Sessions = olderList });
        }
        catch { /* sessões são opcionais */ }
    }

    [RelayCommand]
    private async Task DeleteSessionAsync(Guid sessionId)
    {
        await _mediator.Send(new DeleteSessionCommand(sessionId));
        await LoadSessionsAsync();
    }

    [RelayCommand]
    private async Task LoadSessionAsync(Guid sessionId)
    {
        IsLoadingSession = true;
        LoadingSessionId = sessionId;
        try
        {
            await _mediator.Send(new ResumeSessionCommand(sessionId));
            await LoadSessionsAsync();
            CurrentView = _navigationService.GetView<ChatViewModel>();
        }
        finally
        {
            IsLoadingSession = false;
            LoadingSessionId = null;
        }
    }

    [RelayCommand]
    private async Task NavigateToChat()
    {
        await _mediator.Send(new ClearConversationCommand());
        await LoadSessionsAsync();
        CurrentView = _navigationService.GetView<ChatViewModel>();
    }

    [RelayCommand]
    private void NavigateToSettings() => CurrentView = _navigationService.GetView<SettingsViewModel>();

    [RelayCommand]
    private void NavigateToTools() => CurrentView = _navigationService.GetView<ToolsViewModel>();

    [RelayCommand]
    private void NavigateToLogs() => CurrentView = _navigationService.GetView<LogsViewModel>();
}
