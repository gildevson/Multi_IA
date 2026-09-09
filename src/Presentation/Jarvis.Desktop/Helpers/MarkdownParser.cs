using System.Text.RegularExpressions;

namespace Jarvis.Desktop.Helpers;

public static class MarkdownParser
{
    private static readonly Regex CodeBlockRegex = new(
        @"```(\w*)\n?([\s\S]*?)```",
        RegexOptions.Compiled);

    public static List<MarkdownSegment> Parse(string markdown)
    {
        var segments = new List<MarkdownSegment>();
        if (string.IsNullOrEmpty(markdown))
            return segments;

        var lastIndex = 0;
        foreach (Match match in CodeBlockRegex.Matches(markdown))
        {
            if (match.Index > lastIndex)
            {
                var text = markdown[lastIndex..match.Index].Trim();
                if (!string.IsNullOrEmpty(text))
                    segments.Add(new MarkdownSegment { Text = text, IsCode = false });
            }

            segments.Add(new MarkdownSegment
            {
                Text = match.Groups[2].Value.TrimEnd(),
                Language = match.Groups[1].Value.ToLower(),
                IsCode = true
            });

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < markdown.Length)
        {
            var remaining = markdown[lastIndex..].Trim();
            if (!string.IsNullOrEmpty(remaining))
                segments.Add(new MarkdownSegment { Text = remaining, IsCode = false });
        }

        if (segments.Count == 0)
            segments.Add(new MarkdownSegment { Text = markdown, IsCode = false });

        return segments;
    }
}
