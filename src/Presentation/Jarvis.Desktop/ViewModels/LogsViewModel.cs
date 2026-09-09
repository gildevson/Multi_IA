using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Jarvis.Desktop.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _filterText = string.Empty;

    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    public void AddLog(string level, string message)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            LogEntries.Add(new LogEntry(DateTime.Now, level, message));
            if (LogEntries.Count > 500)
                LogEntries.RemoveAt(0);
        });
    }

    [RelayCommand]
    private void ClearLogs() => LogEntries.Clear();
}

public record LogEntry(DateTime Timestamp, string Level, string Message)
{
    public string FormattedTime => Timestamp.ToString("HH:mm:ss");
    public string Display => $"[{FormattedTime}] [{Level}] {Message}";
}
