namespace Cadroue.UIDeportment;

public sealed class LSkip
{
    private bool lSkipActive;
    private bool lSkipPersistent;

    public event Action? LSkipChange;

    public bool LSkipActive => lSkipActive;

    public bool LSkipPersistent => lSkipPersistent;

    public void LSkipActiveSet(bool lActive)
    {
        if (lSkipActive == lActive)
        {
            return;
        }

        lSkipActive = lActive;
        LSkipChange?.Invoke();
    }

    public void LSkipPersistentSet(bool lPersistent)
    {
        if (lSkipPersistent == lPersistent)
        {
            return;
        }

        lSkipPersistent = lPersistent;
        LSkipChange?.Invoke();
    }
}
