using Jarvis.Application.Abstractions;
using Jarvis.Application.Tools;
using Jarvis.Shared.Models;

namespace Jarvis.Tools.Vision;

public class CaptureScreenTool : ITool
{
    private readonly IScreenCaptureProvider _captureProvider;

    public string Name => "capture_screen";
    public string Description => "Captures the current screen and returns it as a base64 image for visual analysis.";

    public CaptureScreenTool(IScreenCaptureProvider captureProvider)
    {
        _captureProvider = captureProvider;
    }

    public ToolDefinition GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        Parameters = new()
    };

    public async Task<Result<ToolResult>> ExecuteAsync(IDictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        var result = await _captureProvider.CaptureFullScreenAsync(cancellationToken);
        if (!result.IsSuccess)
            return Result.Failure<ToolResult>($"Screen capture failed: {result.Error}");

        var image = result.Value;
        return Result.Success(ToolResult.Ok(
            $"Screen captured ({image.Width}x{image.Height})",
            new Dictionary<string, object>
            {
                ["image_base64"] = image.ToBase64(),
                ["mime_type"] = image.MimeType,
                ["width"] = image.Width,
                ["height"] = image.Height
            }));
    }
}
