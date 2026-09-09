using Jarvis.Domain.ValueObjects;
using Jarvis.Shared.Models;
using System.Drawing;

namespace Jarvis.Application.Abstractions;

public interface IScreenCaptureProvider
{
    Task<Result<ImageData>> CaptureFullScreenAsync(
        CancellationToken cancellationToken = default);

    Task<Result<ImageData>> CaptureRegionAsync(
        Rectangle region,
        CancellationToken cancellationToken = default);
}