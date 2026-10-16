using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The viewer's trace decisions: whether and which record is written, with the state only the records use (the
// host stamp, the drag seek buffer, the overlay shown flag). The words are the outlet's. One per viewer, built
// with its workspace.
public sealed class CViewer
{
    private readonly LViewerPort _cViewerPort;

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
    public void CViewerHostRecord(string stage, Func<string>? detail)
    {
        if (detail is null || !_cViewerPort.LViewerVerboseRead())
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

    public void CViewerStageRecord(string stage, Exception failure) => _cViewerPort.LViewerStageRecord(stage, failure);

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
