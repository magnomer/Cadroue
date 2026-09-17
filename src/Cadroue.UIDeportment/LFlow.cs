using Cadroue.Application;
using Cadroue.Core;

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
    private string lFlowMapTrigger = "attach";
    private string lFlowViewfinderTrigger = "attach";

    public event Action<bool>? LFlowEditChange;
    public event Action<bool>? LFlowWaveformChange;

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

    public string LFlowMapTrigger => lFlowMapTrigger;

    public string LFlowViewfinderTrigger => lFlowViewfinderTrigger;

    public TimeSpan LFlowDuration => lFlowSpool?.LSpoolDuration ?? TimeSpan.Zero;

    public bool LFlowSourceCheck() =>
        lFlowSpool is not null && !string.IsNullOrWhiteSpace(lFlowSourcePath);

    public bool LFlowScanCheck() =>
        lFlowCommandActive && !lFlowUnloaded && lFlowMediaInfo is not null && LFlowSourceCheck();

    public bool LFlowEditCheck() =>
        lFlowSectionActive && lFlowSectionEditable;

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

    public void LFlowSectionSelect(int? lSectionIndex) => lFlowSectionIndex = lSectionIndex;

    public void LFlowDragSet(TimeSpan lDragTime) => lFlowDragTime = lDragTime;

    public void LFlowSectionSet(bool lSectionActive) => lFlowSectionActive = lSectionActive;

    public void LFlowCommandSet(bool lCommandActive) => lFlowCommandActive = lCommandActive;

    public void LFlowUnloadSet() => lFlowUnloaded = true;

    public void LFlowPausedSet(bool lDragPaused) => lFlowDragPaused = lDragPaused;

    public bool LFlowEditSet(bool lSectionEditable)
    {
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

    public void LFlowMapSet(string lTrigger) => lFlowMapTrigger = lTrigger;

    public void LFlowViewfinderSet(string lTrigger) => lFlowViewfinderTrigger = lTrigger;
}
