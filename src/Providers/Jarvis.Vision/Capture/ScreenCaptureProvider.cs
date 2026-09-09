using Jarvis.Application.Abstractions;
using Jarvis.Domain.ValueObjects;
using Jarvis.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;

namespace Jarvis.Vision.Capture;

public class ScreenCaptureProvider : IScreenCaptureProvider
{
    private readonly ILogger<ScreenCaptureProvider> _logger;

    public ScreenCaptureProvider(ILogger<ScreenCaptureProvider> logger)
    {
        _logger = logger;
    }

    public Task<Result<ImageData>> CaptureFullScreenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var screenBounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds
                ?? new Rectangle(0, 0, 1920, 1080);

            return Task.FromResult(CaptureRegion(screenBounds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture full screen");
            return Task.FromResult(Result.Failure<ImageData>($"Screen capture failed: {ex.Message}"));
        }
    }

    public Task<Result<ImageData>> CaptureRegionAsync(Rectangle region, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(CaptureRegion(region));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture screen region");
            return Task.FromResult(Result.Failure<ImageData>($"Region capture failed: {ex.Message}"));
        }
    }

    private Result<ImageData> CaptureRegion(Rectangle region)
    {
        using var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.Location, Point.Empty, region.Size, CopyPixelOperation.SourceCopy);

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        var bytes = ms.ToArray();

        _logger.LogDebug("Screen captured: {Width}x{Height} ({Bytes} bytes)", region.Width, region.Height, bytes.Length);
        return Result.Success(new ImageData(bytes, "image/png", region.Width, region.Height));
    }
}
