using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PRailMenu
{
    private readonly PStrip pStrip;
    private readonly QRail lRail;

    internal PRailMenu(PStrip pStripOwner, QRail lRailOwner)
    {
        pStrip = pStripOwner;
        lRail = lRailOwner;
    }

    internal void PTabMenuHandle(object sender, RoutedEventArgs e)
    {
        ContextMenu pTabAddMenu = QMenu.QMenuBuild((Button)sender, QMenuSide.QMenuSideBelow);
        QStripCatalog.QStripKeysRead().ToList().ForEach(pTabLayoutKey => PTabMenuAppend(pTabAddMenu, pTabLayoutKey));
        pTabAddMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PTabMenuAppend(ContextMenu pTabAddMenu, string pTabLayoutKey)
    {
        MenuItem pTabAddMenuItem = QMenu.QMenuItemBuild(
            QStripTitle.QStripTitleResolve(pTabLayoutKey),
            QStripIcon.QStripIconRead(pTabLayoutKey));
        PTabItemAttach(pTabAddMenuItem, pTabLayoutKey);
        pTabAddMenu.Items.Add(pTabAddMenuItem);
    }

    private void PTabItemAttach(MenuItem pTabAddMenuItem, string pTabLayoutKey) =>
        pTabAddMenuItem.Click += (_, _) => PTabLayoutAdd(pTabLayoutKey);

    private void PTabLayoutAdd(string pTabLayoutKey) =>
        lRail.QStrip.QStripSelect(pStrip.PStripAdd(lRail.QStrip.QStripTabCreate(pTabLayoutKey)));
}
