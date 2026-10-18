using System;

namespace Cadroue.Conduct;

// The skip tool decisions: whether skipping applies and whether it is kept, and when a change is told. A switch change
// is told as a change, then as a switch change.
public sealed class CSkip
{
    private bool _cSkipActive;
    private bool _cSkipPersistent;

    internal CSkip()
    {
    }

    public event Action? CSkipChanged;

    public event Action? CSkipActiveChanged;

    public bool CSkipActive => _cSkipActive;

    public bool CSkipPersistent => _cSkipPersistent;

    public void CSkipActiveSet(bool active)
    {
        if (_cSkipActive == active)
        {
            return;
        }

        _cSkipActive = active;
        CSkipChanged?.Invoke();
        CSkipActiveChanged?.Invoke();
    }

    public void CSkipPersistentSet(bool persistent)
    {
        if (_cSkipPersistent == persistent)
        {
            return;
        }

        _cSkipPersistent = persistent;
        CSkipChanged?.Invoke();
    }
}
