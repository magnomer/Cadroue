using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Tests.Interface;

internal static class TNeutral
{
    internal static LNeutralSample TNeutralResolve(
        byte[] pixels, int width, int height, int centerX, int centerY) =>
        LNeutralGain.LNeutralResolve(pixels, width, height, centerX, centerY);

    internal static LNeutralSample TNeutralWhiteResolve(
        byte[] pixels, int width, int height, int centerX, int centerY) =>
        LNeutralGain.LNeutralResolve(
            pixels, width, height, centerX, centerY, LNeutralTarget.LNeutralTargetWhite);

    internal static LNeutralSample TNeutralColorResolve(double x, double y) =>
        LNeutralColor.LNeutralColorResolve(x, y);

    internal static LNeutralWheel TNeutralWheelResolve(int red, int green, int blue) =>
        LNeutralColor.LNeutralWheelResolve(red, green, blue);

    internal static LNeutralWheel TNeutralAnalyzeResolve(
        byte[] pixels, int width, int height, LWhitebalanceMethod method) =>
        LNeutralAnalysis.LNeutralAnalyzeResolve(pixels, width, height, method);

    internal static LNeutralStatus TNeutralStatusResolve(LNeutralOutcome outcome) =>
        LNeutralReadout.LNeutralStatusResolve(outcome);

    internal static LNeutralDisplay TNeutralDisplayResolve(
        bool manual, int red, int green, int blue) =>
        LNeutralReadout.LNeutralDisplayResolve(manual, red, green, blue);

    internal static LNeutralPoint TNeutralPointResolve(
        double clickX, double clickY,
        double displayX, double displayY, double displayWidth, double displayHeight,
        double shownX, double shownY, double shownWidth, double shownHeight,
        LRotateKind rotateKind, bool flipHorizontal, bool flipVertical,
        int sourceWidth, int sourceHeight) =>
        LNeutralOverlay.LNeutralPointResolve(
            clickX, clickY,
            displayX, displayY, displayWidth, displayHeight,
            shownX, shownY, shownWidth, shownHeight,
            rotateKind, flipHorizontal, flipVertical,
            sourceWidth, sourceHeight);
}
