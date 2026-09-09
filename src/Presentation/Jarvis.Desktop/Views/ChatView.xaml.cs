using Jarvis.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Jarvis.Desktop.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
        // Rola para o fim sempre que o conteúdo crescer (novo texto renderizado)
        MessagesScroller.ScrollChanged += (_, e) =>
        {
            if (e.ExtentHeightChange > 0)
                MessagesScroller.ScrollToEnd();
        };
    }

    private void AttachButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu is { } menu)
        {
            menu.PlacementTarget = btn;
            menu.Placement = PlacementMode.Top;
            menu.IsOpen = true;
        }
    }

    private void CopyCodeBlock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string code)
            FlashCopyButton(btn, code);
    }

    private void CopyMessage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string content)
            FlashCopyButton(btn, content, originalLabel: "📋 Copiar", copiedLabel: "✓ Copiado!");
    }

    private static void FlashCopyButton(Button btn, string text,
        string originalLabel = "⎘ Copy", string copiedLabel = "✓ Copied!")
    {
        Clipboard.SetText(text);
        var originalContent = btn.Content;
        btn.Content = copiedLabel;
        btn.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(80, 250, 123));
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        timer.Tick += (_, _) =>
        {
            btn.Content = originalContent;
            btn.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(98, 114, 164));
            timer.Stop();
        };
        timer.Start();
    }
}
