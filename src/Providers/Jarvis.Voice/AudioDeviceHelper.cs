using NAudio.Wave;

namespace Jarvis.Voice.TextToSpeech;

internal static class AudioDeviceHelper
{
    /// <summary>Returns the WaveOut device number for the given device name.
    /// Returns -1 (system default) if the name is empty or not found.</summary>
    internal static int GetDeviceNumber(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName)) return -1;
        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var caps = WaveOut.GetCapabilities(i);
            if (caps.ProductName.StartsWith(deviceName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
}
