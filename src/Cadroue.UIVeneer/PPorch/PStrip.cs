using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPorch;

public sealed class PStrip
{
    private readonly List<PWorkspace> pStripWorkspaces = new();

    public PStrip()
    {
        LStrip = new LStrip();
        LStrip.LStripChange += PStripStaleRemove;
        LStrip.LStripTabChange += PStripTabHandle;
        LStrip.LStripTabClose += PStripCloseHandle;
        LStrip.LStripTitleChange += PStripRelayUpdate;
        LStrip.LStripRelayAttach();
    }

    public LStrip LStrip { get; }

    public PWorkspace? PStripSelected => PStripWorkspaceRead(LStrip.LStripSelected);

    public PWorkspace? PStripWorkspaceRead(LStripTab? lStripTab) =>
        pStripWorkspaces.FirstOrDefault(pWorkspace => ReferenceEquals(pWorkspace.PWorkspaceTab, lStripTab));

    public LStripTab PStripAdd(
        string pTabLayoutKey,
        LPreset? lExportSpecificState = null,
        LSceneTabRecord? lPreferenceTabLayout = null)
    {
        LStripTab lStripTab = LStrip.LStripTabCreate(pTabLayoutKey);
        var pWorkspace = new PWorkspace(lStripTab, lExportSpecificState, lPreferenceTabLayout);
        pStripWorkspaces.Add(pWorkspace);
        LStrip.LStripAdd(lStripTab);
        return lStripTab;
    }

    public void PStripRelayUpdate() => LStrip.LStripTabs.ToList().ForEach(PStripRelayApply);

    private void PStripRelayApply(LStripTab lStripTab) =>
        PStripWorkspaceRead(lStripTab)?.PWorkspaceSurface.PTabAction?.PActionRelayApply(
            LStrip.LStripTargetRead(lStripTab.LStripTabId));

    private void PStripTabHandle(LStripTab lStripTab) =>
        PStripWorkspaceRead(lStripTab)?.PWorkspaceRoot.SetValue(
            System.Windows.UIElement.IsEnabledProperty, !lStripTab.LStripTabPending);

    private void PStripCloseHandle(LStripTab lStripTab) => PStripWorkspaceRead(lStripTab)?.PWorkspaceClose();

    private void PStripStaleRemove() =>
        pStripWorkspaces.RemoveAll(pWorkspace => !LStrip.LStripTabs.Contains(pWorkspace.PWorkspaceTab));
}
