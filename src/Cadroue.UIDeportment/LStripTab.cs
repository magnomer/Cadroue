using System.ComponentModel;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LStripTab : INotifyPropertyChanged
{
    private const string LStripFunnelKey = "Funnel";
    private const string LStripMergeKey = "Merge";
    private const string LStripWorklistKey = "Worklist";

    public LStripTab(string lKey, CUsher lUsher, CLineage lLineage)
    {
        LStripTabId = Guid.NewGuid();
        LStripTabKey = lKey;
        LStripTabWorkspace = new LWorkspace(lKey, lUsher, lLineage);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid LStripTabId { get; }

    public string LStripTabKey { get; }

    public string LStripTabTitle { get; internal set; } = string.Empty;

    public string LStripTabCustom { get; internal set; } = string.Empty;

    public bool LStripTabSelected { get; internal set; }

    public bool LStripTabSeparator { get; internal set; }

    public bool LStripTabEditing { get; internal set; }

    public bool LStripTabPending { get; internal set; }

    public LWorkspace LStripTabWorkspace { get; }

    public LAction? LStripTabAction { get; private set; }

    public LPreset? LStripTabPreset => LStripTabWorkspace.LWorkspacePreset;

    public LDocket? LStripTabDocket => LStripTabWorkspace.LWorkspaceDocket;

    public bool LStripTabBusy => LStripTabWorkspace.LWorkspaceBusyCheck();

    public bool LStripTabFunnel => string.Equals(LStripTabKey, LStripFunnelKey, StringComparison.Ordinal);

    public bool LStripTabMerge => string.Equals(LStripTabKey, LStripMergeKey, StringComparison.Ordinal);

    public bool LStripTabWorklist => string.Equals(LStripTabKey, LStripWorklistKey, StringComparison.Ordinal);

    public void LStripActionAttach(LAction lAction) => LStripTabAction = lAction;

    public LSceneTabEntry? LStripLayoutRead() => LStripTabWorkspace.LWorkspaceLayoutRead();

    public bool LStripRelayCheck() => LStripTabAction?.LActionAutoRelay ?? false;

    public bool LStripCohortRun(Guid lCohort) => LStripTabAction?.LActionCohortRun(lCohort) ?? false;

    internal void LStripTabUpdate() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
