using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TPreview
{
    private static readonly object TPreviewGate = new();

    public readonly record struct TPreviewResult(
        int Brightness,
        int Contrast,
        int Saturation,
        int Hue,
        uint Rotation);

    public TPreviewResult ApplyState(LPreviewState state)
    {
        lock (TPreviewGate)
        {
            Action<object, LPreviewApplication>? previous = LPreview.LPreviewApplySeam;
            try
            {
                LPreviewApplication? captured = null;
                LPreview.LPreviewApplySeam = (_, application) => captured = application;
                LPreview.LPreviewApply(new object(), state);
                Assert.NotNull(captured);
                return new TPreviewResult(
                    captured!.LPreviewBrightness,
                    captured.LPreviewContrast,
                    captured.LPreviewSaturation,
                    captured.LPreviewHue,
                    captured.LPreviewRotation);
            }
            finally
            {
                LPreview.LPreviewApplySeam = previous;
            }
        }
    }
}
