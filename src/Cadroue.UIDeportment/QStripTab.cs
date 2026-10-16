using System.ComponentModel;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QStripTab : INotifyPropertyChanged
{
    private const string QStripFunnelKey = "Funnel";
    private const string QStripMergeKey = "Merge";
    private const string QStripWorklistKey = "Worklist";

    public QStripTab(
        string lKey,
        CUsher lUsher,
        CInfo lInfo,
        CLineage lLineage,
        CWorkspace lWorkspace,
        CInspector lInspector,
        CEdit lEdit)
    {
        QStripTabId = Guid.NewGuid();
        QStripTabKey = lKey;
        QStripTabWorkspace = new QWorkspace(lKey, lUsher, lInfo, lLineage, lWorkspace, lInspector, lEdit);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid QStripTabId { get; }

    public string QStripTabKey { get; }

    public string QStripTabTitle { get; internal set; } = string.Empty;

    public string QStripTabCustom { get; internal set; } = string.Empty;

    public bool QStripTabSelected { get; internal set; }

    public bool QStripTabSeparator { get; internal set; }

    public bool QStripTabEditing { get; internal set; }

    public bool QStripTabPending { get; internal set; }

    public QWorkspace QStripTabWorkspace { get; }

    public QAction? QStripTabAction { get; private set; }

    public LPreset? QStripTabPreset => QStripTabWorkspace.QWorkspacePreset;

    public LDocket? QStripTabDocket => QStripTabWorkspace.QWorkspaceDocket;

    public bool QStripTabBusy => QStripTabWorkspace.QWorkspaceBusyCheck();

    public bool QStripTabFunnel => string.Equals(QStripTabKey, QStripFunnelKey, StringComparison.Ordinal);

    public bool QStripTabMerge => string.Equals(QStripTabKey, QStripMergeKey, StringComparison.Ordinal);

    public bool QStripTabWorklist => string.Equals(QStripTabKey, QStripWorklistKey, StringComparison.Ordinal);

    public void QStripActionAttach(QAction lAction) => QStripTabAction = lAction;

    public LSceneTabEntry? QStripLayoutRead() => QStripTabWorkspace.QWorkspaceLayoutRead();

    public bool QStripRelayCheck() => QStripTabAction?.QActionAutoRelay ?? false;

    public bool QStripCohortRun(Guid lCohort) => QStripTabAction?.QActionCohortRun(lCohort) ?? false;

    internal void QStripTabUpdate() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
