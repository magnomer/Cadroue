using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QInspector
{
    private double qInspectorSourceWidth = 1920;
    private double qInspectorSourceHeight = 1080;
    private bool qInspectorSourcePresent;
    private bool qInspectorCropCapable = true;
    private bool qInspectorOrientationCapable = true;
    private int qInspectorSaveDepth;
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
    }

    public event Action? QInspectorChanged;

    // Raised once when the set of kept tools changes; Conduct decides it.
    public event Action? QInspectorPersistentChanged
    {
        add => QInspectorGate.CInspectorPersistentChanged += value;
        remove => QInspectorGate.CInspectorPersistentChanged -= value;
    }

    public event Action<bool>? QInspectorToolChanged
    {
        add => QInspectorGate.CInspectorFrame.CInspectorToolChanged += value;
        remove => QInspectorGate.CInspectorFrame.CInspectorToolChanged -= value;
    }

    public event Action<bool>? QInspectorMinimizeChanged
    {
        add => QInspectorGate.CInspectorFrame.CInspectorMinimizeChanged += value;
        remove => QInspectorGate.CInspectorFrame.CInspectorMinimizeChanged -= value;
    }

    public QInspectorCrop QInspectorCrop { get; }

    public QInspectorOrientation QInspectorOrientation { get; }

    public QInspectorRatio QInspectorRatio { get; }

    public CInspector QInspectorGate { get; }

    public QInspectorVideo QInspectorVideo { get; }

    public QSkip QInspectorSkip { get; }

    public QInspectorAudio QInspectorAudio { get; }

    public QSensor QInspectorSensor { get; } = new();

    public QBlank QInspectorBlank { get; } = new();

    public bool QInspectorMinimized => QInspectorGate.CInspectorFrame.CInspectorMinimized;

    public double QInspectorSourceWidth => qInspectorSourceWidth;

    public double QInspectorSourceHeight => qInspectorSourceHeight;

    public bool QInspectorSourcePresent => qInspectorSourcePresent;

    public bool QInspectorCropCapable => qInspectorCropCapable;

    public bool QInspectorOrientationCapable => qInspectorOrientationCapable;

    public bool QInspectorToolArmed => QInspectorGate.CInspectorFrame.CInspectorToolArmed;

    public bool QInspectorSaveSuspended => qInspectorSaveDepth > 0;

    public string? QInspectorOwnerPath => qInspectorOwnerPath;

    public bool QInspectorPersistentShown => QInspectorGate.CInspectorFrame.CInspectorPersistentShown;

    public bool QInspectorEmptyShown => QInspectorGate.CInspectorFrame.CInspectorEmptyShown;

    public bool QInspectorSectionCheck(string lKey) => QInspectorGate.CInspectorFrame.CInspectorSectionCheck(lKey);

    public bool QInspectorSensorCheck(LDetectorKind lKind) =>
        QInspectorGate.CInspectorFrame.CInspectorSensorCheck(QSensorName.QSensorNameRead(lKind));

    public string QInspectorTitleRead() =>
        QLocalizationCatalog.QLocalizationTextRead(QInspectorGate.CInspectorFrame.CInspectorTitleKey);

    public void QInspectorSaveSuspend() => qInspectorSaveDepth++;

    public void QInspectorSaveResume() => qInspectorSaveDepth = QInspectorDepthClamp(qInspectorSaveDepth - 1);

    public void QInspectorMinimizedSet(bool lMinimized) =>
        QInspectorGate.CInspectorFrame.CInspectorMinimizedSet(lMinimized);

    public void QInspectorStepSet(string? lStep) => QInspectorGate.CInspectorFrame.CInspectorStepSet(lStep);

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

    public void QInspectorToolSet(bool lToolArmed) => QInspectorGate.CInspectorFrame.CInspectorToolSet(lToolArmed);

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

    private static int QInspectorDepthClamp(int lDepth) => Math.Max(0, lDepth);

    private void QInspectorAttach() =>
        QInspectorGate.CInspectorFrame.CInspectorChanged += QInspectorChangeHandle;

    private void QInspectorChangeHandle() => QInspectorChanged?.Invoke();
}
