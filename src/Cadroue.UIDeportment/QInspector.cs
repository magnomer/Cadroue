using System.Globalization;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QInspector
{
    private static readonly LColorKind[] qInspectorKinds =
    {
        LColorKind.LColorKindBrightness,
        LColorKind.LColorKindContrast,
        LColorKind.LColorKindSaturation,
        LColorKind.LColorKindGamma,
        LColorKind.LColorKindWhitebalance,
        LColorKind.LColorKindExposure,
        LColorKind.LColorKindCurve
    };

    private static readonly IReadOnlyDictionary<string, string> qInspectorTitles = new Dictionary<string, string>
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

    private static readonly IReadOnlyDictionary<LDetectorKind, string> qInspectorSensorTitles =
        new Dictionary<LDetectorKind, string>
        {
            [LDetectorKind.LDetectorKindBlank] = "Inspector.Step.Blank",
            [LDetectorKind.LDetectorKindScene] = "Inspector.Step.Scene",
            [LDetectorKind.LDetectorKindStill] = "Inspector.Step.Still",
            [LDetectorKind.LDetectorKindLuminance] = "Inspector.Step.Luminance",
            [LDetectorKind.LDetectorKindSilence] = "Inspector.Step.Silence",
            [LDetectorKind.LDetectorKindVolume] = "Inspector.Step.Volume"
        };

    private bool qInspectorMinimized;
    private string? qInspectorStep;
    private double qInspectorSourceWidth = 1920;
    private double qInspectorSourceHeight = 1080;
    private bool qInspectorSourcePresent;
    private bool qInspectorCropCapable = true;
    private bool qInspectorOrientationCapable = true;
    private bool qInspectorToolArmed;
    private int qInspectorSaveDepth;
    private int qInspectorVideoDepth;
    private bool qInspectorVideoPending;
    private int qInspectorPersistentMask;
    private string? qInspectorOwnerPath;
    private string? qInspectorFailurePath;

    public QInspector()
    {
        QInspectorCrop = new QInspectorCrop(this);
        QInspectorAudio = new QInspectorAudio(QInspectorSkip);
        QInspectorTone.QToneChanged += QInspectorVideoHandle;
        QInspectorGamma.QGammaChanged += QInspectorVideoHandle;
        QInspectorExposure.QExposureChanged += QInspectorVideoHandle;
        QInspectorCurve.QCurveChanged += QInspectorVideoHandle;
        QInspectorWhitebalance.QWhitebalanceChanged += QInspectorVideoHandle;
        QInspectorWhitebalance.QWhitebalanceToolChanged += QInspectorNeutralHandle;
        QInspectorCrop.QInspectorCropbox.LCropboxStateChanged += QInspectorPersistentHandle;
        QInspectorAudio.QInspectorAudioChanged += QInspectorPersistentHandle;
        QInspectorSkip.QSkipChanged += QInspectorPersistentHandle;
        qInspectorPersistentMask = QInspectorPersistentResolve();
    }

    public event Action? QInspectorChanged;
    public event Action? QInspectorVideoChanged;
    public event Action? QInspectorPersistentChanged;
    public event Action<bool>? QInspectorToolChanged;
    public event Action<bool>? QInspectorMinimizeChanged;

    public QInspectorCrop QInspectorCrop { get; }

    public QTone QInspectorTone { get; } = new();

    public QGamma QInspectorGamma { get; } = new();

    public QExposure QInspectorExposure { get; } = new();

    public QCurve QInspectorCurve { get; } = new();

    public QWhitebalance QInspectorWhitebalance { get; } = new();

    public QSkip QInspectorSkip { get; } = new();

    public QInspectorAudio QInspectorAudio { get; }

    public QSensor QInspectorSensor { get; } = new();

    public QBlank QInspectorBlank { get; } = new();

    public bool QInspectorMinimized => qInspectorMinimized;

    public string? QInspectorStep => qInspectorStep;

    public double QInspectorSourceWidth => qInspectorSourceWidth;

    public double QInspectorSourceHeight => qInspectorSourceHeight;

    public bool QInspectorSourcePresent => qInspectorSourcePresent;

    public bool QInspectorCropCapable => qInspectorCropCapable;

    public bool QInspectorOrientationCapable => qInspectorOrientationCapable;

    public bool QInspectorToolArmed => qInspectorToolArmed;

    public bool QInspectorSaveSuspended => qInspectorSaveDepth > 0;

    public string? QInspectorOwnerPath => qInspectorOwnerPath;

    public bool QInspectorPersistentShown => qInspectorTitles.Keys.Any(QInspectorSectionCheck);

    public bool QInspectorEmptyShown => !QInspectorPersistentShown && QSensor.QSensorKindRead(qInspectorStep) is null;

    public bool QInspectorSectionCheck(string lKey) =>
        qInspectorStep == lKey && !(lKey == "Volume" && QSensor.QSensorKindRead(qInspectorStep) is not null);

    public bool QInspectorSensorCheck(LDetectorKind lKind) => QSensor.QSensorKindRead(qInspectorStep) == lKind;

    public string QInspectorTitleRead() => LLocalization.LLocalizationTextRead(
        QSensor.QSensorKindRead(qInspectorStep) is { } lKind
            ? qInspectorSensorTitles[lKind]
            : qInspectorTitles.GetValueOrDefault(qInspectorStep ?? string.Empty, "Inspector.Header.Title"));

    public void QInspectorSaveSuspend() => qInspectorSaveDepth++;

    public void QInspectorSaveResume() => qInspectorSaveDepth = Math.Max(0, qInspectorSaveDepth - 1);

    public void QInspectorMinimizedSet(bool lMinimized)
    {
        if (qInspectorMinimized == lMinimized)
        {
            return;
        }

        qInspectorMinimized = lMinimized;
        QInspectorToolsNormalize();
        QInspectorChanged?.Invoke();
        QInspectorMinimizeChanged?.Invoke(lMinimized);
    }

    public void QInspectorStepSet(string? lStep)
    {
        if (qInspectorStep == lStep)
        {
            return;
        }

        qInspectorStep = lStep;
        QInspectorToolsNormalize();
        QInspectorChanged?.Invoke();
    }

    public void QInspectorSourceSet(double lSourceWidth, double lSourceHeight)
    {
        bool lPresent = lSourceWidth > 0 && lSourceHeight > 0;
        double lWidth = lPresent ? lSourceWidth : 0;
        double lHeight = lPresent ? lSourceHeight : 0;
        if (qInspectorSourcePresent == lPresent
            && qInspectorSourceWidth == lWidth
            && qInspectorSourceHeight == lHeight)
        {
            return;
        }

        qInspectorSourcePresent = lPresent;
        qInspectorSourceWidth = lWidth;
        qInspectorSourceHeight = lHeight;
        QInspectorChanged?.Invoke();
    }

    public void QInspectorCapableSet(bool lCropCapable, bool lOrientationCapable)
    {
        if (qInspectorCropCapable == lCropCapable && qInspectorOrientationCapable == lOrientationCapable)
        {
            return;
        }

        qInspectorCropCapable = lCropCapable;
        qInspectorOrientationCapable = lOrientationCapable;
        QInspectorChanged?.Invoke();
    }

    public void QInspectorToolSet(bool lToolArmed)
    {
        if (qInspectorToolArmed == lToolArmed)
        {
            return;
        }

        qInspectorToolArmed = lToolArmed;
        QInspectorChanged?.Invoke();
        QInspectorToolChanged?.Invoke(lToolArmed);
    }

    public bool QInspectorOwnerSet(string lOwnerPath)
    {
        bool lOwnerFirst = qInspectorOwnerPath is null;
        qInspectorOwnerPath = lOwnerPath;
        return lOwnerFirst;
    }

    public bool QInspectorFailureSet(string? lFailurePath)
    {
        if (lFailurePath is null)
        {
            qInspectorFailurePath = null;
            return false;
        }

        if (string.Equals(qInspectorFailurePath, lFailurePath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        qInspectorFailurePath = lFailurePath;
        return true;
    }

    public LWorkVideoStep QInspectorStepRead(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindGamma => QInspectorGamma.QGammaStep,
        LColorKind.LColorKindWhitebalance => QInspectorWhitebalance.QWhitebalanceStep,
        LColorKind.LColorKindExposure => QInspectorExposure.QExposureStep,
        LColorKind.LColorKindCurve => QInspectorCurve.QCurveStepRead(),
        _ => QInspectorTone.QToneStepRead(lKind)
    };

    public void QInspectorPlanApply(LEditPlan lPlan)
    {
        qInspectorVideoDepth++;
        try
        {
            QInspectorCrop.QInspectorCropApply(lPlan.LEditCrop, lPlan.LEditCropActive);
            QInspectorCrop.QInspectorRatioApply(
                lPlan.LEditRatioFixed, lPlan.LEditRatioLenient, lPlan.LEditRatioWidth, lPlan.LEditRatioHeight);
            QInspectorVideoApply(lPlan.LEditVideo);
            QInspectorSkip.QSkipActiveSet(lPlan.LEditSkip);
        }
        finally
        {
            QInspectorVideoRelease();
        }
    }

    public void QInspectorVideoApply(LWorkVideo lVideo)
    {
        qInspectorVideoDepth++;
        try
        {
            QInspectorTone.QToneStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindBrightness)
                ?? LWorkVideoStep.LWorkBrightnessCreate(false, 0));
            QInspectorTone.QToneStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindContrast)
                ?? LWorkVideoStep.LWorkContrastCreate(false, 100));
            QInspectorTone.QToneStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindSaturation)
                ?? LWorkVideoStep.LWorkSaturationCreate(false, 100));
            QInspectorGamma.QGammaStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindGamma)
                ?? LWorkVideoStep.LWorkGammaCreate(false, 0));
            QInspectorWhitebalance.QWhitebalanceStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindWhitebalance)
                ?? LWorkVideoStep.LWorkWhitebalanceCreate(false));
            QInspectorExposure.QExposureStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindExposure)
                ?? LWorkVideoStep.LWorkExposureCreate(false, 0));
            QInspectorCurve.QCurveStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindCurve)
                ?? LWorkVideoStep.LWorkCurveCreate(false));
            qInspectorVideoPending = true;
        }
        finally
        {
            QInspectorVideoRelease();
        }
    }

    public bool QInspectorPersistentCheck() => qInspectorKinds.Any(QInspectorPersistentCheck);

    public bool QInspectorPersistentCheck(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindGamma => QInspectorGamma.QGammaPersistent,
        LColorKind.LColorKindWhitebalance => QInspectorWhitebalance.QWhitebalancePersistent,
        LColorKind.LColorKindExposure => QInspectorExposure.QExposurePersistent,
        LColorKind.LColorKindCurve => QInspectorCurve.QCurvePersistent,
        _ => QInspectorTone.QTonePersistentRead(lKind)
    };

    public void QInspectorPersistentSet(LColorKind lKind, bool lPersistent)
    {
        switch (lKind)
        {
            case LColorKind.LColorKindGamma:
                QInspectorGamma.QGammaPersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindWhitebalance:
                QInspectorWhitebalance.QWhitebalancePersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindExposure:
                QInspectorExposure.QExposurePersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindCurve:
                QInspectorCurve.QCurvePersistentSet(lPersistent);
                break;
            default:
                QInspectorTone.QTonePersistentSet(lKind, lPersistent);
                break;
        }
    }

    public void QInspectorPersistentApply(LWorkVideo lVideo)
    {
        foreach (LWorkVideoStep lStep in lVideo.LWorkVideoSteps)
        {
            QInspectorPersistentSet(lStep.LWorkStepKind, true);
        }
    }

    public LWorkVideo QInspectorPersistentRead() =>
        new(qInspectorKinds.Where(QInspectorPersistentCheck).Select(QInspectorStepRead).ToList());

    public QInspectorTip QInspectorTipResolve(
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
        return new QInspectorTip(
            lCapable && lActive,
            lNotice,
            lNotice ?? LLocalization.LLocalizationTextRead(lApplyKey),
            lNotice ?? LLocalization.LLocalizationTextRead(lPersistKey));
    }

    public string QInspectorNoticeRead(QInspectorTip lTip, string lKey) =>
        lTip.QInspectorTipNotice ?? LLocalization.LLocalizationTextRead(lKey);

    public static bool QInspectorDigitCheck(string lText) => lText.All(char.IsDigit);

    public static bool QInspectorDecimalCheck(string lText) =>
        lText.All(lChar => char.IsDigit(lChar) || lChar == '.' || lChar == '-');

    public static double QInspectorValueCommit(string lText, double lCurrent, double? lLeast, double? lMost)
    {
        double lParsed = double.TryParse(lText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lValue)
            ? lValue
            : lCurrent;
        return lLeast is double lMin && lMost is double lMax ? Math.Clamp(lParsed, lMin, lMax) : lParsed;
    }

    public static string QInspectorValueFormat(string lText, double lNumber, string lFormat)
    {
        string lShown = lNumber.ToString(lFormat, CultureInfo.InvariantCulture);
        bool lSame = lText == lShown
            || (double.TryParse(lText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lParsed)
                && lParsed == lNumber);
        return lSame ? lText : lShown;
    }

    public static void QInspectorSlideCommit(
        double lSlider, double lCurrent, double lLeast, double lMost, Action<double> lSet)
    {
        if (lSlider != Math.Clamp(lCurrent, lLeast, lMost))
        {
            lSet(lSlider);
        }
    }

    private void QInspectorToolsNormalize()
    {
        if (qInspectorMinimized || qInspectorStep != "Whitebalance")
        {
            QInspectorWhitebalance.QWhitebalanceToolSet(false, QInspectorWhitebalance.QWhitebalanceTarget);
        }

        if (qInspectorStep != "Crop")
        {
            QInspectorToolSet(false);
        }
    }

    private void QInspectorNeutralHandle(bool lArmed, LNeutralTarget lTarget)
    {
        if (lArmed)
        {
            QInspectorToolSet(false);
        }
    }

    private void QInspectorVideoHandle()
    {
        QInspectorPersistentHandle();
        if (qInspectorVideoDepth > 0)
        {
            qInspectorVideoPending = true;
            return;
        }

        QInspectorVideoChanged?.Invoke();
    }

    private void QInspectorVideoRelease()
    {
        qInspectorVideoDepth--;
        if (qInspectorVideoDepth > 0 || !qInspectorVideoPending)
        {
            return;
        }

        qInspectorVideoPending = false;
        QInspectorVideoChanged?.Invoke();
    }

    private void QInspectorPersistentHandle()
    {
        int lMask = QInspectorPersistentResolve();
        if (lMask == qInspectorPersistentMask)
        {
            return;
        }

        qInspectorPersistentMask = lMask;
        QInspectorPersistentChanged?.Invoke();
    }

    private int QInspectorPersistentResolve()
    {
        bool[] lFlags =
        {
            QInspectorCrop.QInspectorPersistent,
            QInspectorTone.QTonePersistentRead(LColorKind.LColorKindBrightness),
            QInspectorTone.QTonePersistentRead(LColorKind.LColorKindContrast),
            QInspectorTone.QTonePersistentRead(LColorKind.LColorKindSaturation),
            QInspectorGamma.QGammaPersistent,
            QInspectorExposure.QExposurePersistent,
            QInspectorCurve.QCurvePersistent,
            QInspectorWhitebalance.QWhitebalancePersistent,
            QInspectorAudio.QInspectorVolume.QVolumePersistent,
            QInspectorAudio.QInspectorLoudness.QLoudnessPersistent,
            QInspectorAudio.QInspectorNoise.QNoisePersistent,
            QInspectorAudio.QInspectorHighpass.QFilterPersistent,
            QInspectorAudio.QInspectorLowpass.QFilterPersistent,
            QInspectorAudio.QInspectorEqualizer.QEqualizerPersistent,
            QInspectorSkip.QSkipPersistent
        };
        return lFlags.Select((lFlag, lIndex) => lFlag ? 1 << lIndex : 0).Sum();
    }

    private static LWorkVideoStep? QInspectorStepFind(LWorkVideo lVideo, LColorKind lKind) =>
        lVideo.LWorkVideoSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}

public sealed record QInspectorTip(
    bool QInspectorTipEnabled,
    string? QInspectorTipNotice,
    string QInspectorTipBox,
    string QInspectorTipPersistent);
