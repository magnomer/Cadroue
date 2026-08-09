using System.Globalization;

using Cadroue.Core;

namespace Cadroue.Application;

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
    public const double LPreviewBrightnessFactor = 2.5;

    public static Action<object, LPreviewApplication>? LPreviewApplySeam;

    public static LColor LPreviewColorResolve(LWorkVideo lVideo)
    {
        double lBrightness = (lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindBrightness
                && lStep.LWorkStepActive)
            ?.LWorkFfmpegValue ?? 0) * LPreviewBrightnessFactor;
        double lContrast = lVideo.LWorkVideoSteps
            .FirstOrDefault(lStep => lStep.LWorkStepKind == LColorKind.LColorKindContrast
                && lStep.LWorkStepActive)
            ?.LWorkFfmpegValue ?? 1;
        return new LColor(lBrightness, lContrast, 1, 0);
    }

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

    private static string LPreviewMpvEqFilterResolve(LColor lColor)
    {
        double lBrightness = lColor.LColorBrightness / LPreviewBrightnessFactor;
        double lContrast = lColor.LColorContrast;
        var lEqParts = new List<string>();

        if (lBrightness != 0)
        {
            lEqParts.Add($"brightness={lBrightness.ToString("0.###", CultureInfo.InvariantCulture)}");
        }

        if (lContrast != 1)
        {
            lEqParts.Add($"contrast={lContrast.ToString("0.###", CultureInfo.InvariantCulture)}");
        }

        return lEqParts.Count > 0 ? "eq=" + string.Join(':', lEqParts) : string.Empty;
    }

    public static string LPreviewMpvFilterResolve(LPreviewState lPreviewState)
    {
        var lFilters = new List<string>();

        string lEq = LPreviewMpvEqFilterResolve(lPreviewState.LColor);
        if (lEq.Length > 0)
        {
            lFilters.Add(lEq);
        }

        LRotateFlip lRotateFlip = lPreviewState.LRotateFlip;

        if (lRotateFlip.LRotateFlipHorizontal)
        {
            lFilters.Add("hflip");
        }

        if (lRotateFlip.LRotateFlipVertical)
        {
            lFilters.Add("vflip");
        }

        string? lRotate = lRotateFlip.LRotateKind switch
        {
            LRotateKind.LRotate90 => "transpose=1",
            LRotateKind.LRotate180 => "transpose=1,transpose=1",
            LRotateKind.LRotate270 => "transpose=2",
            _ => null
        };

        if (lRotate is not null)
        {
            lFilters.Add(lRotate);
        }

        if (lPreviewState.LCropbox is { LCropboxWidth: > 0, LCropboxHeight: > 0 } lCropbox)
        {
            int lCropWidth = (int)Math.Round(lCropbox.LCropboxWidth);
            int lCropHeight = (int)Math.Round(lCropbox.LCropboxHeight);
            int lCropX = (int)Math.Round(lCropbox.LCropboxX);
            int lCropY = (int)Math.Round(lCropbox.LCropboxY);
            lFilters.Add($"crop={lCropWidth}:{lCropHeight}:{lCropX}:{lCropY}");
        }

        return lFilters.Count > 0 ? "lavfi=[" + string.Join(',', lFilters) + "]" : string.Empty;
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
