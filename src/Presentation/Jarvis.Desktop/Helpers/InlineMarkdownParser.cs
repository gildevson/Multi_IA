using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Jarvis.Desktop.Helpers;

/// <summary>
/// Parses markdown inline syntax and generates WPF controls with Dracula theme.
/// Uses RichTextBox (IsReadOnly) so the user can select and copy text with Ctrl+C.
/// </summary>
public static class InlineMarkdownParser
{
    // Professional palette — clean dark theme
    private static readonly SolidColorBrush CodeBg    = Brush("#1E1E2E");
    private static readonly SolidColorBrush CodeFg    = Brush("#A9B1D6"); // soft blue-gray
    private static readonly SolidColorBrush BoldFg    = Brush("#FFFFFF");  // pure white
    private static readonly SolidColorBrush ItalicFg  = Brush("#C0C0C0"); // light gray
    private static readonly SolidColorBrush Heading1Fg= Brush("#FFFFFF");
    private static readonly SolidColorBrush Heading2Fg= Brush("#E0E0E0");
    private static readonly SolidColorBrush Heading3Fg= Brush("#C8C8C8");
    private static readonly SolidColorBrush BulletFg  = Brush("#6272A4"); // subtle blue-gray
    private static readonly SolidColorBrush NormalFg  = Brush("#D4D4D4"); // readable gray
    private static readonly SolidColorBrush SelectionBg = Brush("#44475A");

    private static readonly SolidColorBrush LinkFg = Brush("#8BE9FD"); // Dracula cyan

    private static readonly Regex InlinePattern = new(
        @"(\*\*[^*\n]+?\*\*|\*[^*\n]+?\*|`[^`\n]+?`|\[[^\]\n]+\]\(https?://[^\)\n]+\)|https?://[^\s<>""\)\n]+)",
        RegexOptions.Compiled);

    public static IEnumerable<FrameworkElement> Parse(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) yield break;

        var lines = markdown.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            // Headings
            if (line.StartsWith("### "))
            {
                yield return MakeSimpleRtb(line[4..], Heading3Fg, 13, FontWeights.SemiBold, new Thickness(0, 6, 0, 2));
                continue;
            }
            if (line.StartsWith("## "))
            {
                yield return MakeSimpleRtb(line[3..], Heading2Fg, 15, FontWeights.SemiBold, new Thickness(0, 8, 0, 2));
                continue;
            }
            if (line.StartsWith("# "))
            {
                yield return MakeSimpleRtb(line[2..], Heading1Fg, 17, FontWeights.Bold, new Thickness(0, 8, 0, 4));
                continue;
            }

            // Bullet list items
            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                yield return MakeInlineRtb(line[2..], NormalFg, new Thickness(14, 1, 0, 1), prefix: new Run("• ") { Foreground = BulletFg });
                continue;
            }

            // Numbered list
            var numMatch = Regex.Match(line, @"^(\d+)\.\s(.+)$");
            if (numMatch.Success)
            {
                yield return MakeInlineRtb(numMatch.Groups[2].Value, NormalFg, new Thickness(14, 1, 0, 1),
                    prefix: new Run($"{numMatch.Groups[1].Value}. ") { Foreground = BulletFg });
                continue;
            }

            // Empty line → small spacer
            if (string.IsNullOrWhiteSpace(line))
            {
                yield return new Border { Height = 6 };
                continue;
            }

            // Normal text with inline formatting
            yield return MakeInlineRtb(line, NormalFg);
        }
    }

    /// <summary>RichTextBox for plain text (headings).</summary>
    private static RichTextBox MakeSimpleRtb(string text, Brush fg, double size, FontWeight weight, Thickness margin)
    {
        var para = new Paragraph
        {
            Margin = new Thickness(0),
            FontSize = size,
            FontWeight = weight
        };
        para.Inlines.Add(new Run(text) { Foreground = fg });

        return CreateRtb(para, margin);
    }

    /// <summary>RichTextBox for inline-formatted text (bold, italic, code).</summary>
    private static RichTextBox MakeInlineRtb(string text, Brush defaultFg, Thickness? margin = null, Run? prefix = null)
    {
        var para = new Paragraph
        {
            Margin = new Thickness(0),
            FontSize = 13
        };

        if (prefix is not null)
            para.Inlines.Add(prefix);

        var parts = InlinePattern.Split(text);
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
            {
                para.Inlines.Add(new Bold(new Run(part[2..^2])) { Foreground = BoldFg });
            }
            else if (part.StartsWith("*") && part.EndsWith("*") && part.Length > 2)
            {
                para.Inlines.Add(new Italic(new Run(part[1..^1])) { Foreground = ItalicFg });
            }
            else if (part.StartsWith("`") && part.EndsWith("`") && part.Length > 2)
            {
                var border = new Border
                {
                    Background = CodeBg,
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(2, 0, 2, 0),
                    Child = new TextBlock
                    {
                        Text = part[1..^1],
                        Foreground = CodeFg,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 12
                    }
                };
                para.Inlines.Add(new InlineUIContainer(border));
            }
            // Markdown link: [texto](url)
            else if (part.StartsWith("["))
            {
                var m = Regex.Match(part, @"\[([^\]]+)\]\((https?://[^\)]+)\)");
                if (m.Success)
                    para.Inlines.Add(CreateHyperlink(m.Groups[1].Value, m.Groups[2].Value));
                else
                    para.Inlines.Add(new Run(part) { Foreground = defaultFg });
            }
            // URL pura
            else if (part.StartsWith("http://") || part.StartsWith("https://"))
            {
                para.Inlines.Add(CreateHyperlink(part, part));
            }
            else
            {
                para.Inlines.Add(new Run(part) { Foreground = defaultFg });
            }
        }

        if (!para.Inlines.Any())
            para.Inlines.Add(new Run(text) { Foreground = defaultFg });

        return CreateRtb(para, margin ?? new Thickness(0, 1, 0, 1));
    }

    private static RichTextBox CreateRtb(Paragraph para, Thickness margin)
    {
        var doc = new FlowDocument(para)
        {
            PagePadding = new Thickness(0),
            TextAlignment = TextAlignment.Left
        };

        var rtb = new RichTextBox(doc)
        {
            IsReadOnly = true,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = margin,
            IsDocumentEnabled = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            SelectionBrush = SelectionBg,
            Focusable = true,
        };

        return rtb;
    }

    private static Hyperlink CreateHyperlink(string label, string url)
    {
        Uri? uri = null;
        try { uri = new Uri(url); } catch { }

        var hl = new Hyperlink(new Run(label))
        {
            Foreground = LinkFg,
            TextDecorations = TextDecorations.Underline,
            Cursor = System.Windows.Input.Cursors.Hand
        };

        if (uri is not null)
        {
            hl.NavigateUri = uri;
            hl.RequestNavigate += (_, e) =>
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            };
        }

        return hl;
    }

    private static SolidColorBrush Brush(string hex)
    {
        var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
        return new SolidColorBrush(c);
    }
}
