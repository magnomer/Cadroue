using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The viewer's seek trace: a seek is written at once, or buffered while the wheel drags and written as one record
// when the drag ends; a seek the player rejects is traced. CViewer makes one per viewer.
public sealed class CViewerScrub
{
    private readonly LViewerPort _cViewerScrubPort;

    // While the wheel drags, the seeks are buffered as lines and written as one record when the drag ends.
    private bool _cViewerDragActive;

    private readonly List<string> _cViewerSeekLines = [];

    private TimeSpan _cViewerSeekFinal;

    internal CViewerScrub(LViewerPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cViewerScrubPort = port;
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

        _cViewerScrubPort.LViewerDragRecord(_cViewerSeekFinal, _cViewerSeekLines);
        _cViewerSeekLines.Clear();
    }

    // A seek outside a drag is written at once; inside a drag it is buffered, and only while verbose tracing is on.
    public void CViewerSeekRecord(TimeSpan position, bool direct, bool running)
    {
        if (!_cViewerDragActive)
        {
            _cViewerScrubPort.LViewerSeekRecord(position, direct, running);
            return;
        }

        if (!_cViewerScrubPort.LViewerVerboseRead())
        {
            return;
        }

        _cViewerSeekLines.Add(_cViewerScrubPort.LViewerSeekFormat(position, direct, running));
        _cViewerSeekFinal = position;
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
            _cViewerScrubPort.LViewerRejectedRecord(position, failure.Message);
            return false;
        }
    }
}
