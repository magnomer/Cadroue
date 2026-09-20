using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LInspector
{
    private static readonly LColorKind[] lInspectorKinds =
    {
        LColorKind.LColorKindBrightness,
        LColorKind.LColorKindContrast,
        LColorKind.LColorKindSaturation,
        LColorKind.LColorKindGamma,
        LColorKind.LColorKindWhitebalance,
        LColorKind.LColorKindExposure,
        LColorKind.LColorKindCurve
    };

    private bool lInspectorMinimized;
    private string? lInspectorStep;
    private double lInspectorSourceWidth = 1920;
    private double lInspectorSourceHeight = 1080;
    private bool lInspectorSourcePresent;
    private bool lInspectorCropCapable = true;
    private bool lInspectorOrientationCapable = true;
    private bool lInspectorToolArmed;
    private int lInspectorSaveDepth;
    private string? lInspectorOwnerPath;
    private string? lInspectorFailurePath;

    public event Action? LInspectorChange;

    public LCropboxEdgeLock LInspectorEdgeLock { get; } = new();

    public LCropboxState LInspectorCropbox { get; } = new();

    public LTone LInspectorTone { get; } = new();

    public LGamma LInspectorGamma { get; } = new();

    public LExposure LInspectorExposure { get; } = new();

    public LCurve LInspectorCurve { get; } = new();

    public LWhitebalance LInspectorWhitebalance { get; } = new();

    public LSkip LInspectorSkip { get; } = new();

    public LSensor LInspectorSensor { get; } = new();

    public LBlank LInspectorBlank { get; } = new();

    public bool LInspectorMinimized => lInspectorMinimized;

    public string? LInspectorStep => lInspectorStep;

    public double LInspectorSourceWidth => lInspectorSourceWidth;

    public double LInspectorSourceHeight => lInspectorSourceHeight;

    public bool LInspectorSourcePresent => lInspectorSourcePresent;

    public bool LInspectorCropCapable => lInspectorCropCapable;

    public bool LInspectorOrientationCapable => lInspectorOrientationCapable;

    public bool LInspectorToolArmed => lInspectorToolArmed;

    public bool LInspectorSaveSuspended => lInspectorSaveDepth > 0;

    public string? LInspectorOwnerPath => lInspectorOwnerPath;

    public void LInspectorSaveSuspend() => lInspectorSaveDepth++;

    public void LInspectorSaveResume() => lInspectorSaveDepth = Math.Max(0, lInspectorSaveDepth - 1);

    public void LInspectorMinimizedSet(bool lMinimized)
    {
        if (lInspectorMinimized == lMinimized)
        {
            return;
        }

        lInspectorMinimized = lMinimized;
        LInspectorChange?.Invoke();
    }

    public void LInspectorStepSet(string? lStep)
    {
        if (lInspectorStep == lStep)
        {
            return;
        }

        lInspectorStep = lStep;
        LInspectorChange?.Invoke();
    }

    public void LInspectorSourceSet(double lSourceWidth, double lSourceHeight)
    {
        bool lPresent = lSourceWidth > 0 && lSourceHeight > 0;
        double lWidth = lPresent ? lSourceWidth : 0;
        double lHeight = lPresent ? lSourceHeight : 0;
        if (lInspectorSourcePresent == lPresent
            && lInspectorSourceWidth == lWidth
            && lInspectorSourceHeight == lHeight)
        {
            return;
        }

        lInspectorSourcePresent = lPresent;
        lInspectorSourceWidth = lWidth;
        lInspectorSourceHeight = lHeight;
        LInspectorChange?.Invoke();
    }

    public void LInspectorCapableSet(bool lCropCapable, bool lOrientationCapable)
    {
        if (lInspectorCropCapable == lCropCapable && lInspectorOrientationCapable == lOrientationCapable)
        {
            return;
        }

        lInspectorCropCapable = lCropCapable;
        lInspectorOrientationCapable = lOrientationCapable;
        LInspectorChange?.Invoke();
    }

    public void LInspectorToolSet(bool lToolArmed)
    {
        if (lInspectorToolArmed == lToolArmed)
        {
            return;
        }

        lInspectorToolArmed = lToolArmed;
        LInspectorChange?.Invoke();
    }

    public bool LInspectorOwnerSet(string lOwnerPath)
    {
        bool lOwnerFirst = lInspectorOwnerPath is null;
        lInspectorOwnerPath = lOwnerPath;
        return lOwnerFirst;
    }

    public bool LInspectorFailureSet(string? lFailurePath)
    {
        if (lFailurePath is null)
        {
            lInspectorFailurePath = null;
            return false;
        }

        if (string.Equals(lInspectorFailurePath, lFailurePath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        lInspectorFailurePath = lFailurePath;
        return true;
    }

    public LWorkCrop LInspectorCropRead() => LCropbox.LCropboxEdgeNormalize(
        LInspectorCropbox.LCropboxStateCrop, lInspectorSourceWidth, lInspectorSourceHeight);

    public LCropbox? LInspectorRectRead() => LCropbox.LCropboxRectResolve(
        LInspectorCropbox.LCropboxStateCrop, lInspectorSourceWidth, lInspectorSourceHeight);

    public LRotateFlip LInspectorRotateRead() => LRotateFlip.LRotateCropResolve(LInspectorCropbox.LCropboxStateCrop);

    public void LInspectorCropApply(LWorkCrop lCrop, bool lApply)
    {
        LInspectorEdgeLock.LCropboxEdgeClear();
        LInspectorCropbox.LCropboxCropSet(lCrop);
        LInspectorCropbox.LCropboxApplySet(lApply);
    }

    public void LInspectorRatioApply(bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight)
    {
        bool lValid = lRatioWidth > 0 && lRatioHeight > 0;
        LInspectorCropbox.LCropboxRatioSet(
            lRatioFixed && lValid, lRatioLenient && lRatioFixed && lValid, lRatioWidth, lRatioHeight);
    }

    public void LInspectorCropReset()
    {
        if (LInspectorCropbox.LCropboxStatePersistent)
        {
            return;
        }

        LInspectorEdgeLock.LCropboxEdgeClear();
        LInspectorToolSet(false);
        LInspectorCropbox.LCropboxStateReset();
    }

    public LWorkVideoStep LInspectorStepRead(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindGamma => LInspectorGamma.LGammaStep,
        LColorKind.LColorKindWhitebalance => LInspectorWhitebalance.LWhitebalanceStep,
        LColorKind.LColorKindExposure => LInspectorExposure.LExposureStep,
        LColorKind.LColorKindCurve => LInspectorCurve.LCurveStepRead(),
        _ => LInspectorTone.LToneStepRead(lKind)
    };

    public void LInspectorVideoApply(LWorkVideo lVideo)
    {
        LInspectorTone.LToneStepSet(LInspectorStepFind(lVideo, LColorKind.LColorKindBrightness)
            ?? LWorkVideoStep.LWorkBrightnessCreate(false, 0));
        LInspectorTone.LToneStepSet(LInspectorStepFind(lVideo, LColorKind.LColorKindContrast)
            ?? LWorkVideoStep.LWorkContrastCreate(false, 100));
        LInspectorTone.LToneStepSet(LInspectorStepFind(lVideo, LColorKind.LColorKindSaturation)
            ?? LWorkVideoStep.LWorkSaturationCreate(false, 100));
        LInspectorGamma.LGammaStepSet(LInspectorStepFind(lVideo, LColorKind.LColorKindGamma)
            ?? LWorkVideoStep.LWorkGammaCreate(false, 0));
        LInspectorWhitebalance.LWhitebalanceStepSet(LInspectorStepFind(lVideo, LColorKind.LColorKindWhitebalance)
            ?? LWorkVideoStep.LWorkWhitebalanceCreate(false));
        LInspectorExposure.LExposureStepSet(LInspectorStepFind(lVideo, LColorKind.LColorKindExposure)
            ?? LWorkVideoStep.LWorkExposureCreate(false, 0));
        LInspectorCurve.LCurveStepSet(LInspectorStepFind(lVideo, LColorKind.LColorKindCurve)
            ?? LWorkVideoStep.LWorkCurveCreate(false));
    }

    public bool LInspectorPersistentCheck() => lInspectorKinds.Any(LInspectorPersistentCheck);

    public bool LInspectorPersistentCheck(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindGamma => LInspectorGamma.LGammaPersistent,
        LColorKind.LColorKindWhitebalance => LInspectorWhitebalance.LWhitebalancePersistent,
        LColorKind.LColorKindExposure => LInspectorExposure.LExposurePersistent,
        LColorKind.LColorKindCurve => LInspectorCurve.LCurvePersistent,
        _ => LInspectorTone.LTonePersistentRead(lKind)
    };

    public void LInspectorPersistentSet(LColorKind lKind, bool lPersistent)
    {
        switch (lKind)
        {
            case LColorKind.LColorKindGamma:
                LInspectorGamma.LGammaPersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindWhitebalance:
                LInspectorWhitebalance.LWhitebalancePersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindExposure:
                LInspectorExposure.LExposurePersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindCurve:
                LInspectorCurve.LCurvePersistentSet(lPersistent);
                break;
            default:
                LInspectorTone.LTonePersistentSet(lKind, lPersistent);
                break;
        }
    }

    public void LInspectorPersistentApply(LWorkVideo lVideo)
    {
        foreach (LWorkVideoStep lStep in lVideo.LWorkVideoSteps)
        {
            LInspectorPersistentSet(lStep.LWorkStepKind, true);
        }
    }

    public LWorkVideo LInspectorPersistentRead() =>
        new(lInspectorKinds.Where(LInspectorPersistentCheck).Select(LInspectorStepRead).ToList());

    private static LWorkVideoStep? LInspectorStepFind(LWorkVideo lVideo, LColorKind lKind) =>
        lVideo.LWorkVideoSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}
