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
    private bool qFlowWaveformActive = LPreference.LPreferenceStateCurrent.LPreferenceWaveform;
    private bool qFlowWaveformAudio;
    private int? qFlowKeyframeDirection;
    private int qFlowPaletteCount = 1;

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
        QFlowWheel = new QFlowWheel(this);
        QFlowViewfinder = new QViewfinder(this);
        QFlowMap = new QMap(this);
        QFlowDivider = new QDivider();
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

    public LSpool? QFlowRange => qFlowRange;

    public LMediaInfo? QFlowMediaInfo => qFlowMediaInfo;

    public string? QFlowSourcePath => qFlowSourcePath;

    public TimeSpan QFlowCursor => qFlowCursor;

    public bool QFlowSectionActive => qFlowSectionActive;

    public bool QFlowCommandActive => qFlowCommandActive;

    public bool QFlowUnloaded => qFlowUnloaded;

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

    public void QFlowPaletteApply(int lPaletteCount) => qFlowPaletteCount = QFlowPaletteClamp(lPaletteCount);

    private static int QFlowPaletteClamp(int lPaletteCount) => Math.Max(1, lPaletteCount);

    public void QFlowAttach(LMediaInfo lMediaInfo, string? lSourcePath, TimeSpan lCursorTime)
    {
        if (!qFlowCommandActive)
        {
            return;
        }

        QFlowKeyframe.QFlowKeyframeReset();
        QFlowKeyframe.QFlowKeyframeClear();
        bool lSameSource = QFlowSourceMatch(lSourcePath);
        TimeSpan lResumeAt = QFlowResumeResolve(lSameSource, lCursorTime);
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

    private TimeSpan QFlowResumeResolve(bool lSameSource, TimeSpan lCursorTime) =>
        lSameSource ? qFlowCursor : lCursorTime;

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

        QFlowUnloadSet();
        QFlowName.QFlowNameHide();
        QFlowKeyframe.QFlowKeyframeClose();
        QFlowWaveform.QFlowWaveformClose();
    }

    public void QFlowSourceSet(LMediaInfo lMediaInfo, string? lSourcePath)
    {
        qFlowSourcePath = QFlowPathNormalize(lSourcePath);
        qFlowMediaInfo = lMediaInfo;
        qFlowRange = new LSpool(lMediaInfo.LMediaInfoDuration);
        qFlowWaveformAudio = lMediaInfo.LMediaAudioPresent;
        qFlowKeyframeDirection = null;
    }

    private static string? QFlowPathNormalize(string? lSourcePath) =>
        string.IsNullOrWhiteSpace(lSourcePath) ? null : lSourcePath;

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
        QFlowDirectionSet(null);
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
            case "ZoomIn": QFlowWheel.QFlowZoom(1); return true;
            case "ZoomOut": QFlowWheel.QFlowZoom(-1); return true;
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
        QFlowCommandApply(lCommandActive);
        if (lCommandActive)
        {
            QFlowKeyframe.QFlowKeyframeDefer();
            return;
        }

        QFlowKeyframe.QFlowKeyframeReset();
        QFlowKeyframe.QFlowKeyframeSuspend(false);
        QFlowDirectionSet(null);
    }

    private void QFlowCommandApply(bool lCommandActive) => qFlowCommandActive = lCommandActive;

    public void QFlowUnloadSet() => qFlowUnloaded = true;

    public bool QFlowWaveformSet(bool lWaveformActive)
    {
        if (!QFlowWaveformChange(lWaveformActive))
        {
            return false;
        }

        QFlowWaveform.QFlowWaveformApply();
        QFlowWaveform.QFlowWaveformStart();
        return true;
    }

    private bool QFlowWaveformChange(bool lWaveformActive)
    {
        if (qFlowWaveformActive == lWaveformActive)
        {
            return false;
        }

        qFlowWaveformActive = lWaveformActive;
        QFlowWaveformChanged?.Invoke(lWaveformActive);
        return true;
    }

    public void QFlowDirectionSet(int? lKeyframeDirection) => qFlowKeyframeDirection = lKeyframeDirection;

    private static bool QFlowOrderCheck() =>
        LPreference.LPreferenceStateCurrent.LPreferenceTimelineOrder != "ViewfinderFirst";
}
