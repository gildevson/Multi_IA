namespace Jarvis.Domain.ValueObjects;

public record PdfData(byte[] Data, string FileName)
{
    public static PdfData FromBytes(byte[] data, string fileName)
        => new(data, fileName);

    public string ToBase64() => Convert.ToBase64String(Data);

    public bool IsEmpty => Data is null || Data.Length == 0;
}
