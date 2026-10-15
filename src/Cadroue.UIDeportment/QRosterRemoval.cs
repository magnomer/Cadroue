
namespace Cadroue.UIDeportment;

public sealed class QRosterRemoval
{
    public event Action<IReadOnlyList<Guid>>? QRosterRemovalConfirmed;

    public event Action<string, string>? QRosterWarningShown;

    public void QRosterRemoveConfirm(IReadOnlyList<Guid> lRemovable)
    {
        if (lRemovable.Count == 0)
        {
            return;
        }

        QAsk? lAsk = QAskNotice.QAskCreate(
            () => QLocalizationCatalog.QLocalizationFormat("Roster.Card.Confirm", lRemovable.Count),
            "Terms.Remove",
            "Console.Confirm.Title");
        QAskNotice.QAskPublish(lAsk, lAnswer => QRosterRemoveRun(lAnswer, lRemovable));
    }

    private void QRosterRemoveRun(bool lAnswer, IReadOnlyList<Guid> lRemovable)
    {
        if (!lAnswer)
        {
            return;
        }

        QRosterRemovalConfirmed?.Invoke(lRemovable);
    }

    public void QRosterWarningRaise(string? lMessage)
    {
        if (lMessage is not null)
        {
            QRosterWarningShown?.Invoke(QLocalizationCatalog.QLocalizationTextRead("Console.Remove.Title"), lMessage);
        }
    }
}
