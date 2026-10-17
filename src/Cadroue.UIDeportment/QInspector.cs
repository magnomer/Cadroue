using System.Globalization;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QInspector
{
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
    private int qInspectorPersistentMask;
    private string? qInspectorOwnerPath;
    private string? qInspectorFailurePath;

    public QInspector(CInspector lInspector)
    {
        QInspectorGate = lInspector;
        QInspectorVideo = new QInspectorVideo(QInspectorGate);
        QInspectorCrop = new QInspectorCrop(this);
        QInspectorOrientation = new QInspectorOrientation(this);
        QInspectorRatio = new QInspectorRatio(this, QInspectorCrop);
        QInspectorAudio = new QInspectorAudio(QInspectorSkip, QInspectorGate);
        QInspectorAttach();
        qInspectorPersistentMask = QInspectorPersistentResolve();
    }

    public event Action? QInspectorChanged;
    public event Action? QInspectorPersistentChanged;
    public event Action<bool>? QInspectorToolChanged;
    public event Action<bool>? QInspectorMinimizeChanged;

    public QInspectorCrop QInspectorCrop { get; }

    public QInspectorOrientation QInspectorOrientation { get; }

    public QInspectorRatio QInspectorRatio { get; }

    public CInspector QInspectorGate { get; }

    public QInspectorVideo QInspectorVideo { get; }

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

    public string QInspectorTitleRead() => QLocalizationCatalog.QLocalizationTextRead(
        QSensor.QSensorKindRead(qInspectorStep) is { } lKind
            ? qInspectorSensorTitles[lKind]
            : qInspectorTitles.GetValueOrDefault(qInspectorStep ?? string.Empty, "Inspector.Header.Title"));

    public void QInspectorSaveSuspend() => qInspectorSaveDepth++;

    public void QInspectorSaveResume() => qInspectorSaveDepth = QInspectorDepthClamp(qInspectorSaveDepth - 1);

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
        (bool lPresent, double lWidth, double lHeight) = QInspectorSourceNormalize(lSourceWidth, lSourceHeight);
        QInspectorSourceApply(lPresent, lWidth, lHeight);
    }

    private static (bool, double, double) QInspectorSourceNormalize(double lSourceWidth, double lSourceHeight)
    {
        bool lPresent = lSourceWidth > 0 && lSourceHeight > 0;
        return (lPresent, lPresent ? lSourceWidth : 0, lPresent ? lSourceHeight : 0);
    }

    private void QInspectorSourceApply(bool lPresent, double lWidth, double lHeight)
    {
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

    public void QInspectorPlanApply(LEditPlan lPlan)
    {
        QInspectorVideo.QInspectorVideoSuspend();
        try
        {
            QInspectorCrop.QInspectorCropApply(lPlan.LEditCrop, lPlan.LEditCropActive);
            QInspectorRatio.QInspectorRatioApply(
                lPlan.LEditRatioFixed, lPlan.LEditRatioLenient, lPlan.LEditRatioWidth, lPlan.LEditRatioHeight);
            QInspectorVideo.QInspectorVideoApply(lPlan.LEditVideo);
            QInspectorSkip.QSkipActiveSet(lPlan.LEditSkip);
        }
        finally
        {
            QInspectorVideo.QInspectorVideoResume();
        }
    }

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
            ? QLocalizationCatalog.QLocalizationTextRead(lDisabledKey)
            : !lPreviewAvailable && lPreviewKey.Length > 0
                ? QLocalizationCatalog.QLocalizationTextRead(lPreviewKey)
                : null;
        return new QInspectorTip(
            lCapable && lActive,
            lNotice,
            lNotice ?? QLocalizationCatalog.QLocalizationTextRead(lApplyKey),
            lNotice ?? QLocalizationCatalog.QLocalizationTextRead(lPersistKey));
    }

    public string QInspectorNoticeRead(QInspectorTip lTip, string lKey) =>
        lTip.QInspectorTipNotice ?? QLocalizationCatalog.QLocalizationTextRead(lKey);

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
            QInspectorVideo.QInspectorWhitebalance.QWhitebalanceToolSet(
                false, QInspectorVideo.QInspectorWhitebalance.QWhitebalanceTarget);
        }

        if (qInspectorStep != "Crop")
        {
            QInspectorToolSet(false);
        }
    }

    private static int QInspectorDepthClamp(int lDepth) => Math.Max(0, lDepth);

    private void QInspectorAttach()
    {
        QInspectorVideo.QInspectorSectionChanged += QInspectorPersistentHandle;
        QInspectorVideo.QInspectorWhitebalance.QWhitebalanceToolChanged += QInspectorNeutralHandle;
        QInspectorCrop.QInspectorCropbox.LCropboxStateChanged += QInspectorPersistentHandle;
        QInspectorAudio.QInspectorAudioChanged += QInspectorPersistentHandle;
        QInspectorSkip.QSkipChanged += QInspectorPersistentHandle;
    }

    private void QInspectorNeutralHandle(bool lArmed, LNeutralTarget lTarget)
    {
        if (lArmed)
        {
            QInspectorToolSet(false);
        }
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
            QInspectorVideo.QInspectorTone.QTonePersistentRead(LColorKind.LColorKindBrightness),
            QInspectorVideo.QInspectorTone.QTonePersistentRead(LColorKind.LColorKindContrast),
            QInspectorVideo.QInspectorTone.QTonePersistentRead(LColorKind.LColorKindSaturation),
            QInspectorVideo.QInspectorGamma.CGammaPersistent,
            QInspectorVideo.QInspectorExposure.CExposurePersistent,
            QInspectorVideo.QInspectorCurve.QCurvePersistent,
            QInspectorVideo.QInspectorWhitebalance.QWhitebalancePersistent,
            QInspectorAudio.QInspectorVolume.CVolumePersistent,
            QInspectorAudio.QInspectorLoudness.QLoudnessPersistent,
            QInspectorAudio.QInspectorNoise.QNoisePersistent,
            QInspectorAudio.QInspectorHighpass.QFilterPersistent,
            QInspectorAudio.QInspectorLowpass.QFilterPersistent,
            QInspectorAudio.QInspectorEqualizer.QEqualizerPersistent,
            QInspectorSkip.QSkipPersistent
        };
        return lFlags.Select((lFlag, lIndex) => lFlag ? 1 << lIndex : 0).Sum();
    }
}

public sealed record QInspectorTip(
    bool QInspectorTipEnabled,
    string? QInspectorTipNotice,
    string QInspectorTipBox,
    string QInspectorTipPersistent);
