using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The strip's tab titles: each tab's standard, numbered or custom title, and the user's rename.
public sealed class QStripTitle
{
    private readonly QStrip qStrip;
    private readonly Func<string, string> qStripTitleSource;
    private readonly Func<string, int, string> qStripNumberSource;

    public QStripTitle(QStrip lOwner, Func<string, string> lTitleSource, Func<string, int, string> lNumberSource)
    {
        qStrip = lOwner;
        qStripTitleSource = lTitleSource;
        qStripNumberSource = lNumberSource;
    }

    public event Action? QStripTitleChanged;

    public static string QStripTitleResolve(string lKey) => QLocalizationCatalog.QLocalizationTextRead("Tab." + lKey);

    // A tab takes its kind's standard title as it joins the strip.
    public void QStripTitleReset(QStripTab lTab) => QStripTitleSet(lTab, qStripTitleSource(lTab.QStripTabKey));

    public void QStripTitleUpdate()
    {
        if (qStrip.QStripSuspended)
        {
            return;
        }

        IReadOnlyList<LTabsetSlot> lSlots = QStripSlotsCreate();
        foreach (LTabsetTitlePlan lPlan in LTabset.LTabsetTitleResolve(lSlots))
        {
            if (qStrip.QStripTabFind(lPlan.LTabsetId) is not { } lTab)
            {
                continue;
            }

            string lTitle = QStripTitleFormat(lTab, lPlan);
            QStripTitleSet(lTab, lTitle);
        }

        QStripTitleChanged?.Invoke();
    }

    private IReadOnlyList<LTabsetSlot> QStripSlotsCreate() =>
        qStrip.QStripTabs
            .Select(lTab => new LTabsetSlot(lTab.QStripTabId, lTab.QStripTabKey, lTab.QStripTabCustom))
            .ToList();

    private string QStripTitleFormat(QStripTab lTab, LTabsetTitlePlan lPlan) =>
        lPlan.LTabsetCustom
            ? lTab.QStripTabCustom
            : lPlan.LTabsetNumbered
                ? qStripNumberSource(qStripTitleSource(lTab.QStripTabKey), lPlan.LTabsetOrdinal)
                : qStripTitleSource(lTab.QStripTabKey);

    public bool QStripNameSet(QStripTab lTab, string? lName)
    {
        bool lHadCustom = QStripCustomCheck(lTab);
        string lTrimmed = QStripNameNormalize(lName);
        if (QStripStandardCheck(lTab, lTrimmed))
        {
            QStripCustomSet(lTab, string.Empty);
            QStripTitleUpdate();
            if (lHadCustom)
            {
                LTraceLog.LTraceInfoRecord($"Tab name reset to the standard name for {lTab.QStripTabKey}");
            }

            return false;
        }

        List<string> lTaken = QStripTakenRead(lTab);
        QStripCustomSet(lTab, LTabset.LTabsetNameResolve(lTaken, lTrimmed, qStripNumberSource));
        QStripTitleUpdate();
        LTraceLog.LTraceInfoRecord($"Tab renamed to '{lTab.QStripTabTitle}' ({lTab.QStripTabKey})");
        return true;
    }

    private static bool QStripCustomCheck(QStripTab lTab) => lTab.QStripTabCustom.Length > 0;

    private static string QStripNameNormalize(string? lName) => (lName ?? string.Empty).Trim();

    private bool QStripStandardCheck(QStripTab lTab, string lTrimmed) =>
        lTrimmed.Length == 0
        || string.Equals(lTrimmed, qStripTitleSource(lTab.QStripTabKey), StringComparison.Ordinal);

    private List<string> QStripTakenRead(QStripTab lTab) =>
        qStrip.QStripTabs
            .Where(lOther => !ReferenceEquals(lOther, lTab))
            .Select(lOther => lOther.QStripTabTitle)
            .ToList();

    private static void QStripCustomSet(QStripTab lTab, string lCustom) => lTab.QStripTabCustom = lCustom;

    private void QStripTitleSet(QStripTab lTab, string lTitle)
    {
        if (lTab.QStripTabTitle == lTitle)
        {
            return;
        }

        lTab.QStripTabTitle = lTitle;
        qStrip.QStripTabRaise(lTab);
    }
}
