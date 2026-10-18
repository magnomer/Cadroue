using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The flow's wiring: hands every input to the flow gate, relays its answers to the parts and holds no flow state.
// The range passes through here only for the readers not yet moved to a gate.
public sealed class QFlow
{
    private readonly CFlow qFlowGate;
    private readonly CKeyframe qFlowKeyframeGate;
    private readonly CWaveform qFlowWaveformGate;

    public QFlow(CUsher lUsher, CFlow lGate)
    {
        qFlowGate = lGate;
        qFlowKeyframeGate = lGate.CFlowKeyframe;
        qFlowWaveformGate = lGate.CFlowWaveform;
        QFlowEdit = new QFlowEdit(this);
        QFlowSpool = new QFlowSpool(this);
        QFlowStamp = new QFlowStamp();
        QFlowSection = new QFlowSection(this, lGate.CFlowSection);
        QFlowKeyframe = new QFlowKeyframe(lGate.CFlowKeyframe);
        QFlowWaveform = new QFlowWaveform(lGate.CFlowWaveform);
        QFlowLosslesscut = new QFlowLosslesscut(this, lUsher);
        QFlowName = new QFlowName(this, lGate.CFlowName);
        QFlowWheel = new QFlowWheel(this, lGate);
        QFlowViewfinder = new QViewfinder(this);
        QFlowMap = new QMap(this, lGate.CFlowMap);
        QFlowDivider = new QDivider();
        QFlowGateAttach();
    }

    public event Action<bool>? QFlowWaveformChanged;
    public event Action? QFlowMediaChanged;
    public event Action<TimeSpan>? QFlowCursorChanged;
    public event Action? QFlowPlayed;
    public event Action? QFlowPaused;
    public event Action? QFlowAttachApplied;
    public event Action? QFlowClearApplied;
    public event Action? QFlowCursorApplied;

    public QFlowSection QFlowSection { get; }

    public QFlowKeyframe QFlowKeyframe { get; }

    public QFlowWaveform QFlowWaveform { get; }

    public QFlowLosslesscut QFlowLosslesscut { get; }

    public QFlowName QFlowName { get; }

    public QFlowWheel QFlowWheel { get; }

    public QViewfinder QFlowViewfinder { get; }

    public QMap QFlowMap { get; }

    public QDivider QFlowDivider { get; }

    public QFlowEdit QFlowEdit { get; }

    public QFlowSpool QFlowSpool { get; }

    public QFlowStamp QFlowStamp { get; }

    public LSpool? QFlowRange => qFlowGate.CFlowRange;

    public string? QFlowSourcePath => qFlowGate.CFlowSource;

    public TimeSpan QFlowCursor => qFlowGate.CFlowCursor;

    public bool QFlowSectionActive => qFlowGate.CFlowSectionActive;

    public bool QFlowCommandActive => qFlowGate.CFlowCommandActive;

    public bool QFlowUnloaded => qFlowGate.CFlowUnloaded;

    public bool QFlowWaveformActive => qFlowGate.CFlowWaveformActive;

    public int QFlowPaletteCount => qFlowGate.CFlowPaletteCount;

    public TimeSpan QFlowDuration => qFlowGate.CFlowDuration;

    public int QFlowMapRow => qFlowGate.CFlowMapFirst ? 1 : 3;

    public int QFlowViewfinderRow => qFlowGate.CFlowMapFirst ? 3 : 1;

    public double QFlowHeight => qFlowGate.CFlowHeight;

    public bool QFlowSourceCheck() => qFlowGate.CFlowSourceCheck();

    public bool QFlowScanCheck() => qFlowGate.CFlowScanCheck();

    public string QFlowSourceFormat() => qFlowGate.CFlowSourceFormat();

    public void QFlowPaletteApply(int lPaletteCount) => qFlowGate.CFlowPaletteApply(lPaletteCount);

    private void QFlowGateAttach()
    {
        qFlowGate.CFlowWaveformChanged += QFlowWaveformRaise;
        qFlowGate.CFlowPlayed += QFlowPlayedRaise;
        qFlowGate.CFlowPaused += QFlowPausedRaise;
        qFlowGate.CFlowCursorMoved += QFlowCursorRaise;
        qFlowGate.CFlowCursorSought += QFlowSeekRaise;
    }

    private void QFlowWaveformRaise(bool lWaveformActive) => QFlowWaveformChanged?.Invoke(lWaveformActive);

    private void QFlowPlayedRaise() => QFlowPlayed?.Invoke();

    private void QFlowPausedRaise() => QFlowPaused?.Invoke();

    private void QFlowCursorRaise() => QFlowCursorApplied?.Invoke();

    private void QFlowSeekRaise(TimeSpan lCursor) => QFlowCursorChanged?.Invoke(lCursor);

    public void QFlowAttach(LMediaInfo lMediaInfo, string? lSourcePath, TimeSpan lCursorTime)
    {
        if (!qFlowGate.CFlowAttach(lMediaInfo, lSourcePath, lCursorTime))
        {
            return;
        }

        QFlowSection.QFlowSectionAttach();
        QFlowAttachApplied?.Invoke();
        QFlowSection.QFlowSectionLoad();
        QFlowMediaChanged?.Invoke();
        qFlowKeyframeGate.CKeyframeRun();
        qFlowWaveformGate.CWaveformStart();
        if (qFlowGate.CFlowResumed)
        {
            QFlowCursorChanged?.Invoke(qFlowGate.CFlowCursor);
        }
    }

    public bool QFlowClear()
    {
        if (!qFlowGate.CFlowClear(QFlowSection.QFlowEmptyCheck()))
        {
            return false;
        }

        QFlowRangeClear();
        QFlowSection.QFlowSectionReset();
        QFlowClearApplied?.Invoke();
        QFlowSection.QFlowSectionRaise();
        QFlowMediaChanged?.Invoke();
        return true;
    }

    public void QFlowClose()
    {
        if (!qFlowGate.CFlowClose())
        {
            return;
        }

        QFlowName.QFlowNameHide();
    }

    public void QFlowSourceSet(LMediaInfo lMediaInfo, string? lSourcePath) =>
        qFlowGate.CFlowSourceSet(lMediaInfo, lSourcePath);

    public bool QFlowSourceMatch(string? lSourcePath) => qFlowGate.CFlowSourceMatch(lSourcePath);

    public void QFlowSourceClear()
    {
        qFlowGate.CFlowSourceClear();
        QFlowRangeClear();
    }

    private void QFlowRangeClear() => QFlowEdit.QFlowSectionSelect(null);

    public void QFlowCursorSet(TimeSpan lCursor) => qFlowGate.CFlowCursorSet(lCursor);

    public void QFlowCursorUpdate(TimeSpan lCursor) => qFlowGate.CFlowCursorUpdate(lCursor);

    public void QFlowCursorSeek(TimeSpan lCursor) => qFlowGate.CFlowCursorSeek(lCursor);

    public void QFlowCursorPropagate(TimeSpan lCursor, bool lViewerSeek, bool lKeyframeRestart) =>
        qFlowGate.CFlowCursorPropagate(lCursor, lViewerSeek, lKeyframeRestart);

    public void QFlowPlayRaise() => qFlowGate.CFlowPlayRaise();

    public void QFlowPauseRaise() => qFlowGate.CFlowPauseRaise();

    public bool QFlowShortcutRun(string lToken)
    {
        switch (qFlowGate.CFlowActionResolve(lToken, QFlowEdit.QFlowEditCheck()))
        {
            case CFlowAction.CFlowActionInward: QFlowWheel.QFlowZoom(1); return true;
            case CFlowAction.CFlowActionOutward: QFlowWheel.QFlowZoom(-1); return true;
            case CFlowAction.CFlowActionAddition: QFlowSection.QFlowSectionAdd(); return true;
            case CFlowAction.CFlowActionOrigin: QFlowSection.QFlowStartSet(); return true;
            case CFlowAction.CFlowActionSplit: QFlowSection.QFlowSectionDivide(); return true;
            case CFlowAction.CFlowActionEnd: QFlowSection.QFlowEndSet(); return true;
            case CFlowAction.CFlowActionRemoval: QFlowSection.QFlowSectionDelete(); return true;
            case CFlowAction.CFlowActionRename: return QFlowName.QFlowNameStart();
            case CFlowAction.CFlowActionPrevious: qFlowKeyframeGate.CKeyframePreviousMove(); return true;
            case CFlowAction.CFlowActionNearest: qFlowKeyframeGate.CKeyframeNearestMove(); return true;
            case CFlowAction.CFlowActionNext: qFlowKeyframeGate.CKeyframeNextMove(); return true;
            default: return false;
        }
    }

    public void QFlowSectionSet(bool lSectionActive) => qFlowGate.CFlowSectionSet(lSectionActive);

    public void QFlowCommandSet(bool lCommandActive) => qFlowGate.CFlowCommandSet(lCommandActive);

    public void QFlowUnloadSet() => qFlowGate.CFlowUnloadSet();

    public bool QFlowWaveformSet(bool lWaveformActive) => qFlowGate.CFlowWaveformSet(lWaveformActive);
}
