using Jarvis.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Jarvis.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private WindowState _previousState;
    private WindowStyle _previousStyle;

    // ── Hotkey global ────────────────────────────────────────────────────────
    // Ctrl+Shift+J (evita conflito com atalhos reservados do Windows)
    private const int  HotkeyId  = 0x4A52;
    private const uint ModCtrl   = 0x0002;
    private const uint ModShift  = 0x0004;
    private const int  WmHotkey  = 0x0312;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SwRestore = 9;

    public MainWindow(MainViewModel viewModel, ILogger<MainWindow> logger)
    {
        InitializeComponent();
        DataContext = viewModel;
        _logger = logger;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;

        var ok = RegisterHotKey(hwnd, HotkeyId, ModCtrl | ModShift, (uint)'J');
        if (ok)
            _logger.LogInformation("Hotkey global registrado: Ctrl+Shift+J");
        else
            _logger.LogWarning("Falha ao registrar hotkey Ctrl+Shift+J (pode estar em uso por outro app)");

        HwndSource.FromHwnd(hwnd)?.AddHook(HotKeyHook);
    }

    private IntPtr HotKeyHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            BringToFront();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void BringToFront()
    {
        var helper = new WindowInteropHelper(this);

        // Restaura se minimizado
        if (WindowState == WindowState.Minimized)
            ShowWindow(helper.Handle, SwRestore);

        Show();
        WindowState = WindowState.Normal;
        SetForegroundWindow(helper.Handle);
        Activate();
        Focus();
    }

    protected override void OnClosed(EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        UnregisterHotKey(helper.Handle, HotkeyId);
        base.OnClosed(e);
    }

    // ── Teclado ──────────────────────────────────────────────────────────────
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
            ToggleFullscreen();
    }

    private void ToggleFullscreen_Click(object sender, RoutedEventArgs e)
        => ToggleFullscreen();

    private void ToggleFullscreen()
    {
        if (WindowStyle == WindowStyle.None)
        {
            // Sair da tela cheia
            WindowStyle = _previousStyle;
            ResizeMode = ResizeMode.CanResize;
            WindowState = _previousState;
            FullscreenIcon.Text = "⛶";
        }
        else
        {
            // Entrar na tela cheia
            _previousStyle = WindowStyle;
            _previousState = WindowState;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            // Workaround para WPF cobrir a barra de tarefas corretamente:
            // é necessário passar por Normal antes de Maximized
            WindowState = WindowState.Normal;
            WindowState = WindowState.Maximized;
            FullscreenIcon.Text = "✕";
        }
    }
}
