using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The viewer's tenure and trace decisions: the tenure state its gates decide from (the load serial, unloaded,
// command, preview engine and host shown), whether and which record is written, with the state only the records use
// (the host stamp and detail, the drag seek buffer, the overlay shown flag). The words are the outlet's. One per
// viewer, built with its workspace; the drivers read the state through its properties and keep no copy.
public sealed class CViewer
{
    private readonly LViewerPort _cViewerPort;

    // Moves on at every load start and teardown, so a load that lands under an older serial is stale.
    private int _cViewerSerial;

    private bool _cViewerUnloaded;

    private bool _cViewerCommandActive;

    private bool _cViewerMpvActive;

    private bool _cViewerHostVisible;

    // The host facts as detail lines, attached once by the surface; read only for a taken host record.
    private Func<string>? _cViewerHostDetail;

    // Counts every host record asked, so a taken record shows how many went before it.
    private int _cViewerHostStamp;

    // While the wheel drags, the seeks are buffered as lines and written as one record when the drag ends.
    private bool _cViewerDragActive;

    private readonly List<string> _cViewerSeekLines = [];

    private TimeSpan _cViewerSeekFinal;

    private bool _cViewerOverlayShown;

    internal CViewer(LViewerPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cViewerPort = port;
    }

    public int CViewerSerial => _cViewerSerial;

    public bool CViewerUnloaded => _cViewerUnloaded;

    public bool CViewerCommandActive => _cViewerCommandActive;

    public bool CViewerMpvActive => _cViewerMpvActive;

    public bool CViewerHostVisible => _cViewerHostVisible;

    public int CViewerSerialChange() => ++_cViewerSerial;

    // A load lands only under the current serial, while the viewer is loaded and its command is active.
    public bool CViewerSerialCheck(int serial) =>
        !_cViewerUnloaded && serial == _cViewerSerial && _cViewerCommandActive;

    public void CViewerMpvSet(bool mpv) => _cViewerMpvActive = mpv;

    // The tenure ends once; every pending load is outdated with it.
    internal void CViewerUnloadedSet()
    {
        _cViewerUnloaded = true;
        _cViewerSerial++;
    }

    // The command turns on or off once per change, recorded, and never after the viewer unloaded. Off: the
    // driver's player suspends first, then every pending load is outdated. On: the driver's restore runs with the
    // command already on, and the player resumes only when nothing was restored.
    public void CViewerCommandSet(bool active, Action suspend, Func<bool> restore, Action resume)
    {
        ArgumentNullException.ThrowIfNull(suspend);
        ArgumentNullException.ThrowIfNull(restore);
        ArgumentNullException.ThrowIfNull(resume);
        if (active)
        {
            CViewerRestoreRun(restore, resume);
            return;
        }

        CViewerSuspendRun(suspend);
    }

    // The loupe takes the viewer over: the host hides, then the command turns off as in CViewerCommandSet.
    public void CViewerLoupeAttach(Action raise, Action suspend)
    {
        ArgumentNullException.ThrowIfNull(raise);
        ArgumentNullException.ThrowIfNull(suspend);
        CViewerHostShow(false, raise);
        CViewerSuspendRun(suspend);
    }

    // The command turns off: the driver's player suspends first, then every pending load is outdated.
    private void CViewerSuspendRun(Action suspend)
    {
        if (!CViewerCommandChange(false))
        {
            return;
        }

        suspend();
        _cViewerCommandActive = false;
        _cViewerSerial++;
    }

    // The command turns on: the driver's restore runs with the command already on, and the player resumes only
    // when nothing was restored.
    internal void CViewerRestoreRun(Func<bool> restore, Action resume)
    {
        if (!CViewerCommandChange(true))
        {
            return;
        }

        _cViewerCommandActive = true;
        if (restore())
        {
            return;
        }

        resume();
    }

    // A change is taken once, recorded, and never after the viewer unloaded.
    private bool CViewerCommandChange(bool active)
    {
        if (_cViewerUnloaded || _cViewerCommandActive == active)
        {
            return false;
        }

        CViewerHostRecord($"command set {(active ? "on" : "off")}");
        return true;
    }

    // The host shows or hides and the driver's renderer is told at once; a change is recorded only for Flyleaf.
    public void CViewerHostShow(bool visible, Action raise)
    {
        ArgumentNullException.ThrowIfNull(raise);
        bool changed = visible != _cViewerHostVisible;
        _cViewerHostVisible = visible;
        raise();
        if (changed && !_cViewerMpvActive)
        {
            CViewerHostRecord($"host {(visible ? "shown" : "hidden")}");
        }
    }

    public void CViewerHostAttach(Func<string> detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        _cViewerHostDetail = detail;
    }

    // The driver hands in one teardown stage; a stage that throws is traced and the next one still runs.
    public void CViewerStageRun(string stage, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            action();
        }
        catch (Exception failure)
        {
            _cViewerPort.LViewerStageRecord(stage, failure);
        }
    }

    // A reused player that is ready keeps its host; any other commit swaps the player.
    public void CViewerPlayerRecord(bool reused, bool ready)
    {
        if (reused && ready)
        {
            _cViewerPort.LViewerReusedRecord();
            return;
        }

        _cViewerPort.LViewerSwappedRecord(ready);
    }

    // The host record is taken only while verbose tracing is on and the host facts are attached; its stamp
    // counts every ask, taken or not. The detail is read only for a taken record.
    public void CViewerHostRecord(string stage)
    {
        if (_cViewerHostDetail is not { } detail || !_cViewerPort.LViewerVerboseRead())
        {
            _cViewerHostStamp++;
            return;
        }

        _cViewerPort.LViewerHostRecord(++_cViewerHostStamp, stage, detail());
    }

    // A drag start opens an empty buffer; a drag end writes the buffered seeks as one record, if any.
    public void CViewerDragSet(bool dragging)
    {
        if (dragging)
        {
            if (!_cViewerDragActive)
            {
                _cViewerSeekLines.Clear();
            }

            _cViewerDragActive = true;
            return;
        }

        _cViewerDragActive = false;
        if (_cViewerSeekLines.Count == 0)
        {
            return;
        }

        _cViewerPort.LViewerDragRecord(_cViewerSeekFinal, _cViewerSeekLines);
        _cViewerSeekLines.Clear();
    }

    // A seek outside a drag is written at once; inside a drag it is buffered, and only while verbose tracing is on.
    public void CViewerSeekRecord(TimeSpan position, bool direct, bool running)
    {
        if (!_cViewerDragActive)
        {
            _cViewerPort.LViewerSeekRecord(position, direct, running);
            return;
        }

        if (!_cViewerPort.LViewerVerboseRead())
        {
            return;
        }

        _cViewerSeekLines.Add(_cViewerPort.LViewerSeekFormat(position, direct, running));
        _cViewerSeekFinal = position;
    }

    // The overlay shows while mpv is shown over a visible host with a size. Only a change of the shown state is
    // written: opened when it shows, closed when it hides.
    public bool CViewerOverlayCheck(bool hostVisible, bool mpvShown, double width, double height)
    {
        bool shown = mpvShown && hostVisible && width > 0 && height > 0;
        if (shown && !_cViewerOverlayShown)
        {
            _cViewerPort.LViewerShownRecord(width, height);
        }
        else if (!shown && _cViewerOverlayShown)
        {
            _cViewerPort.LViewerHiddenRecord(hostVisible, mpvShown, width, height);
        }

        _cViewerOverlayShown = shown;
        return shown;
    }

    public void CViewerRefreshRecord(Exception failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        _cViewerPort.LViewerRefreshRecord(failure.Message);
    }

    // The driver hands in its seek; a seek the player rejects is traced and answers false. The seek writes its own
    // record before it runs.
    public bool CViewerSeekRun(TimeSpan position, Action<TimeSpan> seek)
    {
        ArgumentNullException.ThrowIfNull(seek);
        try
        {
            seek(position);
            return true;
        }
        catch (Exception failure)
        {
            _cViewerPort.LViewerRejectedRecord(position, failure.Message);
            return false;
        }
    }

    public void CViewerNoticeRecord(Exception failure) => _cViewerPort.LViewerNoticeRecord(failure);

    public void CViewerRotatedRecord(string rotate, bool horizontal, bool vertical, bool ready) =>
        _cViewerPort.LViewerRotatedRecord(rotate, horizontal, vertical, ready);

    public void CViewerClosedRecord(string path) => _cViewerPort.LViewerClosedRecord(path);

    public void CViewerDetachedRecord(bool ready) => _cViewerPort.LViewerDetachedRecord(ready);

    internal void CViewerLoadRecord(int serial, string path, string engine) =>
        _cViewerPort.LViewerLoadRecord(serial, path, engine);

    internal void CViewerProbeRecord(string kind, string path, string? error) =>
        _cViewerPort.LViewerProbeRecord(kind, path, error);

    internal void CViewerSupersededRecord(int serial, bool unloaded) =>
        _cViewerPort.LViewerSupersededRecord(serial, unloaded);

    public void CViewerDiscardedRecord(int serial, int current, bool unloaded) =>
        _cViewerPort.LViewerDiscardedRecord(serial, current, unloaded);

    public void CViewerPreviewRecord(string rotate, bool horizontal, bool vertical) =>
        _cViewerPort.LViewerPreviewRecord(rotate, horizontal, vertical);

    internal void CViewerRequestedRecord(string path, bool active, bool unloaded, string engine) =>
        _cViewerPort.LViewerRequestedRecord(path, active, unloaded, engine);

    internal void CViewerRefusedRecord(bool active) => _cViewerPort.LViewerRefusedRecord(active);

    internal void CViewerUnresolvedRecord() => _cViewerPort.LViewerUnresolvedRecord();
}
