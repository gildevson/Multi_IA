using Jarvis.Desktop.ViewModels;
using System.Windows.Controls;

namespace Jarvis.Desktop.Views;

public partial class LogsView : UserControl
{
    public LogsView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is LogsViewModel vm)
                vm.LogEntries.CollectionChanged += (_, _) => LogsScroller.ScrollToEnd();
        };
    }
}
