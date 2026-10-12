namespace Cadroue.UIDeportment;

public sealed class QSkip
{
    private bool qSkipActive;
    private bool qSkipPersistent;

    public event Action? QSkipChanged;
    public event Action? QSkipActiveChanged;

    public bool QSkipActive => qSkipActive;

    public bool QSkipPersistent => qSkipPersistent;

    public void QSkipActiveSet(bool lActive)
    {
        if (qSkipActive == lActive)
        {
            return;
        }

        qSkipActive = lActive;
        QSkipChanged?.Invoke();
        QSkipActiveChanged?.Invoke();
    }

    public void QSkipPersistentSet(bool lPersistent)
    {
        if (qSkipPersistent == lPersistent)
        {
            return;
        }

        qSkipPersistent = lPersistent;
        QSkipChanged?.Invoke();
    }
}
