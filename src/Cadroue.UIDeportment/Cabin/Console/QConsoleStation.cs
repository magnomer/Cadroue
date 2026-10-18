using Cadroue.Conduct;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The station switch's wiring: the surface's previous and next steps go to the console gate. The depot watch stays
// here until a ShellEngine seam can start one.
public sealed class QConsoleStation
{
    public const int QConsoleStepBack = -1;
    public const int QConsoleStepForward = 1;

    private readonly CConsole qConsoleGate;
    private readonly LDepotWatch qDepotWatch = new();

    public QConsoleStation(CConsole lGate)
    {
        qConsoleGate = lGate;
        qDepotWatch.LDepotChanged += QConsoleWatchHandle;
    }

    public event Action? QConsoleWatchChanged;

    public void QConsoleWatchStart() => qDepotWatch.LDepotWatchStart();

    public void QConsoleWatchStop()
    {
        qDepotWatch.LDepotChanged -= QConsoleWatchHandle;
        qDepotWatch.Dispose();
    }

    private void QConsoleWatchHandle() => QConsoleWatchChanged?.Invoke();

    public void QConsoleStationMove(int lStep) => qConsoleGate.CConsoleStationMove(lStep);
}
