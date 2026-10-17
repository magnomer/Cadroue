using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QCurve
{
    private readonly List<LWorkCurvePoint>[] qCurveChannels =
    {
        QCurveIdentityCreate(),
        QCurveIdentityCreate(),
        QCurveIdentityCreate(),
        QCurveIdentityCreate()
    };

    private bool qCurveActive;
    private int qCurveChannel;
    private int qCurveSelected = 1;
    private bool qCurvePersistent;
    private bool qCurveCapable;
    private bool qCurvePreview;
    private LHistogramCounts? qCurveHistogram;
    private bool qCurveDragActive;

    public QCurve()
    {
        QCurveCanvas = new QCurveCanvas(this);
    }

    public event Action? QCurveChanged;

    public QCurveCanvas QCurveCanvas { get; }

    public bool QCurveActive => qCurveActive;

    public int QCurveChannel => qCurveChannel;

    public int QCurveSelected => qCurveSelected;

    public bool QCurvePersistent => qCurvePersistent;

    public bool QCurveCapable => qCurveCapable;

    public bool QCurvePreview => qCurvePreview;

    public LHistogramCounts? QCurveHistogram => qCurveHistogram;

    public bool QCurveDragActive => qCurveDragActive;

    public IReadOnlyList<LWorkCurvePoint> QCurvePoints => qCurveChannels[qCurveChannel];

    public LWorkCurvePoint QCurvePointRead() => qCurveChannels[qCurveChannel][qCurveSelected];

    public double QCurveInputPercent => QCurvePointRead().LWorkCurveInput * 100;

    public double QCurveOutputPercent => QCurvePointRead().LWorkCurveOutput * 100;

    public LWorkVideoStep QCurveStepRead() => LWorkVideoStep.LWorkCurveCreate(
        qCurveActive,
        qCurveChannels[0].ToArray(),
        qCurveChannels[1].ToArray(),
        qCurveChannels[2].ToArray(),
        qCurveChannels[3].ToArray());

    public void QCurveStepSet(LWorkVideoStep lStep)
    {
        LWorkCurveSettings lCurve = lStep.LWorkCurveRead();
        bool lPointsChanged = QCurvePointsCheck(lCurve);
        if (!lPointsChanged && qCurveActive == lStep.LWorkStepActive)
        {
            return;
        }

        QCurveStepApply(lStep.LWorkStepActive, lPointsChanged ? lCurve : null);
    }

    private bool QCurvePointsCheck(LWorkCurveSettings lCurve) =>
        !qCurveChannels[0].SequenceEqual(lCurve.LWorkCurveMaster)
        || !qCurveChannels[1].SequenceEqual(lCurve.LWorkCurveRed)
        || !qCurveChannels[2].SequenceEqual(lCurve.LWorkCurveGreen)
        || !qCurveChannels[3].SequenceEqual(lCurve.LWorkCurveBlue);

    private void QCurveStepApply(bool lActive, LWorkCurveSettings? lCurve)
    {
        qCurveActive = lActive;
        if (lCurve is not null)
        {
            qCurveChannels[0] = lCurve.LWorkCurveMaster.ToList();
            qCurveChannels[1] = lCurve.LWorkCurveRed.ToList();
            qCurveChannels[2] = lCurve.LWorkCurveGreen.ToList();
            qCurveChannels[3] = lCurve.LWorkCurveBlue.ToList();
            QCurveSelectedClamp();
        }

        QCurveChanged?.Invoke();
    }

    public void QCurveActiveSet(bool lActive)
    {
        if (qCurveActive == lActive)
        {
            return;
        }

        qCurveActive = lActive;
        QCurveChanged?.Invoke();
    }

    public void QCurveChannelSelect(int lChannel)
    {
        if (QCurveChannelResolve(lChannel) is not { } lClamped)
        {
            return;
        }

        QCurveChannelChange(lClamped);
    }

    private int? QCurveChannelResolve(int lChannel) =>
        lChannel < 0 ? null : Math.Clamp(lChannel, 0, qCurveChannels.Length - 1);

    private void QCurveChannelChange(int lClamped)
    {
        if (qCurveChannel == lClamped)
        {
            return;
        }

        qCurveChannel = lClamped;
        QCurveSelectedClamp();
        QCurveChanged?.Invoke();
    }

    public void QCurvePointSelect(int lIndex) => QCurveSelectedChange(QCurveIndexClamp(lIndex));

    private int QCurveIndexClamp(int lIndex) => Math.Clamp(lIndex, 0, QCurvePoints.Count - 1);

    private void QCurveSelectedChange(int lClamped)
    {
        if (qCurveSelected == lClamped)
        {
            return;
        }

        qCurveSelected = lClamped;
        QCurveChanged?.Invoke();
    }

    public void QCurvePointSet(double lInput, double lOutput) =>
        QCurvePointChange(QCurvePointClamp(lInput, lOutput));

    private LWorkCurvePoint QCurvePointClamp(double lInput, double lOutput)
    {
        double lClampedInput = LColorCurve.LColorCurveClamp(
            qCurveChannels[qCurveChannel], qCurveSelected, QCurveUnitClamp(lInput));
        return new LWorkCurvePoint(lClampedInput, QCurveUnitClamp(lOutput));
    }

    private static double QCurveUnitClamp(double lValue) => Math.Clamp(lValue, 0, 1);

    private void QCurvePointChange(LWorkCurvePoint lPoint)
    {
        List<LWorkCurvePoint> lPoints = qCurveChannels[qCurveChannel];
        if (lPoints[qCurveSelected] == lPoint)
        {
            return;
        }

        lPoints[qCurveSelected] = lPoint;
        QCurveChanged?.Invoke();
    }

    public void QCurvePointCommit(string lInputText, string lOutputText)
    {
        LWorkCurvePoint lPoint = QCurvePointRead();
        double lInput = QCurvePercentParse(lInputText, lPoint.LWorkCurveInput);
        double lOutput = QCurvePercentParse(lOutputText, lPoint.LWorkCurveOutput);
        QCurvePointSet(lInput, lOutput);
    }

    private static double QCurvePercentParse(string lText, double lValue) =>
        QInspector.QInspectorValueCommit(lText, lValue * 100, null, null) / 100;

    public void QCurvePressHandle(double lX, double lY, double lSize)
    {
        int lHit = QCurveCanvas.QCurveHitFind(lX, lY, lSize);
        if (lHit < 0)
        {
            (double lInput, double lOutput) = QCurveCanvas.QCurveValueResolve(lX, lY, lSize);
            QCurvePointAdd(lInput, lOutput);
        }
        else
        {
            QCurvePointSelect(lHit);
        }

        QCurveDragSet(true);
    }

    private void QCurveDragSet(bool lActive) => qCurveDragActive = lActive;

    public bool QCurveMoveHandle(double lX, double lY, double lSize)
    {
        if (!qCurveDragActive)
        {
            return false;
        }

        (double lInput, double lOutput) = QCurveCanvas.QCurveValueResolve(lX, lY, lSize);
        QCurvePointSet(lInput, lOutput);
        return true;
    }

    public bool QCurveReleaseHandle()
    {
        if (!qCurveDragActive)
        {
            return false;
        }

        qCurveDragActive = false;
        return true;
    }

    public void QCurvePointAdd(double lInput, double lOutput) =>
        QCurvePointInsert(QCurveUnitClamp(lInput), QCurveUnitClamp(lOutput));

    private void QCurvePointInsert(double lInput, double lOutput)
    {
        qCurveSelected = LColorCurve.LColorCurveInsert(qCurveChannels[qCurveChannel], lInput, lOutput);
        QCurveChanged?.Invoke();
    }

    public void QCurvePointDelete()
    {
        if (!QCurveRemovableCheck())
        {
            return;
        }

        QCurvePointRemove();
    }

    private bool QCurveRemovableCheck()
    {
        List<LWorkCurvePoint> lPoints = qCurveChannels[qCurveChannel];
        return lPoints.Count > 2 && qCurveSelected > 0 && qCurveSelected < lPoints.Count - 1;
    }

    private void QCurvePointRemove()
    {
        qCurveChannels[qCurveChannel].RemoveAt(qCurveSelected);
        QCurveSelectedClamp();
        QCurveChanged?.Invoke();
    }

    public void QCurveChannelReset()
    {
        qCurveChannels[qCurveChannel] = QCurveIdentityCreate();
        QCurveSelectedClamp();
        QCurveChanged?.Invoke();
    }

    public void QCurveReset()
    {
        for (int lIndex = 0; lIndex < qCurveChannels.Length; lIndex++)
        {
            qCurveChannels[lIndex] = QCurveIdentityCreate();
        }

        QCurveSelectedClamp();
        QCurveChanged?.Invoke();
    }

    public void QCurveHistogramSet(LHistogramCounts? lHistogram)
    {
        if (ReferenceEquals(qCurveHistogram, lHistogram))
        {
            return;
        }

        qCurveHistogram = lHistogram;
        QCurveChanged?.Invoke();
    }

    public void QCurvePersistentSet(bool lPersistent)
    {
        if (qCurvePersistent == lPersistent)
        {
            return;
        }

        qCurvePersistent = lPersistent;
        QCurveChanged?.Invoke();
    }

    public void QCurveCapableSet(bool lCapable, bool lPreview) =>
        QCurveCapableChange(lCapable, QCurvePreviewResolve(lCapable, lPreview));

    private static bool QCurvePreviewResolve(bool lCapable, bool lPreview) => lCapable && lPreview;

    private void QCurveCapableChange(bool lCapable, bool lPreviewShown)
    {
        if (qCurveCapable == lCapable && qCurvePreview == lPreviewShown)
        {
            return;
        }

        qCurveCapable = lCapable;
        qCurvePreview = lPreviewShown;
        QCurveChanged?.Invoke();
    }

    private void QCurveSelectedClamp() => qCurveSelected = QCurveIndexClamp(qCurveSelected);

    private static List<LWorkCurvePoint> QCurveIdentityCreate() =>
        new() { new LWorkCurvePoint(0, 0), new LWorkCurvePoint(1, 1) };
}
