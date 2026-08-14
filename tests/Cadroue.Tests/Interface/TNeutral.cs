using Cadroue.Application;

namespace Cadroue.Tests;

internal static class TNeutral
{
    internal static LNeutralSample Resolve(
        byte[] pixels, int width, int height, int centerX, int centerY) =>
        LNeutral.LNeutralResolve(pixels, width, height, centerX, centerY);

    internal static LNeutralDisplay DisplayResolve(
        bool manual, int red, int green, int blue) =>
        LNeutral.LNeutralDisplayResolve(manual, red, green, blue);

    internal static LNeutralPoint PointResolve(
        double clickX, double clickY,
        double displayX, double displayY, double displayWidth, double displayHeight,
        double shownX, double shownY, double shownWidth, double shownHeight,
        LRotateKind rotateKind, bool flipHorizontal, bool flipVertical,
        int sourceWidth, int sourceHeight) =>
        LNeutral.LNeutralPointResolve(
            clickX, clickY,
            displayX, displayY, displayWidth, displayHeight,
            shownX, shownY, shownWidth, shownHeight,
            rotateKind, flipHorizontal, flipVertical,
            sourceWidth, sourceHeight);
}
