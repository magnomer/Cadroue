using System.Windows.Threading;

namespace Cadroue.UIVeneer;

internal sealed class PDebounce
{
    private readonly DispatcherTimer pDebounceTimer;
    private readonly Action pDebounceAction;

    public PDebounce(TimeSpan pInterval, Action pAction)
    {
        pDebounceTimer = new DispatcherTimer { Interval = pInterval };
        pDebounceAction = pAction;
        pDebounceTimer.Tick += PDebounceTick;
    }

    public void PDebounceDefer()
    {
        pDebounceTimer.Stop();
        pDebounceTimer.Start();
    }

    public void PDebounceStop() => pDebounceTimer.Stop();

    private void PDebounceTick(object? pSender, EventArgs pEvent)
    {
        pDebounceTimer.Stop();
        pDebounceAction();
    }
}
