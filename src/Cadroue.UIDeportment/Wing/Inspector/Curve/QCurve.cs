using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The curve section's wiring: the gate holds the switch, shown channel and point, channel points and keep switch;
// the histogram and what the GUI editor can run stay here. QCurveDrag owns the GUI-only drag lifecycle.
public sealed class QCurve
{
    private readonly CCurve qCurveGate;
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

    public int QCurveChannel => qCurveGate.CCurveChannel;

    public int QCurveSelected => qCurveGate.CCurveSelected;

    public bool QCurvePersistent => qCurveGate.CCurvePersistent;

    public bool QCurveCapable => qCurveCapable;

    public bool QCurvePreview => qCurvePreview;

    public LHistogramCounts? QCurveHistogram => qCurveHistogram;

    public IReadOnlyList<LWorkCurvePoint> QCurvePoints => QCurvePointsRead(qCurveGate.CCurveChannel);

    public double QCurveInputPercent => qCurveGate.CCurveInputPercent;

    public double QCurveOutputPercent => qCurveGate.CCurveOutputPercent;

    public LWorkVideoStep QCurveStepRead()
    {
        (bool active, IReadOnlyList<IReadOnlyList<(double, double)>> channels) = qCurveGate.CCurveStepRead();
        return LWorkVideoStep.LWorkCurveCreate(
            active,
            QCurvePointsRead(channels[0]),
            QCurvePointsRead(channels[1]),
            QCurvePointsRead(channels[2]),
            QCurvePointsRead(channels[3]));
    }

    public void QCurveStepSet(LWorkVideoStep lStep) =>
        qCurveGate.CCurveStepSet(lStep.LWorkStepActive, QCurveChannelsRead(lStep.LWorkCurveRead()));

    public void QCurveActiveSet(bool lActive) => qCurveGate.CCurveActiveSet(lActive);

    public void QCurveChannelSelect(int lChannel) => qCurveGate.CCurveChannelSelect(lChannel);

    internal IReadOnlyList<(double, double)> QCurveTrackRead(int lSampleCount) =>
        qCurveGate.CCurveTrackRead(lSampleCount);

    internal void QCurvePressHandle(double lInput, double lOutput, double lTolerance) =>
        qCurveGate.CCurvePressHandle(lInput, lOutput, lTolerance);

    internal bool QCurveMoveHandle(double lInput, double lOutput, bool lActive) =>
        qCurveGate.CCurveMove(lInput, lOutput, lActive);

    public void QCurvePointCommit(string lInputText, string lOutputText) =>
        qCurveGate.CCurvePointCommit(lInputText, lOutputText);

    public void QCurvePointDelete() => qCurveGate.CCurvePointDelete();

    public void QCurveChannelReset() => qCurveGate.CCurveChannelReset(qCurveGate.CCurveChannel);

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

    private void QCurveGateHandle() => QCurveChanged?.Invoke();

    private static (double, double)[][] QCurveChannelsRead(LWorkCurveSettings lCurve) =>
        new[]
        {
            QCurvePairsRead(lCurve.LWorkCurveMaster),
            QCurvePairsRead(lCurve.LWorkCurveRed),
            QCurvePairsRead(lCurve.LWorkCurveGreen),
            QCurvePairsRead(lCurve.LWorkCurveBlue)
        };

    private List<LWorkCurvePoint> QCurvePointsRead(int lChannel) =>
        QCurvePointsRead(qCurveGate.CCurveChannels[lChannel]);

    private static List<LWorkCurvePoint> QCurvePointsRead(IReadOnlyList<(double, double)> lPoints) =>
        lPoints.Select(lPoint => new LWorkCurvePoint(lPoint.Item1, lPoint.Item2)).ToList();

    private static (double, double)[] QCurvePairsRead(IEnumerable<LWorkCurvePoint> lPoints) =>
        lPoints.Select(lPoint => (lPoint.LWorkCurveInput, lPoint.LWorkCurveOutput)).ToArray();
}
