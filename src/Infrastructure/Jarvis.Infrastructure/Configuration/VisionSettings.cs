namespace Jarvis.Infrastructure.Configuration;

public class VisionSettings
{
    public bool EnableVision { get; set; } = true;
    public int CaptureQuality { get; set; } = 85;
    public string CaptureFormat { get; set; } = "png";
}