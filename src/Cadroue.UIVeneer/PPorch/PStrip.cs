using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPorch;

public sealed class PStrip
{
    private readonly List<PWorkspace> pStripWorkspaces = new();

    public PStrip(LStrip lStrip)
    {
        LStrip = lStrip;
        LStrip.LStripChanged += PStripStaleRemove;
        LStrip.LStripTabChanged += PStripTabHandle;
        LStrip.QStripClosure.LStripTabClosed += PStripCloseHandle;
        LStrip.LStripRelayAttach();
    }

    public LStrip LStrip { get; }

    public PWorkspace? PStripSelected => PStripWorkspaceRead(LStrip.LStripSelected);

    public PWorkspace? PStripWorkspaceRead(LStripTab? lStripTab) =>
        pStripWorkspaces.FirstOrDefault(pWorkspace => ReferenceEquals(pWorkspace.PWorkspaceTab, lStripTab));

    public LStripTab PStripAdd(
        string pTabLayoutKey,
        LPreset? lExportSpecificState = null,
        LSceneTabEntry? lPreferenceTabLayout = null)
    {
        LStripTab lStripTab = LStrip.LStripTabCreate(pTabLayoutKey);
        var pWorkspace = new PWorkspace(LStrip, lStripTab, lExportSpecificState, lPreferenceTabLayout);
        pStripWorkspaces.Add(pWorkspace);
        LStrip.LStripAdd(lStripTab);
        return lStripTab;
    }

    private void PStripTabHandle(LStripTab lStripTab) =>
        PStripWorkspaceRead(lStripTab)?.PWorkspaceRoot.SetValue(
            System.Windows.UIElement.IsEnabledProperty, !lStripTab.LStripTabPending);

    private void PStripCloseHandle(LStripTab lStripTab) => PStripWorkspaceRead(lStripTab)?.PWorkspaceClose();

    private void PStripStaleRemove() =>
        pStripWorkspaces.RemoveAll(pWorkspace => !LStrip.LStripTabs.Contains(pWorkspace.PWorkspaceTab));
}
