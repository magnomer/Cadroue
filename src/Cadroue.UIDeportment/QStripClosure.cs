using System.Collections.ObjectModel;

namespace Cadroue.UIDeportment;

public sealed class QStripClosure
{
    private readonly LStrip lStrip;
    private readonly ObservableCollection<LStripTab> lStripTabs;
    private readonly Func<string, string> lStripText;
    private readonly Action<string> lStripTrace;
    private readonly Action<Guid> lStripCartographer;

    public QStripClosure(
        LStrip lOwner,
        ObservableCollection<LStripTab> lTabs,
        Func<string, string> lLocalization,
        Action<string> lTrace,
        Action<Guid> lCartographer)
    {
        lStrip = lOwner;
        lStripTabs = lTabs;
        lStripText = lLocalization;
        lStripTrace = lTrace;
        lStripCartographer = lCartographer;
    }

    public event Action<LStripTab>? LStripTabClosed;

    public LAsk? LStripCloseResolve(LStripTab? lTab = null) =>
        lStrip.LStripBusyCheck(lTab)
            ? new LAsk(
                lStripText(
                    lTab is null ? "Tab.Close.BusyAllMessage" : "Tab.Close.BusyMessage"),
                lStripText("Terms.Stop"))
            {
                LAskTitle = lStripText("Tab.Close.BusyTitle")
            }
            : null;

    public bool LStripCloseConfirm(LStripTab? lTab = null)
    {
        bool lConfirmed = false;
        LAskNotice.LAskPublish(LStripCloseResolve(lTab), lAnswer => lConfirmed = lAnswer);
        if (!lConfirmed)
        {
            lStripTrace("Close declined: a worklist is still working");
        }

        return lConfirmed;
    }

    public void LStripClose(LStripTab lTab)
    {
        if (!lStripTabs.Contains(lTab))
        {
            return;
        }

        string lClosedTitle = lTab.LStripTabTitle;
        LStripTabClosed?.Invoke(lTab);
        lStripCartographer(lTab.LStripTabId);
        lStrip.LStripRemove(lTab);
        lStripTrace($"Tab closed '{lClosedTitle}': {lStripTabs.Count} tab(s) open");
    }

    public void LStripAllClose()
    {
        foreach (LStripTab lTab in lStripTabs.ToArray())
        {
            LStripTabClosed?.Invoke(lTab);
            lStripCartographer(lTab.LStripTabId);
        }

        lStrip.LStripClear();
        lStripTrace("All tabs closed");
    }
}
