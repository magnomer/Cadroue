using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QFlow
{
    private readonly CUsher qFlowUsher;
    private LSpool? qFlowRange;
    private LMediaInfo? qFlowMediaInfo;
    private string? qFlowSourcePath;
    private TimeSpan qFlowCursor;
    private bool qFlowSectionActive;
    private bool qFlowCommandActive;
    private bool qFlowUnloaded;
    private bool qFlowDragPaused;
    private bool qFlowWaveformActive = LPreference.LPreferenceStateCurrent.LPreferenceWaveform;
    private bool qFlowWaveformAudio;
    private int? qFlowKeyframeDirection;
    private int qFlowPaletteCount = 1;
    private Func<bool>? qFlowPlayingSource;

    public QFlow(CUsher lUsher)
    {
        qFlowUsher = lUsher;
        QFlowEdit = new QFlowEdit(this);
        QFlowSpool = new QFlowSpool(this);
        QFlowStamp = new QFlowStamp();
        QFlowSection = new QFlowSection(this);
        QFlowKeyframe = new QFlowKeyframe(this, qFlowUsher);
        QFlowWaveform = new QFlowWaveform(this);
        QFlowLosslesscut = new QFlowLosslesscut(this, qFlowUsher);
        QFlowName = new QFlowName(this);
        QFlowViewfinder = new QViewfinder(this);
        QFlowMap = new QMap(this);
        QFlowDivider = new QDivider();
    }

    public event Action<bool>? QFlowWaveformChanged;
    public event Action? QFlowMediaChanged;
    public event Action<TimeSpan>? QFlowCursorChanged;
    public event Action<bool>? QFlowDragChanged;
    public event Action? QFlowPlayed;
    public event Action? QFlowPaused;
    public event Action<double>? QFlowVolumeAdjusted;
    public event Action? QFlowAttachApplied;
    public event Action? QFlowClearApplied;
    public event Action? QFlowCursorApplied;

    public QFlowSection QFlowSection { get; }

    public QFlowKeyframe QFlowKeyframe { get; }

    public QFlowWaveform QFlowWaveform { get; }

    public QFlowLosslesscut QFlowLosslesscut { get; }

    public QFlowName QFlowName { get; }

    public QViewfinder QFlowViewfinder { get; }

    public QMap QFlowMap { get; }

    public QDivider QFlowDivider { get; }

    public QFlowEdit QFlowEdit { get; }

    public QFlowSpool QFlowSpool { get; }

    public QFlowStamp QFlowStamp { get; }

    public LSpool? QFlowRange => qFlowRange;

    public LMediaInfo? QFlowMediaInfo => qFlowMediaInfo;

    public string? QFlowSourcePath => qFlowSourcePath;

    public TimeSpan QFlowCursor => qFlowCursor;

    public bool QFlowSectionActive => qFlowSectionActive;

    public bool QFlowCommandActive => qFlowCommandActive;

    public bool QFlowUnloaded => qFlowUnloaded;

    public bool QFlowDragPaused => qFlowDragPaused;

    public bool QFlowWaveformActive => qFlowWaveformActive;

    public bool QFlowWaveformAudio => qFlowWaveformAudio;

    public int? QFlowKeyframeDirection => qFlowKeyframeDirection;

    public int QFlowPaletteCount => qFlowPaletteCount;

    public TimeSpan QFlowDuration => qFlowRange?.LSpoolDuration ?? TimeSpan.Zero;

    public int QFlowMapRow => QFlowOrderCheck() ? 1 : 3;

    public int QFlowViewfinderRow => QFlowOrderCheck() ? 3 : 1;

    public bool QFlowSourceCheck() =>
        qFlowRange is not null && !string.IsNullOrWhiteSpace(qFlowSourcePath);

    public bool QFlowScanCheck() =>
        qFlowCommandActive && !qFlowUnloaded && qFlowMediaInfo is not null && QFlowSourceCheck();

    public string QFlowSourceFormat() =>
        string.IsNullOrWhiteSpace(qFlowSourcePath) ? "(no media)" : qFlowUsher.CUsherNameRead(qFlowSourcePath);

    public void QFlowPlayingAttach(Func<bool>? lPlayingSource) => qFlowPlayingSource = lPlayingSource;

    public void QFlowPaletteApply(int lPaletteCount) => qFlowPaletteCount = Math.Max(1, lPaletteCount);

    public void QFlowAttach(LMediaInfo lMediaInfo, string? lSourcePath, TimeSpan lCursorTime)
    {
        if (!qFlowCommandActive)
        {
            return;
        }

        QFlowKeyframe.QFlowKeyframeReset();
        QFlowKeyframe.QFlowKeyframeClear();
        bool lSameSource = QFlowSourceMatch(lSourcePath);
        TimeSpan lResumeAt = lSameSource ? qFlowCursor : lCursorTime;
        QFlowSourceSet(lMediaInfo, lSourcePath);
        QFlowCursorSet(lResumeAt);
        QFlowSection.QFlowSectionAttach();
        QFlowAttachApplied?.Invoke();
        QFlowSection.QFlowSectionLoad();
        QFlowMediaChanged?.Invoke();
        QFlowKeyframe.QFlowKeyframeRun();
        QFlowWaveform.QFlowWaveformStart();
        if (lSameSource && qFlowCursor > TimeSpan.Zero)
        {
            QFlowCursorChanged?.Invoke(qFlowCursor);
        }
    }

    public bool QFlowClear()
    {
        if (qFlowSourcePath is null && qFlowRange is null && QFlowSection.QFlowEmptyCheck())
        {
            return false;
        }

        QFlowKeyframe.QFlowKeyframeReset();
        QFlowKeyframe.QFlowKeyframeSuspend(false);
        QFlowKeyframe.QFlowKeyframeClear();
        QFlowSourceClear();
        QFlowSection.QFlowSectionReset();
        QFlowWaveform.QFlowWaveformClear();
        QFlowClearApplied?.Invoke();
        QFlowSection.QFlowSectionRaise();
        QFlowMediaChanged?.Invoke();
        return true;
    }

    public void QFlowClose()
    {
        if (qFlowUnloaded)
        {
            return;
        }

        qFlowUnloaded = true;
        QFlowName.QFlowNameHide();
        QFlowKeyframe.QFlowKeyframeClose();
        QFlowWaveform.QFlowWaveformClose();
    }

    public void QFlowSourceSet(LMediaInfo lMediaInfo, string? lSourcePath)
    {
        qFlowSourcePath = string.IsNullOrWhiteSpace(lSourcePath) ? null : lSourcePath;
        qFlowMediaInfo = lMediaInfo;
        qFlowRange = new LSpool(lMediaInfo.LMediaInfoDuration);
        qFlowWaveformAudio = lMediaInfo.LMediaAudioPresent;
        qFlowKeyframeDirection = null;
    }

    public bool QFlowSourceMatch(string? lSourcePath) =>
        !string.IsNullOrWhiteSpace(lSourcePath)
        && string.Equals(qFlowSourcePath, lSourcePath, StringComparison.OrdinalIgnoreCase);

    public void QFlowSourceClear()
    {
        qFlowSourcePath = null;
        qFlowMediaInfo = null;
        qFlowRange = null;
        qFlowCursor = TimeSpan.Zero;
        QFlowEdit.QFlowSectionSelect(null);
        qFlowKeyframeDirection = null;
    }

    public TimeSpan QFlowCursorClamp(TimeSpan lCursor)
    {
        if (qFlowRange is null || lCursor < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return lCursor > qFlowRange.LSpoolDuration ? qFlowRange.LSpoolDuration : lCursor;
    }

    public void QFlowCursorSet(TimeSpan lCursor) => qFlowCursor = QFlowCursorClamp(lCursor);

    public void QFlowCursorUpdate(TimeSpan lCursor)
    {
        if (!qFlowCommandActive)
        {
            return;
        }

        QFlowCursorPropagate(lCursor, false, false);
        QFlowKeyframe.QFlowKeyframeSync(lCursor);
        QFlowKeyframe.QFlowKeyframeDefer();
    }

    public void QFlowCursorSeek(TimeSpan lCursor)
    {
        if (!qFlowCommandActive)
        {
            return;
        }

        QFlowKeyframe.QFlowKeyframeSuspend(true);
        QFlowCursorPropagate(lCursor, true, false);
    }

    public void QFlowCursorPropagate(TimeSpan lCursor, bool lViewerSeek, bool lKeyframeRestart)
    {
        qFlowKeyframeDirection = null;
        QFlowCursorSet(lCursor);
        QFlowCursorApplied?.Invoke();
        if (lKeyframeRestart)
        {
            QFlowKeyframe.QFlowKeyframeRun();
        }

        if (lViewerSeek)
        {
            QFlowCursorChanged?.Invoke(qFlowCursor);
        }
    }

    public bool QFlowWheelHandle(int lDelta)
    {
        if (!qFlowCommandActive || lDelta == 0)
        {
            return false;
        }

        int lSteps = QCursor.QCursorWheelResolve(lDelta);
        switch (LPreference.LPreferenceStateCurrent.LPreferenceWheelAction)
        {
            case "Zoom":
                QFlowZoom(lSteps);
                break;
            case "Volume":
                QFlowVolumeAdjusted?.Invoke(lSteps * QCursor.QCursorVolumeStep);
                break;
            default:
                QFlowWheelSeek(lSteps);
                break;
        }

        return true;
    }

    public void QFlowZoom(int lSteps)
    {
        if (qFlowRange is not { } lSpool)
        {
            return;
        }

        lSpool.LSpoolZoom(qFlowCursor, lSteps);
        QFlowSpool.QFlowSpoolHandle();
    }

    private void QFlowWheelSeek(int lSteps)
    {
        if (qFlowRange is not { } lSpool)
        {
            return;
        }

        QFlowCursorSeek(QFlowCursorClamp(qFlowCursor + lSpool.LSpoolStepResolve(lSteps)));
    }

    public void QFlowDragHandle(bool lDragging)
    {
        QFlowDragChanged?.Invoke(lDragging);
        if (!qFlowCommandActive || !LPreference.LPreferenceStateCurrent.LPreferenceDragPaused)
        {
            return;
        }

        if (lDragging)
        {
            if (qFlowDragPaused || qFlowPlayingSource?.Invoke() != true)
            {
                return;
            }

            qFlowDragPaused = true;
            QFlowPauseRaise();
            return;
        }

        if (!qFlowDragPaused)
        {
            return;
        }

        qFlowDragPaused = false;
        QFlowPlayRaise();
    }

    public void QFlowPlayRaise()
    {
        if (qFlowCommandActive)
        {
            QFlowPlayed?.Invoke();
        }
    }

    public void QFlowPauseRaise()
    {
        if (qFlowCommandActive)
        {
            QFlowPaused?.Invoke();
        }
    }

    public bool QFlowShortcutRun(string lToken)
    {
        if (!qFlowCommandActive || qFlowRange is null)
        {
            return false;
        }

        bool lEditable = QFlowEdit.QFlowEditCheck();
        switch (lToken)
        {
            case "ZoomIn": QFlowZoom(1); return true;
            case "ZoomOut": QFlowZoom(-1); return true;
            case "SectionAdd" when lEditable: QFlowSection.QFlowSectionAdd(); return true;
            case "SectionStart" when lEditable: QFlowSection.QFlowStartSet(); return true;
            case "SectionSplit" when lEditable: QFlowSection.QFlowSectionDivide(); return true;
            case "SectionEnd" when lEditable: QFlowSection.QFlowEndSet(); return true;
            case "SectionDelete" when lEditable: QFlowSection.QFlowSectionDelete(); return true;
            case "SectionRename" when lEditable: return QFlowName.QFlowNameStart();
            case "KeyframePrevious": QFlowKeyframe.QFlowKeyframeMove(-1); return true;
            case "KeyframeNearest": QFlowKeyframe.QFlowKeyframeMove(0); return true;
            case "KeyframeNext": QFlowKeyframe.QFlowKeyframeMove(1); return true;
            default: return false;
        }
    }

    public void QFlowSectionSet(bool lSectionActive) => qFlowSectionActive = lSectionActive;

    public void QFlowCommandSet(bool lCommandActive)
    {
        qFlowCommandActive = lCommandActive;
        if (lCommandActive)
        {
            QFlowKeyframe.QFlowKeyframeDefer();
            return;
        }

        QFlowKeyframe.QFlowKeyframeReset();
        QFlowKeyframe.QFlowKeyframeSuspend(false);
        qFlowKeyframeDirection = null;
    }

    public void QFlowUnloadSet() => qFlowUnloaded = true;

    public void QFlowPausedSet(bool lDragPaused) => qFlowDragPaused = lDragPaused;

    public bool QFlowWaveformSet(bool lWaveformActive)
    {
        if (qFlowWaveformActive == lWaveformActive)
        {
            return false;
        }

        qFlowWaveformActive = lWaveformActive;
        QFlowWaveformChanged?.Invoke(lWaveformActive);
        QFlowWaveform.QFlowWaveformApply();
        QFlowWaveform.QFlowWaveformStart();
        return true;
    }

    public void QFlowDirectionSet(int? lKeyframeDirection) => qFlowKeyframeDirection = lKeyframeDirection;

    private static bool QFlowOrderCheck() =>
        LPreference.LPreferenceStateCurrent.LPreferenceTimelineOrder != "ViewfinderFirst";
}
