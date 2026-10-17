using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The shortcut a flow token asks for, once the flow has decided the token may run.
public enum CFlowAction
{
    CFlowActionNone,
    CFlowActionInward,
    CFlowActionOutward,
    CFlowActionAddition,
    CFlowActionOrigin,
    CFlowActionSplit,
    CFlowActionEnd,
    CFlowActionRemoval,
    CFlowActionRename,
    CFlowActionPrevious,
    CFlowActionNearest,
    CFlowActionNext,
}

// The flow's single home: the media it shows, its cursor and switches, and when each request may run. The drivers
// read it and keep no copy. A media record passes through unread; the port answers it as plain values.
public sealed class CFlow
{
    private readonly LFlowPort _cFlowPort;
    private readonly CUsher _cFlowUsher;

    internal CFlow(LFlowPort port, LMapPort map, CUsher usher)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(usher);
        _cFlowPort = port;
        _cFlowUsher = usher;
        CFlowWaveformActive = port.LFlowWaveformRead();
        CFlowSection = new CSection(port.LFlowSectionCreate(CFlowSourceFormat), this);
        CFlowKeyframe = new CKeyframe(port.LFlowKeyframeCreate(CFlowSourceFormat), this);
        CFlowWaveform = new CWaveform(port.LFlowWaveformCreate(), this);
        CFlowName = new CFlowName(port, CFlowSection);
        CFlowMap = new CMap(map, this);
        CFlowWheel = new CFlowWheel(port, this);
    }

    public CSection CFlowSection { get; }
    public CKeyframe CFlowKeyframe { get; }
    public CWaveform CFlowWaveform { get; }
    public CFlowName CFlowName { get; }
    public CMap CFlowMap { get; }
    public CFlowWheel CFlowWheel { get; }

    public event Action<bool>? CFlowWaveformChanged;
    public event Action? CFlowPlayed;
    public event Action? CFlowPaused;

    // The flow moved its cursor itself, as a keyframe move does.
    public event Action? CFlowCursorMoved;

    // The flow asks the viewer to seek to its cursor.
    public event Action<TimeSpan>? CFlowCursorSought;

    // Told at every drag start and end, whether or not commands run.
    public event Action<bool>? CFlowDragChanged;

    public string? CFlowSource { get; private set; }

    // The held media record, passed through unread to the keyframe scan.
    internal Cadroue.Core.LMediaInfo? CFlowMedia { get; private set; }

    // The engine's range over the held media, passed through unread, only for the readers not yet moved to a gate.
    public Cadroue.Core.LSpool? CFlowRange { get; private set; }

    // A media is held: the flow has a range of this duration.
    public bool CFlowLoaded { get; private set; }

    public TimeSpan CFlowDuration { get; private set; }

    public TimeSpan CFlowCursor { get; private set; }

    public bool CFlowSectionActive { get; private set; }

    public bool CFlowCommandActive { get; private set; }

    public bool CFlowUnloaded { get; private set; }

    public bool CFlowWaveformActive { get; private set; }

    public bool CFlowWaveformAudio { get; private set; }

    public int? CFlowKeyframeDirection { get; private set; }

    public int CFlowPaletteCount { get; private set; } = 1;

    // The last attach took the source it already held at a place past the start: the viewer seeks there.
    public bool CFlowResumed { get; private set; }

    // A drag paused the playing media; the drag's end plays it again.
    public bool CFlowDragPaused { get; private set; }

    public bool CFlowMapFirst => _cFlowPort.LFlowOrderRead();

    // The height the flow panel was last left at.
    public double CFlowHeight => _cFlowPort.LFlowHeightRead();

    public bool CFlowSourceCheck() => CFlowLoaded && !string.IsNullOrWhiteSpace(CFlowSource);

    public bool CFlowScanCheck() => CFlowCommandActive && !CFlowUnloaded && CFlowSourceCheck();

    public string CFlowSourceFormat() =>
        string.IsNullOrWhiteSpace(CFlowSource) ? "(no media)" : _cFlowUsher.CUsherNameRead(CFlowSource);

    public void CFlowPaletteApply(int count) => CFlowPaletteCount = Math.Max(1, count);

    // A new media while commands run; the same source resumes at the cursor it held. The sections start over on it.
    public bool CFlowAttach(Cadroue.Core.LMediaInfo media, string? source, TimeSpan cursor)
    {
        if (!CFlowCommandActive)
        {
            return false;
        }

        bool same = CFlowSourceMatch(source);
        TimeSpan resume = same ? CFlowCursor : cursor;
        CFlowSourceSet(media, source);
        CFlowCursorSet(resume);
        CFlowResumed = same && CFlowCursor > TimeSpan.Zero;
        CFlowSection.CSectionAttach();
        CFlowKeyframe.CKeyframeReset();
        CFlowKeyframe.CKeyframeClear();
        return true;
    }

    // Nothing to clear when no source, no range and no section is held. The scans stop and the peaks go.
    public bool CFlowClear(bool sectionsEmpty)
    {
        if (CFlowSource is null && !CFlowLoaded && sectionsEmpty)
        {
            return false;
        }

        CFlowSourceClear();
        CFlowKeyframe.CKeyframeReset();
        CFlowKeyframe.CKeyframeSuspend(false);
        CFlowKeyframe.CKeyframeClear();
        CFlowWaveform.CWaveformClear();
        return true;
    }

    public bool CFlowClose()
    {
        if (CFlowUnloaded)
        {
            return false;
        }

        CFlowUnloaded = true;
        CFlowKeyframe.CKeyframeClose();
        CFlowWaveform.CWaveformClose();
        return true;
    }

    public void CFlowSourceSet(Cadroue.Core.LMediaInfo media, string? source)
    {
        (TimeSpan duration, bool audio, Cadroue.Core.LSpool range) = _cFlowPort.LFlowMediaRead(media);
        CFlowMedia = media;
        CFlowRange = range;
        CFlowSource = string.IsNullOrWhiteSpace(source) ? null : source;
        CFlowLoaded = true;
        CFlowDuration = duration < TimeSpan.Zero ? TimeSpan.Zero : duration;
        CFlowWaveformAudio = audio;
        CFlowKeyframeDirection = null;
    }

    public bool CFlowSourceMatch(string? source) =>
        !string.IsNullOrWhiteSpace(source) && string.Equals(CFlowSource, source, StringComparison.OrdinalIgnoreCase);

    public void CFlowSourceClear()
    {
        CFlowSource = null;
        CFlowMedia = null;
        CFlowRange = null;
        CFlowLoaded = false;
        CFlowDuration = TimeSpan.Zero;
        CFlowCursor = TimeSpan.Zero;
        CFlowKeyframeDirection = null;
    }

    public TimeSpan CFlowCursorClamp(TimeSpan cursor)
    {
        if (!CFlowLoaded || cursor < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return cursor > CFlowDuration ? CFlowDuration : cursor;
    }

    public void CFlowCursorSet(TimeSpan cursor) => CFlowCursor = CFlowCursorClamp(cursor);

    // A clock tick while commands run: the cursor moves, no keyframe move waits any more and the scan follows it.
    public bool CFlowCursorUpdate(TimeSpan cursor)
    {
        if (!CFlowCommandActive)
        {
            return false;
        }

        CFlowCursorPropagate(cursor);
        CFlowCursorMoved?.Invoke();
        CFlowKeyframe.CKeyframeSync(cursor);
        CFlowKeyframe.CKeyframeDefer();
        return true;
    }

    // A seek while commands run: the cursor moves, the scan waits to resume and the viewer seeks.
    public bool CFlowCursorSeek(TimeSpan cursor)
    {
        if (!CFlowCommandActive)
        {
            return false;
        }

        CFlowCursorPropagate(cursor);
        CFlowKeyframe.CKeyframeSuspend(true);
        CFlowCursorMoved?.Invoke();
        CFlowCursorSought?.Invoke(CFlowCursor);
        return true;
    }

    public void CFlowCursorPropagate(TimeSpan cursor)
    {
        CFlowKeyframeDirection = null;
        CFlowCursorSet(cursor);
    }

    // The flow moves its cursor to a place it chose: the scan may start over there and the viewer may seek.
    public void CFlowCursorPropagate(TimeSpan cursor, bool seek, bool restart)
    {
        CFlowCursorPropagate(cursor);
        CFlowCursorMoved?.Invoke();
        if (restart)
        {
            CFlowKeyframe.CKeyframeRun();
        }

        if (seek)
        {
            CFlowCursorSought?.Invoke(CFlowCursor);
        }
    }

    public void CFlowPlayRaise()
    {
        if (CFlowCommandActive)
        {
            CFlowPlayed?.Invoke();
        }
    }

    public void CFlowPauseRaise()
    {
        if (CFlowCommandActive)
        {
            CFlowPaused?.Invoke();
        }
    }

    // Shortcuts run only while commands run on a held range; the section edits also need an editable flow.
    public CFlowAction CFlowActionResolve(string token, bool editable)
    {
        if (!CFlowCommandActive || !CFlowLoaded)
        {
            return CFlowAction.CFlowActionNone;
        }

        return token switch
        {
            "ZoomIn" => CFlowAction.CFlowActionInward,
            "ZoomOut" => CFlowAction.CFlowActionOutward,
            "SectionAdd" when editable => CFlowAction.CFlowActionAddition,
            "SectionStart" when editable => CFlowAction.CFlowActionOrigin,
            "SectionSplit" when editable => CFlowAction.CFlowActionSplit,
            "SectionEnd" when editable => CFlowAction.CFlowActionEnd,
            "SectionDelete" when editable => CFlowAction.CFlowActionRemoval,
            "SectionRename" when editable => CFlowAction.CFlowActionRename,
            "KeyframePrevious" => CFlowAction.CFlowActionPrevious,
            "KeyframeNearest" => CFlowAction.CFlowActionNearest,
            "KeyframeNext" => CFlowAction.CFlowActionNext,
            _ => CFlowAction.CFlowActionNone,
        };
    }

    public void CFlowSectionSet(bool active) => CFlowSectionActive = active;

    // Commands starting wait for the request timer; stopping drops the keyframe move that waited and its timers.
    public void CFlowCommandSet(bool active)
    {
        CFlowCommandActive = active;
        if (active)
        {
            CFlowKeyframe.CKeyframeDefer();
            return;
        }

        CFlowKeyframeDirection = null;
        CFlowKeyframe.CKeyframeReset();
        CFlowKeyframe.CKeyframeSuspend(false);
    }

    public void CFlowUnloadSet() => CFlowUnloaded = true;

    // A changed switch repaints the rail and scans the held source when it turns on.
    public bool CFlowWaveformSet(bool active)
    {
        if (CFlowWaveformActive == active)
        {
            return false;
        }

        CFlowWaveformActive = active;
        CFlowWaveformChanged?.Invoke(active);
        CFlowWaveform.CWaveformApply();
        CFlowWaveform.CWaveformStart();
        return true;
    }

    public void CFlowDirectionSet(int? direction) => CFlowKeyframeDirection = direction;

    // While commands run and the preference asks, a drag start pauses playing media; its end plays what it paused.
    public void CFlowDragSet(bool dragging, Func<bool>? playing)
    {
        CFlowDragChanged?.Invoke(dragging);
        if (!CFlowCommandActive || !_cFlowPort.LFlowDragRead())
        {
            return;
        }

        if (dragging)
        {
            if (CFlowDragPaused || playing?.Invoke() != true)
            {
                return;
            }

            CFlowDragPaused = true;
            CFlowPauseRaise();
            return;
        }

        if (!CFlowDragPaused)
        {
            return;
        }

        CFlowDragPaused = false;
        CFlowPlayRaise();
    }
}
