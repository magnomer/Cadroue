using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

// The relay's arrival side: one strip for the process, and an arriving path goes to the target tab's Auto Relay action.
public static class QActionRelay
{
    private static QStrip? qActionStrip;

    public static void QActionStripAttach(QStrip lStrip) => qActionStrip = lStrip;

    public static void QActionAccept(Guid lTargetTab, string lPath, Guid lCohort)
    {
        if (QActionAutoFind(lTargetTab) is null)
        {
            return;
        }

        LSealPending.LSealPendingAdd(lCohort);
        QProgram.QProgramDefer(() => QActionAcceptRun(lTargetTab, lPath, lCohort));
    }

    private static void QActionAcceptRun(Guid lTargetTab, string lPath, Guid lCohort)
    {
        try
        {
            if (QActionAutoFind(lTargetTab) is { } lAction)
            {
                lAction.QActionItemsRun(new[] { lPath });
            }
            else
            {
                QActionRelayRecord(lPath);
            }
        }
        finally
        {
            LSealPending.LSealPendingRemove(lCohort);
            LSeal.LSealRun();
        }
    }

    private static void QActionRelayRecord(string lPath) =>
        LTraceLog.LTraceWarningRecord(
            $"Relay left '{System.IO.Path.GetFileName(lPath)}' unprocessed: "
            + "the destination tab closed or left Auto Relay before it ran");

    private static QAction? QActionAutoFind(Guid lTargetTab) =>
        qActionStrip?.QStripTabFind(lTargetTab)
            is { QStripTabMerge: false, QStripTabAction: { QActionAutoRelay: true } lAction }
            ? lAction
            : null;
}
