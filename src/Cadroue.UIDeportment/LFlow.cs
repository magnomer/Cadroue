using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LFlow
{
    private readonly CUsher lFlowUsher;
    private LSpool? lFlowSpool;
    private LMediaInfo? lFlowMediaInfo;
    private string? lFlowSourcePath;
    private TimeSpan lFlowCursor;
    private bool lFlowSectionActive;
    private bool lFlowCommandActive;
    private bool lFlowUnloaded;
    private bool lFlowDragPaused;
    private bool lFlowWaveformActive = LPreference.LPreferenceStateCurrent.LPreferenceWaveform;
    private bool lFlowWaveformAudio;
    private int? lFlowKeyframeDirection;
    private int lFlowPaletteCount = 1;
    private Func<bool>? lFlowPlayingSource;

    public LFlow(LStrip lStrip)
    {
        lFlowUsher = lStrip.LStripUsher;
        QFlowEdit = new QFlowEdit(this);
        QFlowSpool = new QFlowSpool(this);
        QFlowStamp = new QFlowStamp();
        LFlowSection = new LFlowSection(this);
        LFlowKeyframe = new LFlowKeyframe(this, lFlowUsher);
        LFlowWaveform = new LFlowWaveform(this);
        LFlowLosslesscut = new LFlowLosslesscut(this, lFlowUsher);
        LFlowName = new LFlowName(this);
    }

    public event Action<bool>? LFlowWaveformChanged;
    public event Action? LFlowMediaChanged;
    public event Action<TimeSpan>? LFlowCursorChanged;
    public event Action<bool>? LFlowDragChanged;
    public event Action? LFlowPlayed;
    public event Action? LFlowPaused;
    public event Action<double>? LFlowVolumeAdjusted;
    public event Action? LFlowAttachApplied;
    public event Action? LFlowClearApplied;
    public event Action? LFlowCursorApplied;

    public LFlowSection LFlowSection { get; }

    public LFlowKeyframe LFlowKeyframe { get; }

    public LFlowWaveform LFlowWaveform { get; }

    public LFlowLosslesscut LFlowLosslesscut { get; }

    public LFlowName LFlowName { get; }

    public QFlowEdit QFlowEdit { get; }

    public QFlowSpool QFlowSpool { get; }

    public QFlowStamp QFlowStamp { get; }

    public LSpool? LFlowSpool => lFlowSpool;

    public LMediaInfo? LFlowMediaInfo => lFlowMediaInfo;

    public string? LFlowSourcePath => lFlowSourcePath;

    public TimeSpan LFlowCursor => lFlowCursor;

    public bool LFlowSectionActive => lFlowSectionActive;

    public bool LFlowCommandActive => lFlowCommandActive;

    public bool LFlowUnloaded => lFlowUnloaded;

    public bool LFlowDragPaused => lFlowDragPaused;

    public bool LFlowWaveformActive => lFlowWaveformActive;

    public bool LFlowWaveformAudio => lFlowWaveformAudio;

    public int? LFlowKeyframeDirection => lFlowKeyframeDirection;

    public int LFlowPaletteCount => lFlowPaletteCount;

    public TimeSpan LFlowDuration => lFlowSpool?.LSpoolDuration ?? TimeSpan.Zero;

    public int LFlowMapRow => LFlowOrderCheck() ? 1 : 3;

    public int LFlowViewfinderRow => LFlowOrderCheck() ? 3 : 1;

    public bool LFlowSourceCheck() =>
        lFlowSpool is not null && !string.IsNullOrWhiteSpace(lFlowSourcePath);

    public bool LFlowScanCheck() =>
        lFlowCommandActive && !lFlowUnloaded && lFlowMediaInfo is not null && LFlowSourceCheck();

    public string LFlowSourceFormat() =>
        string.IsNullOrWhiteSpace(lFlowSourcePath) ? "(no media)" : lFlowUsher.CUsherNameRead(lFlowSourcePath);

    public void LFlowPlayingAttach(Func<bool>? lPlayingSource) => lFlowPlayingSource = lPlayingSource;

    public void LFlowPaletteApply(int lPaletteCount) => lFlowPaletteCount = Math.Max(1, lPaletteCount);

    public void LFlowAttach(LMediaInfo lMediaInfo, string? lSourcePath, TimeSpan lCursorTime)
    {
        if (!lFlowCommandActive)
        {
            return;
        }

        LFlowKeyframe.LFlowKeyframeReset();
        LFlowKeyframe.LFlowKeyframeClear();
        bool lSameSource = LFlowSourceMatch(lSourcePath);
        TimeSpan lResumeAt = lSameSource ? lFlowCursor : lCursorTime;
        LFlowSourceSet(lMediaInfo, lSourcePath);
        LFlowCursorSet(lResumeAt);
        LFlowSection.LFlowSectionAttach();
        LFlowAttachApplied?.Invoke();
        LFlowSection.LFlowSectionLoad();
        LFlowMediaChanged?.Invoke();
        LFlowKeyframe.LFlowKeyframeRun();
        LFlowWaveform.LFlowWaveformStart();
        if (lSameSource && lFlowCursor > TimeSpan.Zero)
        {
            LFlowCursorChanged?.Invoke(lFlowCursor);
        }
    }

    public bool LFlowClear()
    {
        if (lFlowSourcePath is null && lFlowSpool is null && LFlowSection.LFlowEmptyCheck())
        {
            return false;
        }

        LFlowKeyframe.LFlowKeyframeReset();
        LFlowKeyframe.LFlowKeyframeSuspend(false);
        LFlowKeyframe.LFlowKeyframeClear();
        LFlowSourceClear();
        LFlowSection.LFlowSectionReset();
        LFlowWaveform.LFlowWaveformClear();
        LFlowClearApplied?.Invoke();
        LFlowSection.LFlowSectionRaise();
        LFlowMediaChanged?.Invoke();
        return true;
    }

    public void LFlowClose()
    {
        if (lFlowUnloaded)
        {
            return;
        }

        lFlowUnloaded = true;
        LFlowName.LFlowNameHide();
        LFlowKeyframe.LFlowKeyframeClose();
        LFlowWaveform.LFlowWaveformClose();
    }

    public void LFlowSourceSet(LMediaInfo lMediaInfo, string? lSourcePath)
    {
        lFlowSourcePath = string.IsNullOrWhiteSpace(lSourcePath) ? null : lSourcePath;
        lFlowMediaInfo = lMediaInfo;
        lFlowSpool = new LSpool(lMediaInfo.LMediaInfoDuration);
        lFlowWaveformAudio = lMediaInfo.LMediaAudioPresent;
        lFlowKeyframeDirection = null;
    }

    public bool LFlowSourceMatch(string? lSourcePath) =>
        !string.IsNullOrWhiteSpace(lSourcePath)
        && string.Equals(lFlowSourcePath, lSourcePath, StringComparison.OrdinalIgnoreCase);

    public void LFlowSourceClear()
    {
        lFlowSourcePath = null;
        lFlowMediaInfo = null;
        lFlowSpool = null;
        lFlowCursor = TimeSpan.Zero;
        QFlowEdit.LFlowSectionSelect(null);
        lFlowKeyframeDirection = null;
    }

    public TimeSpan LFlowCursorClamp(TimeSpan lCursor)
    {
        if (lFlowSpool is null || lCursor < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return lCursor > lFlowSpool.LSpoolDuration ? lFlowSpool.LSpoolDuration : lCursor;
    }

    public void LFlowCursorSet(TimeSpan lCursor) => lFlowCursor = LFlowCursorClamp(lCursor);

    public void LFlowCursorUpdate(TimeSpan lCursor)
    {
        if (!lFlowCommandActive)
        {
            return;
        }

        LFlowCursorPropagate(lCursor, false, false);
        LFlowKeyframe.LFlowKeyframeSync(lCursor);
        LFlowKeyframe.LFlowKeyframeDefer();
    }

    public void LFlowCursorSeek(TimeSpan lCursor)
    {
        if (!lFlowCommandActive)
        {
            return;
        }

        LFlowKeyframe.LFlowKeyframeSuspend(true);
        LFlowCursorPropagate(lCursor, true, false);
    }

    public void LFlowCursorPropagate(TimeSpan lCursor, bool lViewerSeek, bool lKeyframeRestart)
    {
        lFlowKeyframeDirection = null;
        LFlowCursorSet(lCursor);
        LFlowCursorApplied?.Invoke();
        if (lKeyframeRestart)
        {
            LFlowKeyframe.LFlowKeyframeRun();
        }

        if (lViewerSeek)
        {
            LFlowCursorChanged?.Invoke(lFlowCursor);
        }
    }

    public bool LFlowWheelHandle(int lDelta)
    {
        if (!lFlowCommandActive || lDelta == 0)
        {
            return false;
        }

        int lSteps = LCursor.LCursorWheelResolve(lDelta);
        switch (LPreference.LPreferenceStateCurrent.LPreferenceWheelAction)
        {
            case "Zoom":
                LFlowZoom(lSteps);
                break;
            case "Volume":
                LFlowVolumeAdjusted?.Invoke(lSteps * LCursor.LCursorVolumeStep);
                break;
            default:
                LFlowWheelSeek(lSteps);
                break;
        }

        return true;
    }

    public void LFlowZoom(int lSteps)
    {
        if (lFlowSpool is not { } lSpool)
        {
            return;
        }

        lSpool.LSpoolZoom(lFlowCursor, lSteps);
        QFlowSpool.LFlowSpoolHandle();
    }

    private void LFlowWheelSeek(int lSteps)
    {
        if (lFlowSpool is not { } lSpool)
        {
            return;
        }

        LFlowCursorSeek(LFlowCursorClamp(lFlowCursor + lSpool.LSpoolStepResolve(lSteps)));
    }

    public void LFlowDragHandle(bool lDragging)
    {
        LFlowDragChanged?.Invoke(lDragging);
        if (!lFlowCommandActive || !LPreference.LPreferenceStateCurrent.LPreferenceDragPaused)
        {
            return;
        }

        if (lDragging)
        {
            if (lFlowDragPaused || lFlowPlayingSource?.Invoke() != true)
            {
                return;
            }

            lFlowDragPaused = true;
            LFlowPauseRaise();
            return;
        }

        if (!lFlowDragPaused)
        {
            return;
        }

        lFlowDragPaused = false;
        LFlowPlayRaise();
    }

    public void LFlowPlayRaise()
    {
        if (lFlowCommandActive)
        {
            LFlowPlayed?.Invoke();
        }
    }

    public void LFlowPauseRaise()
    {
        if (lFlowCommandActive)
        {
            LFlowPaused?.Invoke();
        }
    }

    public bool LFlowShortcutRun(string lToken)
    {
        if (!lFlowCommandActive || lFlowSpool is null)
        {
            return false;
        }

        bool lEditable = QFlowEdit.LFlowEditCheck();
        switch (lToken)
        {
            case "ZoomIn": LFlowZoom(1); return true;
            case "ZoomOut": LFlowZoom(-1); return true;
            case "SectionAdd" when lEditable: LFlowSection.LFlowSectionAdd(); return true;
            case "SectionStart" when lEditable: LFlowSection.LFlowStartSet(); return true;
            case "SectionSplit" when lEditable: LFlowSection.LFlowSectionDivide(); return true;
            case "SectionEnd" when lEditable: LFlowSection.LFlowEndSet(); return true;
            case "SectionDelete" when lEditable: LFlowSection.LFlowSectionDelete(); return true;
            case "SectionRename" when lEditable: return LFlowName.LFlowNameStart();
            case "KeyframePrevious": LFlowKeyframe.LFlowKeyframeMove(-1); return true;
            case "KeyframeNearest": LFlowKeyframe.LFlowKeyframeMove(0); return true;
            case "KeyframeNext": LFlowKeyframe.LFlowKeyframeMove(1); return true;
            default: return false;
        }
    }

    public void LFlowSectionSet(bool lSectionActive) => lFlowSectionActive = lSectionActive;

    public void LFlowCommandSet(bool lCommandActive)
    {
        lFlowCommandActive = lCommandActive;
        if (lCommandActive)
        {
            LFlowKeyframe.LFlowKeyframeDefer();
            return;
        }

        LFlowKeyframe.LFlowKeyframeReset();
        LFlowKeyframe.LFlowKeyframeSuspend(false);
        lFlowKeyframeDirection = null;
    }

    public void LFlowUnloadSet() => lFlowUnloaded = true;

    public void LFlowPausedSet(bool lDragPaused) => lFlowDragPaused = lDragPaused;

    public bool LFlowWaveformSet(bool lWaveformActive)
    {
        if (lFlowWaveformActive == lWaveformActive)
        {
            return false;
        }

        lFlowWaveformActive = lWaveformActive;
        LFlowWaveformChanged?.Invoke(lWaveformActive);
        LFlowWaveform.LFlowWaveformApply();
        LFlowWaveform.LFlowWaveformStart();
        return true;
    }

    public void LFlowDirectionSet(int? lKeyframeDirection) => lFlowKeyframeDirection = lKeyframeDirection;

    private static bool LFlowOrderCheck() =>
        LPreference.LPreferenceStateCurrent.LPreferenceTimelineOrder != "ViewfinderFirst";
}
