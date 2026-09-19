using System.ComponentModel;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LStripTab : INotifyPropertyChanged
{
    private const string LStripFunnelKey = "Funnel";
    private const string LStripMergeKey = "Merge";
    private const string LStripWorklistKey = "Worklist";
    private Func<bool>? lStripRelaySource;
    private Func<Guid, bool>? lStripCohortSource;

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

    public LPreset? LStripTabPreset => LStripTabWorkspace?.LWorkspacePreset;

    public LDocket? LStripTabDocket => LStripTabWorkspace?.LWorkspaceDocket;

    public bool LStripTabBusy => LStripTabWorkspace?.LWorkspaceBusyCheck() == true;

    public bool LStripTabFunnel => string.Equals(LStripTabKey, LStripFunnelKey, StringComparison.Ordinal);

    public bool LStripTabMerge => string.Equals(LStripTabKey, LStripMergeKey, StringComparison.Ordinal);

    public bool LStripTabWorklist => string.Equals(LStripTabKey, LStripWorklistKey, StringComparison.Ordinal);

    public void LStripWorkspaceAttach(LWorkspace lWorkspace) => LStripTabWorkspace = lWorkspace;

    public void LStripActionAttach(Func<bool> lRelaySource, Func<Guid, bool> lCohortSource)
    {
        lStripRelaySource = lRelaySource;
        lStripCohortSource = lCohortSource;
    }

    public LSceneTabRecord? LStripLayoutRead() => LStripTabWorkspace?.LWorkspaceLayoutRead();

    public bool LStripRelayCheck() => lStripRelaySource?.Invoke() ?? false;

    public bool LStripCohortRun(Guid lCohort) => lStripCohortSource?.Invoke(lCohort) ?? false;

    internal void LStripTabUpdate() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}
