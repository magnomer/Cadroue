using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class LFlow
{
    private LSpool? lFlowSpool;
    private LMediaInfo? lFlowMediaInfo;
    private string? lFlowSourcePath;
    private TimeSpan lFlowCursor;
    private int? lFlowSectionIndex;
    private TimeSpan lFlowDragTime;
    private bool lFlowSectionActive;
    private bool lFlowCommandActive;
    private bool lFlowUnloaded;
    private bool lFlowDragPaused;
    private bool lFlowSectionEditable = true;
    private bool lFlowSegmentFired;
    private bool lFlowWaveformActive = LPreference.LPreferenceStateCurrent.LPreferenceWaveform;
    private bool lFlowWaveformAudio;
    private string? lFlowKeyframeStamp;
    private int? lFlowKeyframeDirection;
    private string lFlowLosslesscutPath = string.Empty;
    private int lFlowPaletteCount = 1;
    private Func<bool>? lFlowPlayingSource;

    public LFlow()
    {
        LFlowSection = new LFlowSection(this);
        LFlowKeyframe = new LFlowKeyframe(this);
        LFlowWaveform = new LFlowWaveform(this);
        LFlowLosslesscut = new LFlowLosslesscut(this);
        LFlowName = new LFlowName(this);
    }

    public event Action<bool>? LFlowEditChange;
    public event Action<bool>? LFlowWaveformChange;
    public event Action? LFlowMediaChange;
    public event Action<TimeSpan>? LFlowCursorChange;
    public event Action<bool>? LFlowDragChange;
    public event Action? LFlowPlay;
    public event Action? LFlowPause;
    public event Action<double>? LFlowVolumeAdjust;
    public event Action? LFlowAttachApply;
    public event Action? LFlowClearApply;
    public event Action? LFlowCursorApply;
    public event Action? LFlowSpoolApply;

    public LFlowSection LFlowSection { get; }

    public LFlowKeyframe LFlowKeyframe { get; }

    public LFlowWaveform LFlowWaveform { get; }

    public LFlowLosslesscut LFlowLosslesscut { get; }

    public LFlowName LFlowName { get; }

    public LSpool? LFlowSpool => lFlowSpool;

    public LMediaInfo? LFlowMediaInfo => lFlowMediaInfo;

    public string? LFlowSourcePath => lFlowSourcePath;

    public TimeSpan LFlowCursor => lFlowCursor;

    public int? LFlowSectionIndex => lFlowSectionIndex;

    public TimeSpan LFlowDragTime => lFlowDragTime;

    public bool LFlowSectionActive => lFlowSectionActive;

    public bool LFlowCommandActive => lFlowCommandActive;

    public bool LFlowUnloaded => lFlowUnloaded;

    public bool LFlowDragPaused => lFlowDragPaused;

    public bool LFlowSectionEditable => lFlowSectionEditable;

    public bool LFlowSegmentFired => lFlowSegmentFired;

    public bool LFlowWaveformActive => lFlowWaveformActive;

    public bool LFlowWaveformAudio => lFlowWaveformAudio;

    public string? LFlowKeyframeStamp => lFlowKeyframeStamp;

    public int? LFlowKeyframeDirection => lFlowKeyframeDirection;

    public string LFlowLosslesscutPath => lFlowLosslesscutPath;

    public int LFlowPaletteCount => lFlowPaletteCount;

    public TimeSpan LFlowDuration => lFlowSpool?.LSpoolDuration ?? TimeSpan.Zero;

    public string LFlowLabelOrigin => LCursor.LCursorTimeFormat(lFlowSpool?.LSpoolRangeOrigin ?? TimeSpan.Zero);

    public string LFlowLabelLimit => LCursor.LCursorTimeFormat(lFlowSpool?.LSpoolRangeLimit ?? TimeSpan.Zero);

    public string LFlowLabelZero => LCursor.LCursorTimeFormat(TimeSpan.Zero);

    public string LFlowLabelDuration => LCursor.LCursorTimeFormat(LFlowDuration);

    public int LFlowMapRow => LFlowOrderCheck() ? 1 : 3;

    public int LFlowViewfinderRow => LFlowOrderCheck() ? 3 : 1;

    public bool LFlowSourceCheck() =>
        lFlowSpool is not null && !string.IsNullOrWhiteSpace(lFlowSourcePath);

    public bool LFlowScanCheck() =>
        lFlowCommandActive && !lFlowUnloaded && lFlowMediaInfo is not null && LFlowSourceCheck();

    public bool LFlowEditCheck() =>
        lFlowSectionActive && lFlowSectionEditable;

    public string LFlowSourceFormat() =>
        string.IsNullOrWhiteSpace(lFlowSourcePath) ? "(no media)" : LUsher.LUsherNameRead(lFlowSourcePath);

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
        LFlowAttachApply?.Invoke();
        LFlowSection.LFlowSectionLoad();
        LFlowMediaChange?.Invoke();
        LFlowKeyframe.LFlowKeyframeRun();
        LFlowWaveform.LFlowWaveformStart();
        if (lSameSource && lFlowCursor > TimeSpan.Zero)
        {
            LFlowCursorChange?.Invoke(lFlowCursor);
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
        LFlowClearApply?.Invoke();
        LFlowSection.LFlowSectionRaise();
        LFlowMediaChange?.Invoke();
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
        lFlowSectionIndex = null;
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
        LFlowCursorApply?.Invoke();
        if (lKeyframeRestart)
        {
            LFlowKeyframe.LFlowKeyframeRun();
        }

        if (lViewerSeek)
        {
            LFlowCursorChange?.Invoke(lFlowCursor);
        }
    }

    public void LFlowRangeSet(TimeSpan lOrigin, TimeSpan lLimit)
    {
        if (lFlowSpool is not { } lSpool)
        {
            return;
        }

        lSpool.LSpoolRangeSet(lOrigin, lLimit);
        LFlowSpoolHandle();
    }

    public void LFlowSpoolHandle()
    {
        LFlowKeyframe.LFlowKeyframeSuspend(true);
        LFlowSpoolUpdate();
    }

    public void LFlowSpoolUpdate()
    {
        LFlowSpoolApply?.Invoke();
        if (lFlowSpool is null)
        {
            return;
        }

        LFlowKeyframe.LFlowKeyframeDefer();
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
                LFlowVolumeAdjust?.Invoke(lSteps * LCursor.LCursorVolumeStep);
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
        LFlowSpoolHandle();
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
        LFlowDragChange?.Invoke(lDragging);
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
            LFlowPlay?.Invoke();
        }
    }

    public void LFlowPauseRaise()
    {
        if (lFlowCommandActive)
        {
            LFlowPause?.Invoke();
        }
    }

    public bool LFlowShortcutRun(string lToken)
    {
        if (!lFlowCommandActive || lFlowSpool is null)
        {
            return false;
        }

        bool lEditable = LFlowEditCheck();
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

    public void LFlowSectionSelect(int? lSectionIndex) => lFlowSectionIndex = lSectionIndex;

    public void LFlowDragSet(TimeSpan lDragTime) => lFlowDragTime = lDragTime;

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

    public bool LFlowEditSet(bool lSectionEditable)
    {
        if (!lSectionEditable)
        {
            LFlowName.LFlowNameHide();
        }

        if (lFlowSectionEditable == lSectionEditable)
        {
            return false;
        }

        lFlowSectionEditable = lSectionEditable;
        LFlowEditChange?.Invoke(lSectionEditable);
        return true;
    }

    public void LFlowFiredSet(bool lSegmentFired) => lFlowSegmentFired = lSegmentFired;

    public bool LFlowWaveformSet(bool lWaveformActive)
    {
        if (lFlowWaveformActive == lWaveformActive)
        {
            return false;
        }

        lFlowWaveformActive = lWaveformActive;
        LFlowWaveformChange?.Invoke(lWaveformActive);
        LFlowWaveform.LFlowWaveformApply();
        LFlowWaveform.LFlowWaveformStart();
        return true;
    }

    public bool LFlowStampSet(string lKeyframeStamp)
    {
        if (string.Equals(lFlowKeyframeStamp, lKeyframeStamp, StringComparison.Ordinal))
        {
            return false;
        }

        lFlowKeyframeStamp = lKeyframeStamp;
        return true;
    }

    public void LFlowDirectionSet(int? lKeyframeDirection) => lFlowKeyframeDirection = lKeyframeDirection;

    public bool LFlowLosslesscutSet(string lLosslesscutPath)
    {
        if (string.Equals(lFlowLosslesscutPath, lLosslesscutPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        lFlowLosslesscutPath = lLosslesscutPath;
        return true;
    }

    private static bool LFlowOrderCheck() =>
        LPreference.LPreferenceStateCurrent.LPreferenceTimelineOrder != "ViewfinderFirst";
}
