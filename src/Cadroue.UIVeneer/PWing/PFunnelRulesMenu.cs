using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PFunnelRulesMenu
{
    private readonly QFunnel lFunnel;

    public PFunnelRulesMenu(QFunnel lFunnelOwner) => lFunnel = lFunnelOwner;

    public void PFunnelMenuShow(UIElement pTarget)
    {
        MenuItem pFilenameItem = QMenu.QMenuItemBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Filename"));
        PFunnelItemAttach(pFilenameItem, QFunnelForm.QFunnelFormFilename);

        MenuItem pRegexItem = QMenu.QMenuItemBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Regex"));
        PFunnelItemAttach(pRegexItem, QFunnelForm.QFunnelFormRegex);

        MenuItem pRemainderItem = QMenu.QMenuItemBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Remainder"));
        PFunnelRemainderShow(pRemainderItem);
        PFunnelItemAttach(pRemainderItem, QFunnelForm.QFunnelFormRemainder);

        ContextMenu pAddMenu = PFunnelMenuBuild(pTarget, pFilenameItem, pRegexItem, pRemainderItem);
        PFunnelPopupShow(pAddMenu);
    }

    private void PFunnelItemAttach(MenuItem pItem, QFunnelForm lForm) =>
        pItem.Click += (_, _) => lFunnel.QFunnelRuleAdd(lForm);

    private void PFunnelRemainderShow(MenuItem pRemainderItem) =>
        pRemainderItem.IsEnabled = !lFunnel.QFunnelRemainderCheck();

    private static ContextMenu PFunnelMenuBuild(
        UIElement pTarget,
        MenuItem pFilenameItem,
        MenuItem pRegexItem,
        MenuItem pRemainderItem)
    {
        ContextMenu pAddMenu = QMenu.QMenuBuild(pTarget, QMenuSide.QMenuSideAbove);
        pAddMenu.Items.Add(pFilenameItem);
        pAddMenu.Items.Add(pRegexItem);
        pAddMenu.Items.Add(pRemainderItem);
        return pAddMenu;
    }

    private static void PFunnelPopupShow(ContextMenu pAddMenu) => pAddMenu.IsOpen = true;
}
