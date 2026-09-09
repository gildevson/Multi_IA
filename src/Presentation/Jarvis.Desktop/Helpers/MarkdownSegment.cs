namespace Jarvis.Desktop.Helpers;

public class MarkdownSegment
{
    public string Text { get; set; } = "";
    public bool IsCode { get; set; }
    public string Language { get; set; } = "";
}
