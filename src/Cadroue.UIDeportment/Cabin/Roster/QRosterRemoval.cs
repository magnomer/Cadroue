using Cadroue.Core;

namespace Cadroue.UIDeportment;

// A batch's removal: the schedule's removable members, the confirm that asks for them, the removal and its warning.
public sealed class QRosterRemoval
{
    private readonly LScheduleContract qSchedule;
    private readonly LScheduleRemoverContract qScheduleRemover;

    public QRosterRemoval(LScheduleContract lScheduleOwner, LScheduleRemoverContract lRemoverOwner)
    {
        qSchedule = lScheduleOwner;
        qScheduleRemover = lRemoverOwner;
    }

    public event Action<string, string>? QRosterWarningShown;

    public void QRosterRemove(Guid lBatchId) =>
        QRosterRemoveConfirm(qScheduleRemover.LScheduleRemovableRead(QRosterMembersRead(lBatchId)));

    private IEnumerable<Guid> QRosterMembersRead(Guid lBatchId) =>
        qSchedule.LScheduleRecords
            .Where(lWorkItem => lWorkItem.LWorkBatchId == lBatchId)
            .Select(lWorkItem => lWorkItem.LWorkId);

    private void QRosterRemoveConfirm(IReadOnlyList<Guid> lRemovable)
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

        QRosterBatchRemove(lRemovable);
    }

    private void QRosterBatchRemove(IReadOnlyList<Guid> lRemovable) =>
        QRosterWarningRaise(QConsoleLine.QConsoleRemovalFormat(qScheduleRemover.LScheduleBatchRemove(lRemovable)));

    private void QRosterWarningRaise(string? lMessage)
    {
        if (lMessage is not null)
        {
            QRosterWarningShown?.Invoke(QLocalizationCatalog.QLocalizationTextRead("Console.Remove.Title"), lMessage);
        }
    }
}
