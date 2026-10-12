using System.Collections.ObjectModel;

namespace Cadroue.UIDeportment;

public sealed class QStripClosure
{
    private readonly QStrip qStrip;
    private readonly ObservableCollection<QStripTab> qStripTabs;
    private readonly Func<string, string> qStripText;
    private readonly Action<string> qStripTrace;
    private readonly Action<Guid> qStripCartographer;

    public QStripClosure(
        QStrip lOwner,
        ObservableCollection<QStripTab> lTabs,
        Func<string, string> lLocalization,
        Action<string> lTrace,
        Action<Guid> lCartographer)
    {
        qStrip = lOwner;
        qStripTabs = lTabs;
        qStripText = lLocalization;
        qStripTrace = lTrace;
        qStripCartographer = lCartographer;
    }

    public event Action<QStripTab>? QStripTabClosed;

    public QAsk? QStripCloseResolve(QStripTab? lTab = null) =>
        qStrip.QStripBusyCheck(lTab)
            ? new QAsk(
                qStripText(
                    lTab is null ? "Tab.Close.BusyAllMessage" : "Tab.Close.BusyMessage"),
                qStripText("Terms.Stop"))
            {
                QAskTitle = qStripText("Tab.Close.BusyTitle")
            }
            : null;

    public bool QStripCloseConfirm(QStripTab? lTab = null)
    {
        bool lConfirmed = false;
        QAskNotice.QAskPublish(QStripCloseResolve(lTab), lAnswer => lConfirmed = lAnswer);
        if (!lConfirmed)
        {
            qStripTrace("Close declined: a worklist is still working");
        }

        return lConfirmed;
    }

    public void QStripClose(QStripTab lTab)
    {
        if (!qStripTabs.Contains(lTab))
        {
            return;
        }

        string lClosedTitle = lTab.QStripTabTitle;
        QStripTabClosed?.Invoke(lTab);
        qStripCartographer(lTab.QStripTabId);
        qStrip.QStripRemove(lTab);
        qStripTrace($"Tab closed '{lClosedTitle}': {qStripTabs.Count} tab(s) open");
    }

    public void QStripAllClose()
    {
        foreach (QStripTab lTab in qStripTabs.ToArray())
        {
            QStripTabClosed?.Invoke(lTab);
            qStripCartographer(lTab.QStripTabId);
        }

        qStrip.QStripClear();
        qStripTrace("All tabs closed");
    }
}
