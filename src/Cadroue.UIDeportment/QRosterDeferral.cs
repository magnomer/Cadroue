namespace Cadroue.UIDeportment;

public sealed class QRosterDeferral
{
    private readonly Func<bool> qRosterDeferralClosed;
    private readonly Action qRosterDeferralDetail;
    private bool qRosterDeferralPending;

    public QRosterDeferral(Func<bool> lClosedRead, Action lDetailRaise)
    {
        qRosterDeferralClosed = lClosedRead;
        qRosterDeferralDetail = lDetailRaise;
    }

    public event Action? QRosterDeferralRaised;

    public bool QRosterDeferralPending => qRosterDeferralPending;

    public void QRosterDeferralRaise()
    {
        if (qRosterDeferralPending)
        {
            return;
        }

        qRosterDeferralPending = true;
        QRosterDeferralRaised?.Invoke();
    }

    public void QRosterDeferralRun()
    {
        qRosterDeferralPending = false;
        if (!qRosterDeferralClosed())
        {
            qRosterDeferralDetail();
        }
    }
}
