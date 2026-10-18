using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// A tab dropped outside the rail goes to another window: its workspace is saved as a relay file and dispatched, a
// relayed tab closes here, and each outcome is traced.
public sealed class QRailRelay
{
    private readonly QStrip qStrip;

    internal QRailRelay(QStrip lStripOwner)
    {
        qStrip = lStripOwner;
    }

    public async Task<QRailRelayOutcome> QRailRelayRun(
        QStripTab? lTab,
        bool lInside,
        double lDipX,
        double lDipY,
        double lDeviceX,
        double lDeviceY)
    {
        if (lTab is null || QRailWorkspaceResolve(lTab, lInside) is not { } lWorkspace)
        {
            return QRailRelayOutcome.QRailRelaySkipped;
        }

        if (lTab.QStripTabBusy)
        {
            QRailRelayRecord(lTab, QRailRelayOutcome.QRailRelayBusy);
            return QRailRelayOutcome.QRailRelayBusy;
        }

        if (QRailRelaySave(lWorkspace, lTab, lDipX, lDipY) is not { } lRelayFilePath)
        {
            return QRailRelayOutcome.QRailRelaySkipped;
        }

        qStrip.QStripPendingSet(lTab, true);
        try
        {
            QRailRelayOutcome lOutcome = QRailOutcomeResolve(
                lTab,
                await LRelayDelivery.LRelayDispatch(lRelayFilePath, lDeviceX, lDeviceY));
            QRailRelayRecord(lTab, lOutcome);
            if (lOutcome == QRailRelayOutcome.QRailRelayRelayed)
            {
                qStrip.QStripClosure.QStripClose(lTab);
            }

            return lOutcome;
        }
        finally
        {
            qStrip.QStripPendingSet(lTab, false);
        }
    }

    private static QWorkspace? QRailWorkspaceResolve(QStripTab lTab, bool lInside) =>
        lInside || lTab.QStripTabPending ? null : lTab.QStripTabWorkspace;

    private static string? QRailRelaySave(QWorkspace lWorkspace, QStripTab lTab, double lDipX, double lDipY)
    {
        LRelay lRelay = lWorkspace.QWorkspaceRelay.QWorkspaceRelayCreate(lTab.QStripTabCustom, lDipX, lDipY);
        try
        {
            return LRelayStore.LRelayFileSave(lRelay);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Relay payload could not be written; tab kept", lException);
            return null;
        }
    }

    private static QRailRelayOutcome QRailOutcomeResolve(
        QStripTab lTab,
        LRelayOutcome lDispatched) => lDispatched switch
    {
        LRelayOutcome.LRelayOutcomeExisting or LRelayOutcome.LRelayOutcomeLaunched => lTab.QStripTabBusy
            ? QRailRelayOutcome.QRailRelayCopied
            : QRailRelayOutcome.QRailRelayRelayed,
        _ => QRailRelayOutcome.QRailRelayKept
    };

    private static void QRailRelayRecord(QStripTab lTab, QRailRelayOutcome lOutcome)
    {
        switch (lOutcome)
        {
            case QRailRelayOutcome.QRailRelayBusy:
                LTraceLog.LTraceInfoRecord($"Tab '{lTab.QStripTabTitle}' kept: the worklist is still working");
                break;
            case QRailRelayOutcome.QRailRelayCopied:
                LTraceLog.LTraceInfoRecord(
                    $"Tab '{lTab.QStripTabTitle}' copied: the worklist started working during the relay");
                break;
            case QRailRelayOutcome.QRailRelayRelayed:
                LTraceLog.LTraceInfoRecord($"Tab '{lTab.QStripTabTitle}' relayed");
                break;
            case QRailRelayOutcome.QRailRelayKept:
                LTraceLog.LTraceInfoRecord($"Tab '{lTab.QStripTabTitle}' kept: the other window did not take it");
                break;
        }
    }
}
