using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The flow's drag: each drag start and end is told, and while commands run and the preference asks, a drag start
// pauses playing media and its end plays what it paused. CFlow makes one and answers its switch and its play tells.
public sealed class CFlowDrag
{
    private readonly LFlowPort _cFlowDragPort;
    private readonly CFlow _cFlowDragOwner;

    internal CFlowDrag(LFlowPort port, CFlow owner)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(owner);
        _cFlowDragPort = port;
        _cFlowDragOwner = owner;
    }

    // Told at every drag start and end, whether or not commands run.
    public event Action<bool>? CFlowDragChanged;

    // A drag paused the playing media; the drag's end plays it again.
    public bool CFlowDragPaused { get; private set; }

    // While commands run and the preference asks, a drag start pauses playing media; its end plays what it paused.
    public void CFlowDragSet(bool dragging, Func<bool>? playing)
    {
        CFlowDragChanged?.Invoke(dragging);
        if (!_cFlowDragOwner.CFlowCommandActive || !_cFlowDragPort.LFlowDragRead())
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
            _cFlowDragOwner.CFlowPauseRaise();
            return;
        }

        if (!CFlowDragPaused)
        {
            return;
        }

        CFlowDragPaused = false;
        _cFlowDragOwner.CFlowPlayRaise();
    }
}
