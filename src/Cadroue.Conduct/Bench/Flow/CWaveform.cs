using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The flow's waveform gate: when a scan starts, when its notice reaches the readers, whether the rail shows peaks and
// the outline it paints, from the flow's media and switches. CFlow makes one per flow; the engine scans and keeps the
// peaks.
public sealed class CWaveform
{
    private readonly LWaveformPort _cWaveformPort;
    private readonly CFlow _cWaveformFlow;

    internal CWaveform(LWaveformPort port, CFlow flow)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(flow);
        _cWaveformPort = port;
        _cWaveformFlow = flow;
        _cWaveformPort.LWaveformArrived += CWaveformHandle;
    }

    // Told on the scanner's thread while the flow is loaded: the peaks changed, and CWaveformApply tells the readers.
    public event Action? CWaveformArrived;

    // The readers repaint the rail.
    public event Action? CWaveformChanged;

    // The rail shows peaks only while the waveform is on and peaks are held.
    public bool CWaveformShown => _cWaveformFlow.CFlowWaveformActive && _cWaveformPort.LWaveformPeaksCheck();

    // A scan of the held source starts only while the flow is loaded and the waveform is on.
    public void CWaveformStart()
    {
        if (_cWaveformFlow.CFlowUnloaded || !_cWaveformFlow.CFlowWaveformActive)
        {
            return;
        }

        _cWaveformPort.LWaveformStart(
            _cWaveformFlow.CFlowSource, _cWaveformFlow.CFlowDuration, _cWaveformFlow.CFlowWaveformAudio);
    }

    public void CWaveformClear() => _cWaveformPort.LWaveformClear();

    public void CWaveformClose() => _cWaveformPort.LWaveformClose();

    public void CWaveformApply()
    {
        if (_cWaveformFlow.CFlowUnloaded)
        {
            return;
        }

        CWaveformChanged?.Invoke();
    }

    // The outline over a rail: its origin, then the top edge left to right and the bottom edge back, one column per
    // unit of width. Nothing is outlined, at the origin, while no peaks show; the outline stays flat at the rail's
    // middle while the rail is too narrow or too low or the range holds no level.
    public (double CWaveformOriginX, double CWaveformOriginY, IReadOnlyList<(double, double)> CWaveformPoints)
        CWaveformOutlineResolve(double width, double railTop, double railHeight, TimeSpan rangeStart, TimeSpan rangeEnd)
    {
        int columnCount = (int)Math.Ceiling(width);
        double[]? columns = _cWaveformFlow.CFlowWaveformActive
            ? _cWaveformPort.LWaveformRangeRead(rangeStart, rangeEnd, columnCount)
            : null;
        if (columns is null)
        {
            return (0, 0, Array.Empty<(double, double)>());
        }

        double centerY = railTop + railHeight / 2;
        if (columnCount <= 1 || railHeight <= 2 || columns.Length == 0)
        {
            return (0, centerY, Array.Empty<(double, double)>());
        }

        double columnWidth = width / columnCount;
        double halfHeight = railHeight / 2 - 1;
        var points = new List<(double, double)>(columns.Length * 2);
        for (int column = 1; column < columns.Length; column++)
        {
            points.Add((column * columnWidth, centerY - columns[column] * halfHeight));
        }

        for (int column = columns.Length - 1; column >= 0; column--)
        {
            points.Add((column * columnWidth, centerY + columns[column] * halfHeight));
        }

        return (0, centerY - columns[0] * halfHeight, points);
    }

    private void CWaveformHandle()
    {
        if (_cWaveformFlow.CFlowUnloaded)
        {
            return;
        }

        CWaveformArrived?.Invoke();
    }
}
