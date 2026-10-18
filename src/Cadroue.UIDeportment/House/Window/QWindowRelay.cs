using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The window's tab intake: a tab relayed from another window is taken on the dispatcher, added with its preset,
// layout and name, selected and handed its payload; a relay that cannot be taken is logged and refused.
public sealed class QWindowRelay
{
    private readonly QWindow qWindow;

    public QWindowRelay(QWindow lOwner)
    {
        qWindow = lOwner;
    }

    public event Action? QWindowShown;

    public void QWindowDispatcherAttach(Func<Func<bool>, bool> lDispatch) =>
        LRelayChannel.LRelayAcceptSeam = lRelay => QWindowRelayHandle(lRelay, lDispatch);

    internal static void QWindowDispatcherDetach() => LRelayChannel.LRelayAcceptSeam = null;

    internal void QWindowRelayAccept(LRelay lRelay)
    {
        qWindow.QWindowTabRaise(new QWindowTab(
            lRelay.LRelayLayoutKey,
            LPreset.LPresetStateCreate(lRelay.LRelayExport),
            lRelay.LRelayLayout,
            lRelay.LRelayCustomName));
        QStripTab? lAdded = qWindow.QWindowStrip.QStripTabs.LastOrDefault();
        qWindow.QWindowStrip.QStripSelect(lAdded);
        lAdded?.QStripTabWorkspace.QWorkspaceRelay.QWorkspaceRelayApply(lRelay);
        qWindow.QWindowFunnelRaise();
    }

    private bool QWindowRelayHandle(LRelay lRelay, Func<Func<bool>, bool> lDispatch)
    {
        try
        {
            return lDispatch(() =>
            {
                QWindowRelayAccept(lRelay);
                QWindowShown?.Invoke();
                return true;
            });
        }
        catch (Exception lRelayException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Relayed '{lRelay.LRelayLayoutKey}' tab could not be taken; refused",
                lRelayException);
            return false;
        }
    }
}
