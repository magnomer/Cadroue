namespace Cadroue.UIDeportment;

public sealed class LSkip
{
    private bool lSkipActive;
    private bool lSkipPersistent;

    public event Action? LSkipChanged;
    public event Action? LSkipActiveChanged;

    public bool LSkipActive => lSkipActive;

    public bool LSkipPersistent => lSkipPersistent;

    public void LSkipActiveSet(bool lActive)
    {
        if (lSkipActive == lActive)
        {
            return;
        }

        lSkipActive = lActive;
        LSkipChanged?.Invoke();
        LSkipActiveChanged?.Invoke();
    }

    public void LSkipPersistentSet(bool lPersistent)
    {
        if (lSkipPersistent == lPersistent)
        {
            return;
        }

        lSkipPersistent = lPersistent;
        LSkipChanged?.Invoke();
    }
}
