namespace Cadroue.UIDeportment;

public sealed class QRosterDetail
{
    private readonly Func<bool> lRosterClosed;
    private readonly Action lRosterDetail;
    private bool lRosterDetailPending;

    public QRosterDetail(Func<bool> lClosedRead, Action lDetailRaise)
    {
        lRosterClosed = lClosedRead;
        lRosterDetail = lDetailRaise;
    }

    public event Action? LRosterDetailDeferred;

    public bool LRosterDetailPending => lRosterDetailPending;

    public void LRosterDeferRaise()
    {
        if (lRosterDetailPending)
        {
            return;
        }

        lRosterDetailPending = true;
        LRosterDetailDeferred?.Invoke();
    }

    public void LRosterDetailRun()
    {
        lRosterDetailPending = false;
        if (!lRosterClosed())
        {
            lRosterDetail();
        }
    }
}
