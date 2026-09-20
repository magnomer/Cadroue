using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Tests;

public sealed class TPreview
{
    public readonly record struct TPreviewResult(
        int TPreviewBrightness,
        int TPreviewContrast,
        int TPreviewSaturation,
        int TPreviewHue,
        uint TPreviewRotation);

    public TPreviewResult TPreviewApply(LPreviewState state)
    {
        LPreviewApplication application = LPreview.LPreviewApplicationResolve(state, "preview color/geometry");
        return new TPreviewResult(
            application.LPreviewBrightness,
            application.LPreviewContrast,
            application.LPreviewSaturation,
            application.LPreviewHue,
            application.LPreviewRotation);
    }
}
