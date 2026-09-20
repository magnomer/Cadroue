using System.ComponentModel;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LStripTab : INotifyPropertyChanged
{
    private const string LStripFunnelKey = "Funnel";
    private const string LStripMergeKey = "Merge";
    private const string LStripWorklistKey = "Worklist";

    public LStripTab(string lKey)
    {
        LStripTabId = Guid.NewGuid();
        LStripTabKey = lKey;
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

    public LWorkspace? LStripTabWorkspace { get; private set; }

    public LAction? LStripTabAction { get; private set; }

    public LPreset? LStripTabPreset => LStripTabWorkspace?.LWorkspacePreset;

    public LDocket? LStripTabDocket => LStripTabWorkspace?.LWorkspaceDocket;

    public bool LStripTabBusy => LStripTabWorkspace?.LWorkspaceBusyCheck() == true;

    public bool LStripTabFunnel => string.Equals(LStripTabKey, LStripFunnelKey, StringComparison.Ordinal);

    public bool LStripTabMerge => string.Equals(LStripTabKey, LStripMergeKey, StringComparison.Ordinal);

    public bool LStripTabWorklist => string.Equals(LStripTabKey, LStripWorklistKey, StringComparison.Ordinal);

    public void LStripWorkspaceAttach(LWorkspace lWorkspace) => LStripTabWorkspace = lWorkspace;

    public void LStripActionAttach(LAction lAction) => LStripTabAction = lAction;

    public LSceneTabRecord? LStripLayoutRead() => LStripTabWorkspace?.LWorkspaceLayoutRead();

    public bool LStripRelayCheck() => LStripTabAction?.LActionAutoRelay ?? false;

    public bool LStripCohortRun(Guid lCohort) => LStripTabAction?.LActionCohortRun(lCohort) ?? false;

    internal void LStripTabUpdate() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
