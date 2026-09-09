using System.Windows;
using System.Windows.Controls;
using Jarvis.Desktop.Helpers;

namespace Jarvis.Desktop.Controls;

public partial class SyntaxHighlightedCode : UserControl
{
    public static readonly DependencyProperty CodeProperty =
        DependencyProperty.Register(nameof(Code), typeof(string), typeof(SyntaxHighlightedCode),
            new PropertyMetadata(string.Empty, OnPropertiesChanged));

    public static readonly DependencyProperty LanguageProperty =
        DependencyProperty.Register(nameof(Language), typeof(string), typeof(SyntaxHighlightedCode),
            new PropertyMetadata(string.Empty, OnPropertiesChanged));

    public string Code
    {
        get => (string)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public string Language
    {
        get => (string)GetValue(LanguageProperty);
        set => SetValue(LanguageProperty, value);
    }

    public SyntaxHighlightedCode()
    {
        InitializeComponent();
    }

    private static void OnPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SyntaxHighlightedCode control)
            control.Render();
    }

    private void Render()
    {
        CodeTextBlock.Inlines.Clear();
        foreach (var run in SyntaxHighlighter.Highlight(Code, Language))
            CodeTextBlock.Inlines.Add(run);
    }
}
