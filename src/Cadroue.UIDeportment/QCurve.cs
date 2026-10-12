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
        bool lPointsChanged = !qCurveChannels[0].SequenceEqual(lCurve.LWorkCurveMaster)
            || !qCurveChannels[1].SequenceEqual(lCurve.LWorkCurveRed)
            || !qCurveChannels[2].SequenceEqual(lCurve.LWorkCurveGreen)
            || !qCurveChannels[3].SequenceEqual(lCurve.LWorkCurveBlue);
        if (!lPointsChanged && qCurveActive == lStep.LWorkStepActive)
        {
            return;
        }

        qCurveActive = lStep.LWorkStepActive;
        if (lPointsChanged)
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
        if (lChannel < 0)
        {
            return;
        }

        int lClamped = Math.Clamp(lChannel, 0, qCurveChannels.Length - 1);
        if (qCurveChannel == lClamped)
        {
            return;
        }

        qCurveChannel = lClamped;
        QCurveSelectedClamp();
        QCurveChanged?.Invoke();
    }

    public void QCurvePointSelect(int lIndex)
    {
        int lClamped = Math.Clamp(lIndex, 0, QCurvePoints.Count - 1);
        if (qCurveSelected == lClamped)
        {
            return;
        }

        qCurveSelected = lClamped;
        QCurveChanged?.Invoke();
    }

    public void QCurvePointSet(double lInput, double lOutput)
    {
        List<LWorkCurvePoint> lPoints = qCurveChannels[qCurveChannel];
        double lClampedInput = LColorCurve.LColorCurveClamp(lPoints, qCurveSelected, Math.Clamp(lInput, 0, 1));
        var lPoint = new LWorkCurvePoint(lClampedInput, Math.Clamp(lOutput, 0, 1));
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
        double lInput = QInspector.QInspectorValueCommit(lInputText, lPoint.LWorkCurveInput * 100, null, null) / 100;
        double lOutput = QInspector.QInspectorValueCommit(lOutputText, lPoint.LWorkCurveOutput * 100, null, null) / 100;
        QCurvePointSet(lInput, lOutput);
    }

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

        qCurveDragActive = true;
    }

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

    public void QCurvePointAdd(double lInput, double lOutput)
    {
        qCurveSelected = LColorCurve.LColorCurveInsert(
            qCurveChannels[qCurveChannel], Math.Clamp(lInput, 0, 1), Math.Clamp(lOutput, 0, 1));
        QCurveChanged?.Invoke();
    }

    public void QCurvePointDelete()
    {
        List<LWorkCurvePoint> lPoints = qCurveChannels[qCurveChannel];
        if (lPoints.Count <= 2 || qCurveSelected <= 0 || qCurveSelected >= lPoints.Count - 1)
        {
            return;
        }

        lPoints.RemoveAt(qCurveSelected);
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

    public void QCurveCapableSet(bool lCapable, bool lPreview)
    {
        bool lPreviewShown = lCapable && lPreview;
        if (qCurveCapable == lCapable && qCurvePreview == lPreviewShown)
        {
            return;
        }

        qCurveCapable = lCapable;
        qCurvePreview = lPreviewShown;
        QCurveChanged?.Invoke();
    }

    private void QCurveSelectedClamp() =>
        qCurveSelected = Math.Clamp(qCurveSelected, 0, QCurvePoints.Count - 1);

    private static List<LWorkCurvePoint> QCurveIdentityCreate() =>
        new() { new LWorkCurvePoint(0, 0), new LWorkCurvePoint(1, 1) };
}
