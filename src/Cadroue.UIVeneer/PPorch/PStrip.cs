using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPorch;

public sealed class PStrip
{
    private readonly List<PWorkspace> pStripWorkspaces = new();

    public PStrip(QStrip lStrip)
    {
        QStrip = lStrip;
        QStrip.QStripChanged += PStripStaleRemove;
        QStrip.QStripTabChanged += PStripTabHandle;
        QStrip.QStripClosure.QStripTabClosed += PStripCloseHandle;
    }

    public QStrip QStrip { get; }

    public PWorkspace? PStripSelected => PStripWorkspaceRead(QStrip.QStripSelected);

    public PWorkspace? PStripWorkspaceRead(QStripTab? lStripTab) =>
        pStripWorkspaces.FirstOrDefault(pWorkspace => ReferenceEquals(pWorkspace.PWorkspaceTab, lStripTab));

    public QStripTab PStripAdd(QStripTab lStripTab, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        var pWorkspace = new PWorkspace(QStrip, lStripTab, lPreferenceTabLayout);
        pStripWorkspaces.Add(pWorkspace);
        QStrip.QStripAdd(lStripTab);
        return lStripTab;
    }

    private void PStripTabHandle(QStripTab lStripTab) =>
        PStripWorkspaceRead(lStripTab)?.PWorkspaceRoot.SetValue(
            System.Windows.UIElement.IsEnabledProperty, !lStripTab.QStripTabPending);

    private void PStripCloseHandle(QStripTab lStripTab) => PStripWorkspaceRead(lStripTab)?.PWorkspaceClose();

    private void PStripStaleRemove() =>
        pStripWorkspaces.RemoveAll(pWorkspace => !QStrip.QStripTabs.Contains(pWorkspace.PWorkspaceTab));
}
