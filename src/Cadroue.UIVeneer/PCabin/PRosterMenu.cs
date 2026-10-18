using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PCabin;

internal static class PRosterMenu
{
    public static void PRosterMenuApply(
        ContextMenu pMenu,
        IReadOnlyList<QRosterItem> lItems,
        ContextMenuEventArgs pArgs)
    {
        pMenu.Items.Clear();
        lItems.ToList().ForEach(lItem => pMenu.Items.Add(PRosterItemBuild(lItem)));
        PRosterHandledSet(pMenu, pArgs);
    }

    private static void PRosterHandledSet(ContextMenu pMenu, ContextMenuEventArgs pArgs) =>
        pArgs.Handled = pMenu.Items.IsEmpty;

    private static MenuItem PRosterItemBuild(QRosterItem lItem)
    {
        MenuItem pItem = PRosterChoiceBuild(lItem);
        PRosterItemAttach(pItem, lItem);
        return pItem;
    }

    private static MenuItem PRosterChoiceBuild(QRosterItem lItem)
    {
        MenuItem pItem = QMenu.QMenuItemBuild(lItem.QRosterItemText, QStripIcon.QStripIconFind(lItem.QRosterItemIcon));
        pItem.IsEnabled = lItem.QRosterItemEnabled;
        return pItem;
    }

    private static void PRosterItemAttach(MenuItem pItem, QRosterItem lItem) =>
        pItem.Click += (_, _) => lItem.QRosterItemAction();
}
