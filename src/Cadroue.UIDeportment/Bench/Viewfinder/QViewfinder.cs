using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The viewfinder's draw cycle: asks a new frame on every flow change that alters it, hands the frame from its layout
// and records each draw with its trigger, time and glyph count.
public sealed class QViewfinder
{
    public const double QViewfinderLaneHeight = 20;

    internal const double QViewfinderCoverageHeight = 4;

    private readonly QFlow qFlow;
    private string qViewfinderTrigger = "attach";
    private int qViewfinderGlyphCount;
    private readonly QChronometer qViewfinderChronometer = new();

    public QViewfinder(QFlow lOwner)
    {
        qFlow = lOwner;
        QViewfinderLayout = new QViewfinderLayout(lOwner);
        QViewfinderDrag = new QViewfinderDrag(lOwner);
        QViewfinderTriggersAttach();
    }

    public QViewfinderLayout QViewfinderLayout { get; }

    public QViewfinderDrag QViewfinderDrag { get; }

    // Every flow change that alters the drawing asks a new frame, naming its trigger for the draw record.
    private void QViewfinderTriggersAttach()
    {
        qFlow.QFlowAttachApplied += () => QViewfinderTriggerApply("attach");
        qFlow.QFlowClearApplied += () => QViewfinderTriggerApply("clear");
        qFlow.QFlowCursorApplied += () => QViewfinderTriggerApply("cursor");
        qFlow.QFlowSpool.QFlowSpoolApplied += () => QViewfinderTriggerApply("spool");
        qFlow.QFlowSection.QFlowSectionChanged += (_, _) => QViewfinderTriggerApply("sections");
        qFlow.QFlowKeyframe.QFlowKeyframeChanged += () => QViewfinderTriggerApply("keyframes");
        qFlow.QFlowWaveform.QFlowWaveformUpdated += () => QViewfinderTriggerApply("waveform");
    }

    public event Action? QViewfinderFrameApplied;

    public void QViewfinderDrawStart() => qViewfinderChronometer.QChronometerStart();

    public void QViewfinderDrawRecord()
    {
        LTraceTimeline.LTraceTimelineAdd(
            "Viewfinder",
            qFlow.QFlowCursor,
            qFlow.QFlowSourcePath,
            qViewfinderTrigger,
            qViewfinderChronometer.QChronometerRead(),
            qViewfinderGlyphCount);
    }

    public QViewfinderFrame QViewfinderFrameResolve(double lWidth, double lHeight)
    {
        QViewfinderGlyphSet(0);
        (QViewfinderFrame lFrame, int lGlyphCount) = QViewfinderLayout.QViewfinderLayoutResolve(lWidth, lHeight);
        QViewfinderGlyphSet(lGlyphCount);
        return lFrame;
    }

    private void QViewfinderGlyphSet(int lGlyphCount) => qViewfinderGlyphCount = lGlyphCount;

    private void QViewfinderTriggerApply(string lTrigger)
    {
        QViewfinderTriggerSet(lTrigger);
        QViewfinderFrameApplied?.Invoke();
    }

    private void QViewfinderTriggerSet(string lTrigger) => qViewfinderTrigger = lTrigger;
}
