namespace Jarvis.Domain.ValueObjects;

public record ImageData(byte[] Data, string MimeType, int Width = 0, int Height = 0)
{
    public static ImageData FromBytes(byte[] data, string mimeType = "image/png")
        => new(data, mimeType);

    public string ToBase64() => Convert.ToBase64String(Data);

    public string ToDataUrl() => $"data:{MimeType};base64,{ToBase64()}";

    public bool IsEmpty => Data is null || Data.Length == 0;
}