using System.Globalization;
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

    private static readonly IReadOnlyDictionary<string, string> lInspectorTitles = new Dictionary<string, string>
    {
        ["Crop"] = "Inspector.Step.Crop",
        ["Brightness"] = "Inspector.Step.Brightness",
        ["Contrast"] = "Inspector.Step.Contrast",
        ["Saturation"] = "Inspector.Step.Saturation",
        ["Gamma"] = "Inspector.Step.Gamma",
        ["Exposure"] = "Inspector.Step.Exposure",
        ["Curve"] = "Inspector.Step.Curve",
        ["Whitebalance"] = "Inspector.Step.Whitebalance",
        ["Volume"] = "Inspector.Step.Volume",
        ["Normalize"] = "Inspector.Step.Normalize",
        ["Noise Reduction"] = "Inspector.Step.NoiseReduction",
        ["High Pass"] = "Inspector.Step.HighPass",
        ["Low Pass"] = "Inspector.Step.LowPass",
        ["Equalizer"] = "Inspector.Step.Equalizer",
        ["No Processing"] = "Inspector.Step.NoProcessing"
    };

    private static readonly IReadOnlyDictionary<LDetectorKind, string> lInspectorSensorTitles =
        new Dictionary<LDetectorKind, string>
        {
            [LDetectorKind.LDetectorKindBlank] = "Inspector.Step.Blank",
            [LDetectorKind.LDetectorKindScene] = "Inspector.Step.Scene",
            [LDetectorKind.LDetectorKindStill] = "Inspector.Step.Still",
            [LDetectorKind.LDetectorKindLuminance] = "Inspector.Step.Luminance",
            [LDetectorKind.LDetectorKindSilence] = "Inspector.Step.Silence",
            [LDetectorKind.LDetectorKindVolume] = "Inspector.Step.Volume"
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
    private int lInspectorVideoDepth;
    private bool lInspectorVideoPending;
    private int lInspectorPersistentMask;
    private string? lInspectorOwnerPath;
    private string? lInspectorFailurePath;

    public LInspector()
    {
        LInspectorCrop = new LInspectorCrop(this);
        LInspectorAudio = new LInspectorAudio(LInspectorSkip);
        LInspectorTone.LToneChange += LInspectorVideoHandle;
        LInspectorGamma.LGammaChange += LInspectorVideoHandle;
        LInspectorExposure.LExposureChange += LInspectorVideoHandle;
        LInspectorCurve.LCurveChange += LInspectorVideoHandle;
        LInspectorWhitebalance.LWhitebalanceChange += LInspectorVideoHandle;
        LInspectorWhitebalance.LWhitebalanceToolChange += LInspectorNeutralHandle;
        LInspectorCrop.LInspectorCropbox.LCropboxStateChange += LInspectorPersistentHandle;
        LInspectorAudio.LInspectorAudioChange += LInspectorPersistentHandle;
        LInspectorSkip.LSkipChange += LInspectorPersistentHandle;
        lInspectorPersistentMask = LInspectorPersistentResolve();
    }

    public event Action? LInspectorChange;
    public event Action? LInspectorVideoChange;
    public event Action? LInspectorPersistentChange;
    public event Action<bool>? LInspectorToolChange;
    public event Action<bool>? LInspectorMinimizeChange;

    public LInspectorCrop LInspectorCrop { get; }

    public LTone LInspectorTone { get; } = new();

    public LGamma LInspectorGamma { get; } = new();

    public LExposure LInspectorExposure { get; } = new();

    public LCurve LInspectorCurve { get; } = new();

    public LWhitebalance LInspectorWhitebalance { get; } = new();

    public LSkip LInspectorSkip { get; } = new();

    public LInspectorAudio LInspectorAudio { get; }

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

    public bool LInspectorPersistentShown => lInspectorTitles.Keys.Any(LInspectorSectionCheck);

    public bool LInspectorEmptyShown => !LInspectorPersistentShown && LSensor.LSensorKindRead(lInspectorStep) is null;

    public bool LInspectorSectionCheck(string lKey) =>
        lInspectorStep == lKey && !(lKey == "Volume" && LSensor.LSensorKindRead(lInspectorStep) is not null);

    public bool LInspectorSensorCheck(LDetectorKind lKind) => LSensor.LSensorKindRead(lInspectorStep) == lKind;

    public string LInspectorTitleRead() => LLocalization.LLocalizationTextRead(
        LSensor.LSensorKindRead(lInspectorStep) is { } lKind
            ? lInspectorSensorTitles[lKind]
            : lInspectorTitles.GetValueOrDefault(lInspectorStep ?? string.Empty, "Inspector.Header.Title"));

    public void LInspectorSaveSuspend() => lInspectorSaveDepth++;

    public void LInspectorSaveResume() => lInspectorSaveDepth = Math.Max(0, lInspectorSaveDepth - 1);

    public void LInspectorMinimizedSet(bool lMinimized)
    {
        if (lInspectorMinimized == lMinimized)
        {
            return;
        }

        lInspectorMinimized = lMinimized;
        LInspectorToolsNormalize();
        LInspectorChange?.Invoke();
        LInspectorMinimizeChange?.Invoke(lMinimized);
    }

    public void LInspectorStepSet(string? lStep)
    {
        if (lInspectorStep == lStep)
        {
            return;
        }

        lInspectorStep = lStep;
        LInspectorToolsNormalize();
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
        LInspectorToolChange?.Invoke(lToolArmed);
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

    public LWorkVideoStep LInspectorStepRead(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindGamma => LInspectorGamma.LGammaStep,
        LColorKind.LColorKindWhitebalance => LInspectorWhitebalance.LWhitebalanceStep,
        LColorKind.LColorKindExposure => LInspectorExposure.LExposureStep,
        LColorKind.LColorKindCurve => LInspectorCurve.LCurveStepRead(),
        _ => LInspectorTone.LToneStepRead(lKind)
    };

    public void LInspectorPlanApply(LEditPlan lPlan)
    {
        lInspectorVideoDepth++;
        try
        {
            LInspectorCrop.LInspectorCropApply(lPlan.LEditCrop, lPlan.LEditCropActive);
            LInspectorCrop.LInspectorRatioApply(
                lPlan.LEditRatioFixed, lPlan.LEditRatioLenient, lPlan.LEditRatioWidth, lPlan.LEditRatioHeight);
            LInspectorVideoApply(lPlan.LEditVideo);
            LInspectorSkip.LSkipActiveSet(lPlan.LEditSkip);
        }
        finally
        {
            LInspectorVideoRelease();
        }
    }

    public void LInspectorVideoApply(LWorkVideo lVideo)
    {
        lInspectorVideoDepth++;
        try
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
            lInspectorVideoPending = true;
        }
        finally
        {
            LInspectorVideoRelease();
        }
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

    public LInspectorTip LInspectorTipResolve(
        bool lActive,
        bool lCapable,
        bool lPreviewAvailable,
        string lDisabledKey,
        string lPreviewKey,
        string lApplyKey,
        string lPersistKey)
    {
        string? lNotice = !lCapable
            ? LLocalization.LLocalizationTextRead(lDisabledKey)
            : !lPreviewAvailable && lPreviewKey.Length > 0
                ? LLocalization.LLocalizationTextRead(lPreviewKey)
                : null;
        return new LInspectorTip(
            lCapable && lActive,
            lNotice,
            lNotice ?? LLocalization.LLocalizationTextRead(lApplyKey),
            lNotice ?? LLocalization.LLocalizationTextRead(lPersistKey));
    }

    public string LInspectorNoticeRead(LInspectorTip lTip, string lKey) =>
        lTip.LInspectorTipNotice ?? LLocalization.LLocalizationTextRead(lKey);

    public static bool LInspectorDigitCheck(string lText) => lText.All(char.IsDigit);

    public static bool LInspectorDecimalCheck(string lText) =>
        lText.All(lChar => char.IsDigit(lChar) || lChar == '.' || lChar == '-');

    public static double LInspectorValueCommit(string lText, double lCurrent, double? lLeast, double? lMost)
    {
        double lParsed = double.TryParse(lText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lValue)
            ? lValue
            : lCurrent;
        return lLeast is double lMin && lMost is double lMax ? Math.Clamp(lParsed, lMin, lMax) : lParsed;
    }

    public static string LInspectorValueFormat(string lText, double lNumber, string lFormat)
    {
        string lShown = lNumber.ToString(lFormat, CultureInfo.InvariantCulture);
        bool lSame = lText == lShown
            || (double.TryParse(lText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lParsed)
                && lParsed == lNumber);
        return lSame ? lText : lShown;
    }

    public static void LInspectorSlideCommit(
        double lSlider, double lCurrent, double lLeast, double lMost, Action<double> lSet)
    {
        if (lSlider != Math.Clamp(lCurrent, lLeast, lMost))
        {
            lSet(lSlider);
        }
    }

    private void LInspectorToolsNormalize()
    {
        if (lInspectorMinimized || lInspectorStep != "Whitebalance")
        {
            LInspectorWhitebalance.LWhitebalanceToolSet(false, LInspectorWhitebalance.LWhitebalanceTarget);
        }

        if (lInspectorStep != "Crop")
        {
            LInspectorToolSet(false);
        }
    }

    private void LInspectorNeutralHandle(bool lArmed, LNeutralTarget lTarget)
    {
        if (lArmed)
        {
            LInspectorToolSet(false);
        }
    }

    private void LInspectorVideoHandle()
    {
        LInspectorPersistentHandle();
        if (lInspectorVideoDepth > 0)
        {
            lInspectorVideoPending = true;
            return;
        }

        LInspectorVideoChange?.Invoke();
    }

    private void LInspectorVideoRelease()
    {
        lInspectorVideoDepth--;
        if (lInspectorVideoDepth > 0 || !lInspectorVideoPending)
        {
            return;
        }

        lInspectorVideoPending = false;
        LInspectorVideoChange?.Invoke();
    }

    private void LInspectorPersistentHandle()
    {
        int lMask = LInspectorPersistentResolve();
        if (lMask == lInspectorPersistentMask)
        {
            return;
        }

        lInspectorPersistentMask = lMask;
        LInspectorPersistentChange?.Invoke();
    }

    private int LInspectorPersistentResolve()
    {
        bool[] lFlags =
        {
            LInspectorCrop.LInspectorPersistent,
            LInspectorTone.LTonePersistentRead(LColorKind.LColorKindBrightness),
            LInspectorTone.LTonePersistentRead(LColorKind.LColorKindContrast),
            LInspectorTone.LTonePersistentRead(LColorKind.LColorKindSaturation),
            LInspectorGamma.LGammaPersistent,
            LInspectorExposure.LExposurePersistent,
            LInspectorCurve.LCurvePersistent,
            LInspectorWhitebalance.LWhitebalancePersistent,
            LInspectorAudio.LInspectorVolume.LVolumePersistent,
            LInspectorAudio.LInspectorLoudness.LLoudnessPersistent,
            LInspectorAudio.LInspectorNoise.LNoisePersistent,
            LInspectorAudio.LInspectorHighpass.LFilterPersistent,
            LInspectorAudio.LInspectorLowpass.LFilterPersistent,
            LInspectorAudio.LInspectorEqualizer.LEqualizerPersistent,
            LInspectorSkip.LSkipPersistent
        };
        return lFlags.Select((lFlag, lIndex) => lFlag ? 1 << lIndex : 0).Sum();
    }

    private static LWorkVideoStep? LInspectorStepFind(LWorkVideo lVideo, LColorKind lKind) =>
        lVideo.LWorkVideoSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}

public sealed record LInspectorTip(
    bool LInspectorTipEnabled,
    string? LInspectorTipNotice,
    string LInspectorTipBox,
    string LInspectorTipPersistent);
