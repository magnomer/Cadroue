using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

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
    private readonly QInspectorPersistence qInspectorPersistence;
    private string? qInspectorOwnerPath;
    private string? qInspectorFailurePath;

    public QInspector(CInspector lInspector)
    {
        QInspectorGate = lInspector;
        QInspectorVideo = new QInspectorVideo(QInspectorGate);
        QInspectorCrop = new QInspectorCrop(this);
        QInspectorOrientation = new QInspectorOrientation(this);
        QInspectorRatio = new QInspectorRatio(this);
        QInspectorSkip = new QSkip(QInspectorGate.CInspectorSkip);
        QInspectorAudio = new QInspectorAudio(QInspectorGate);
        QInspectorAttach();
        qInspectorPersistence = new QInspectorPersistence(this);
    }

    public event Action? QInspectorChanged;

    // Raised once when the set of kept tools changes; the persistence watch decides it.
    public event Action? QInspectorPersistentChanged
    {
        add => qInspectorPersistence.QInspectorPersistenceChanged += value;
        remove => qInspectorPersistence.QInspectorPersistenceChanged -= value;
    }

    public event Action<bool>? QInspectorToolChanged;
    public event Action<bool>? QInspectorMinimizeChanged;

    public QInspectorCrop QInspectorCrop { get; }

    public QInspectorOrientation QInspectorOrientation { get; }

    public QInspectorRatio QInspectorRatio { get; }

    public CInspector QInspectorGate { get; }

    public QInspectorVideo QInspectorVideo { get; }

    public QSkip QInspectorSkip { get; }

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

    public void QInspectorPlanApply(LEditPlan lPlan) =>
        QInspectorPlanApply(new LInspectorOutlet(lPlan), lPlan.LEditVideo);

    // A kept layout's plan: the gate applies the crop only when it is kept and keeps the skip switch as told.
    public void QInspectorPlanApply(LEditPlan lPlan, bool lCropKept, bool lSkipKept) =>
        QInspectorPlanApply(new LInspectorOutlet(lPlan, lCropKept, lSkipKept), lPlan.LEditVideo);

    // One gate call sets the plan; the curve, whose plan step the gate does not read yet, takes its step after.
    private void QInspectorPlanApply(LInspectorPort lPlan, LWorkVideo lVideo)
    {
        QInspectorVideo.QInspectorVideoSuspend();
        try
        {
            QInspectorGate.CInspectorEditAccept(lPlan);
            QInspectorVideo.QInspectorCurveApply(lVideo);
        }
        finally
        {
            QInspectorVideo.QInspectorVideoResume();
        }
    }

    private void QInspectorToolsNormalize()
    {
        if (qInspectorMinimized || qInspectorStep != "Whitebalance")
        {
            QInspectorVideo.QInspectorWhitebalance.CWhitebalanceToolCancel();
        }

        if (qInspectorStep != "Crop")
        {
            QInspectorToolSet(false);
        }
    }

    private static int QInspectorDepthClamp(int lDepth) => Math.Max(0, lDepth);

    private void QInspectorAttach() =>
        QInspectorVideo.QInspectorWhitebalance.CWhitebalanceToolChanged += QInspectorNeutralHandle;

    private void QInspectorNeutralHandle(bool lArmed, bool lWhite)
    {
        if (lArmed)
        {
            QInspectorToolSet(false);
        }
    }
}
