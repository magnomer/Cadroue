namespace Cadroue.MigrationInterface;

public sealed record LPreviewApplication(
    int LPreviewBrightness,
    int LPreviewContrast,
    int LPreviewSaturation,
    int LPreviewHue,
    uint LPreviewRotation,
    bool LPreviewFlipHorizontal,
    bool LPreviewFlipVertical,
    string LPreviewReason);

public static class LPreview
{
    public static Action<object, LPreviewApplication>? LPreviewApplySeam;

    public static void LPreviewApply(object? lPreviewTarget, LPreviewState lPreviewState)
    {
        if (lPreviewTarget is null)
        {
            return;
        }

        LPreviewApplySeam?.Invoke(lPreviewTarget, LPreviewApplicationResolve(lPreviewState, "preview color/geometry"));
    }

    public static void LPreviewRestore(object? lPreviewTarget, LPreviewState lPreviewState)
    {
        if (lPreviewTarget is null)
        {
            return;
        }

        LPreviewApplySeam?.Invoke(lPreviewTarget, LPreviewApplicationResolve(lPreviewState, "preview restored"));
    }

    private static LPreviewApplication LPreviewApplicationResolve(LPreviewState lPreviewState, string lPreviewReason)
    {
        LColor lColor = lPreviewState.LColor;
        LRotateFlip lRotateFlip = lPreviewState.LRotateFlip;
        return new LPreviewApplication(
            LPreviewValueClamp(lColor.LColorBrightness * 100, -100, 100),
            LPreviewValueClamp((lColor.LColorContrast - 1) * 100, -100, 100),
            LPreviewValueClamp((lColor.LColorSaturation - 1) * 100, -100, 100),
            LPreviewValueClamp(lColor.LColorHue, -180, 180),
            LPreviewRotationRead(lRotateFlip.LRotateKind),
            lRotateFlip.LRotateFlipHorizontal,
            lRotateFlip.LRotateFlipVertical,
            lPreviewReason);
    }

    private static uint LPreviewRotationRead(LRotateKind lRotateKind)
    {
        return lRotateKind switch
        {
            LRotateKind.LRotate90 => 90u,
            LRotateKind.LRotate180 => 180u,
            LRotateKind.LRotate270 => 270u,
            _ => 0u
        };
    }

    private static int LPreviewValueClamp(double lPreviewValue, int lPreviewMinimum, int lPreviewMaximum)
    {
        return Math.Clamp((int)Math.Round(lPreviewValue), lPreviewMinimum, lPreviewMaximum);
    }
}
