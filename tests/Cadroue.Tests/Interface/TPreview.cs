using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TPreview
{
    public readonly record struct TPreviewResult(
        int Brightness,
        int Contrast,
        int Saturation,
        int Hue,
        uint Rotation);

    public TPreviewResult ApplyState(LPreviewState state)
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
}
