using Jarvis.Domain.ValueObjects;

namespace Jarvis.Domain.ValueObjects;

public record MessageContent
{
    public string Text { get; init; }
    public bool HasImage { get; init; }
    public ImageData? Image { get; init; }

    private MessageContent(string text, bool hasImage, ImageData? image)
    {
        Text = text;
        HasImage = hasImage;
        Image = image;
    }

    public static MessageContent CreateText(string text)
        => new(text, false, null);

    public static MessageContent CreateWithImage(string text, ImageData image)
        => new(text, true, image);

    public static MessageContent Reconstitute(string text, bool hasImage, ImageData? image)
        => new(text, hasImage, image);

    public override string ToString() => Text;
}