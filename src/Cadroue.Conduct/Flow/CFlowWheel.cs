using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The flow's wheel: what a turn does by the wheel preference, a zoom about the cursor, a volume change or a seek by
// a step. CFlow makes one and answers its switches, range and cursor; the range passes to the port unread.
public sealed class CFlowWheel
{
    // The volume one notch changes.
    private const double CFlowWheelVolume = 5;

    private readonly LFlowPort _cFlowWheelPort;
    private readonly CFlow _cFlowWheelOwner;

    internal CFlowWheel(LFlowPort port, CFlow owner)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(owner);
        _cFlowWheelPort = port;
        _cFlowWheelOwner = owner;
    }

    // A zoom changed the range: its readers follow it.
    public event Action? CFlowWheelZoomed;

    // Told with the volume change a turn asks for.
    public event Action<double>? CFlowWheelAdjusted;

    // A wheel turn while commands run, in raw units and in steps: it zooms, changes the volume or seeks a step.
    public bool CFlowWheelRun(int delta, int steps)
    {
        if (!_cFlowWheelOwner.CFlowCommandActive || delta == 0)
        {
            return false;
        }

        switch (_cFlowWheelPort.LFlowWheelRead())
        {
            case "Zoom":
                CFlowWheelZoom(steps);
                break;
            case "Volume":
                CFlowWheelAdjusted?.Invoke(steps * CFlowWheelVolume);
                break;
            default:
                CFlowWheelSeek(steps);
                break;
        }

        return true;
    }

    public void CFlowWheelZoom(int steps)
    {
        if (_cFlowWheelOwner.CFlowRange is not { } range)
        {
            return;
        }

        _cFlowWheelPort.LFlowRangeZoom(range, _cFlowWheelOwner.CFlowCursor, steps);
        CFlowWheelZoomed?.Invoke();
    }

    private void CFlowWheelSeek(int steps)
    {
        if (_cFlowWheelOwner.CFlowRange is not { } range)
        {
            return;
        }

        _cFlowWheelOwner.CFlowCursorSeek(
            _cFlowWheelOwner.CFlowCursorClamp(
                _cFlowWheelOwner.CFlowCursor + _cFlowWheelPort.LFlowStepResolve(range, steps)));
    }
}
