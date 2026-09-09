using System.Windows;
using System.Windows.Threading;

namespace Jarvis.Desktop.Views;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _closeTimer;

    private ToastWindow(string message)
    {
        InitializeComponent();
        MessageText.Text = message;

        // Posiciona no canto inferior direito
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Bottom - Height - 16;

        _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            Close();
        };
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        _closeTimer.Start();
    }

    /// <summary>Exibe uma notificação toast no canto inferior direito por 4 segundos.</summary>
    public static void ShowToast(string message)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var toast = new ToastWindow(message);
            toast.Show();
        });
    }
}
