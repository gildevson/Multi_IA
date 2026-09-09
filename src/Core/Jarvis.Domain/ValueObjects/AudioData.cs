namespace Jarvis.Domain.ValueObjects;

public record AudioData(byte[] Data, string Format, int SampleRate)
{
    public static AudioData FromBytes(byte[] data, string format = "wav", int sampleRate = 16000)
        => new(data, format, sampleRate);

    public bool IsEmpty => Data is null || Data.Length == 0;
}