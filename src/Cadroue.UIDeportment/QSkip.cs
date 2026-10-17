using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The skip tool's wiring: answers the gate's switches and hands each switch to the gate.
public sealed class QSkip
{
    private readonly CSkip qSkipGate;

    public QSkip(CSkip lGate)
    {
        qSkipGate = lGate;
    }

    public event Action? QSkipChanged
    {
        add => qSkipGate.CSkipChanged += value;
        remove => qSkipGate.CSkipChanged -= value;
    }

    public bool QSkipActive => qSkipGate.CSkipActive;

    public bool QSkipPersistent => qSkipGate.CSkipPersistent;

    public void QSkipActiveSet(bool lActive) => qSkipGate.CSkipActiveSet(lActive);

    public void QSkipPersistentSet(bool lPersistent) => qSkipGate.CSkipPersistentSet(lPersistent);
}
