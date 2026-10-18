using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The curve section's wiring: the gate holds the switch, the points and the keep switch; the channel and point shown,
// the histogram and what the editor can run stay here; QCurveDrag owns the drag. A point edit is resolved here and
// handed to the gate as the channel's new points.
public sealed class QCurve
{
    private readonly CCurve qCurveGate;
    private int qCurveChannel;
    private int qCurveSelected = 1;
    private bool qCurveCapable;
    private bool qCurvePreview;
    private LHistogramCounts? qCurveHistogram;

    public QCurve(CCurve lCurve)
    {
        qCurveGate = lCurve;
        QCurveCanvas = new QCurveCanvas(this);
        QCurveDrag = new QCurveDrag(this);
        qCurveGate.CCurveChanged += QCurveGateHandle;
    }

    public event Action? QCurveChanged;

    public QCurveCanvas QCurveCanvas { get; }

    public QCurveDrag QCurveDrag { get; }

    public bool QCurveActive => qCurveGate.CCurveActive;

    public int QCurveChannel => qCurveChannel;

    public int QCurveSelected => qCurveSelected;

    public bool QCurvePersistent => qCurveGate.CCurvePersistent;

    public bool QCurveCapable => qCurveCapable;

    public bool QCurvePreview => qCurvePreview;

    public LHistogramCounts? QCurveHistogram => qCurveHistogram;

    public IReadOnlyList<LWorkCurvePoint> QCurvePoints => QCurvePointsRead(qCurveChannel);

    public LWorkCurvePoint QCurvePointRead() => QCurvePoints[qCurveSelected];

    public double QCurveInputPercent => QCurvePointRead().LWorkCurveInput * 100;

    public double QCurveOutputPercent => QCurvePointRead().LWorkCurveOutput * 100;

    public LWorkVideoStep QCurveStepRead() => LWorkVideoStep.LWorkCurveCreate(
        qCurveGate.CCurveActive, QCurvePointsRead(0), QCurvePointsRead(1), QCurvePointsRead(2), QCurvePointsRead(3));

    // The step's points, held as the step holds them, go to the gate with its switch.
    public void QCurveStepSet(LWorkVideoStep lStep) =>
        qCurveGate.CCurveStepSet(lStep.LWorkStepActive, QCurveChannelsRead(lStep.LWorkCurveRead()));

    public void QCurveActiveSet(bool lActive) => qCurveGate.CCurveActiveSet(lActive);

    public void QCurveChannelSelect(int lChannel)
    {
        if (QCurveChannelResolve(lChannel) is not { } lClamped)
        {
            return;
        }

        QCurveChannelChange(lClamped);
    }

    private int? QCurveChannelResolve(int lChannel) =>
        lChannel < 0 ? null : Math.Clamp(lChannel, 0, qCurveGate.CCurveChannels.Count - 1);

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
        double lClampedInput = LColorCurve.LColorCurveClamp(QCurvePoints, qCurveSelected, QCurveUnitClamp(lInput));
        return new LWorkCurvePoint(lClampedInput, QCurveUnitClamp(lOutput));
    }

    private static double QCurveUnitClamp(double lValue) => Math.Clamp(lValue, 0, 1);

    private void QCurvePointChange(LWorkCurvePoint lPoint)
    {
        if (QCurvePointRead() == lPoint)
        {
            return;
        }

        QCurveChannelApply(lPoints => lPoints[qCurveSelected] = lPoint);
    }

    public void QCurvePointCommit(string lInputText, string lOutputText)
    {
        LWorkCurvePoint lPoint = QCurvePointRead();
        double lInput = QCurvePercentParse(lInputText, lPoint.LWorkCurveInput);
        double lOutput = QCurvePercentParse(lOutputText, lPoint.LWorkCurveOutput);
        QCurvePointSet(lInput, lOutput);
    }

    private static double QCurvePercentParse(string lText, double lValue) =>
        QInspectorField.QInspectorValueCommit(lText, lValue * 100, null, null) / 100;

    public void QCurvePointAdd(double lInput, double lOutput) =>
        QCurvePointInsert(QCurveUnitClamp(lInput), QCurveUnitClamp(lOutput));

    private void QCurvePointInsert(double lInput, double lOutput)
    {
        QCurveChannelApply(lPoints => qCurveSelected = LColorCurve.LColorCurveInsert(lPoints, lInput, lOutput));
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
        IReadOnlyList<LWorkCurvePoint> lPoints = QCurvePoints;
        return lPoints.Count > 2 && qCurveSelected > 0 && qCurveSelected < lPoints.Count - 1;
    }

    private void QCurvePointRemove()
    {
        QCurveChannelApply(lPoints => lPoints.RemoveAt(qCurveSelected));
    }

    public void QCurveChannelReset() => qCurveGate.CCurveChannelReset(qCurveChannel);

    public void QCurveReset() => qCurveGate.CCurveReset();

    public void QCurveHistogramSet(LHistogramCounts? lHistogram)
    {
        if (ReferenceEquals(qCurveHistogram, lHistogram))
        {
            return;
        }

        qCurveHistogram = lHistogram;
        QCurveChanged?.Invoke();
    }

    public void QCurvePersistentSet(bool lPersistent) => qCurveGate.CCurvePersistentSet(lPersistent);

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

    // Any change of the gate's points or switches keeps the shown point inside its channel, then is told on.
    private void QCurveGateHandle()
    {
        QCurveSelectedClamp();
        QCurveChanged?.Invoke();
    }

    // The shown channel's points take the edit, then go to the gate whole.
    private void QCurveChannelApply(Action<List<LWorkCurvePoint>> lEdit)
    {
        List<LWorkCurvePoint> lPoints = QCurvePointsRead(qCurveChannel);
        lEdit(lPoints);
        qCurveGate.CCurveChannelSet(qCurveChannel, QCurvePairsRead(lPoints));
    }

    private static (double, double)[][] QCurveChannelsRead(LWorkCurveSettings lCurve) =>
        new[]
        {
            QCurvePairsRead(lCurve.LWorkCurveMaster),
            QCurvePairsRead(lCurve.LWorkCurveRed),
            QCurvePairsRead(lCurve.LWorkCurveGreen),
            QCurvePairsRead(lCurve.LWorkCurveBlue)
        };

    private List<LWorkCurvePoint> QCurvePointsRead(int lChannel) =>
        qCurveGate.CCurveChannels[lChannel].Select(lPoint => new LWorkCurvePoint(lPoint.Item1, lPoint.Item2)).ToList();

    private static (double, double)[] QCurvePairsRead(IEnumerable<LWorkCurvePoint> lPoints) =>
        lPoints.Select(lPoint => (lPoint.LWorkCurveInput, lPoint.LWorkCurveOutput)).ToArray();
}
