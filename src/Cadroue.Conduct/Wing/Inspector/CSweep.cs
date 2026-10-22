using System;

namespace Cadroue.Conduct;

// The detection sweep's medium-free state and its run-versus-stop decision.
public sealed class CSweep
{
    private bool cSweepRunning;
    private bool cSweepPersistent;

    public event Action<bool>? CSweepRunningChanged;
    public event Action<bool>? CSweepPersistentChanged;
    public event Action? CSweepRunApplied;
    public event Action? CSweepStopApplied;

    public bool CSweepRunning => cSweepRunning;

    public bool CSweepPersistent => cSweepPersistent;

    public void CSweepRunHandle()
    {
        if (cSweepRunning)
        {
            CSweepStopApplied?.Invoke();
        }
        else
        {
            CSweepRunApplied?.Invoke();
        }
    }

    public void CSweepRunningSet(bool running)
    {
        if (cSweepRunning == running)
        {
            return;
        }

        cSweepRunning = running;
        CSweepRunningChanged?.Invoke(running);
    }

    public void CSweepPersistentSet(bool persistent)
    {
        if (cSweepPersistent == persistent)
        {
            return;
        }

        cSweepPersistent = persistent;
        CSweepPersistentChanged?.Invoke(persistent);
    }
}
