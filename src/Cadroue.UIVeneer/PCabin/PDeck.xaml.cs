using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PPorch;

namespace Cadroue.UIVeneer.PCabin;

public partial class PDeck : UserControl
{
    private PStrip pStrip = null!;
    private LDeck lDeck = null!;

    public PDeck()
    {
        InitializeComponent();
        pDeckNotice.Text = LLocalization.LLocalizationTextRead("Deck.Empty.Notice");
        Unloaded += PDeckUnloadHandle;
    }

    public void PDeckAttach(PStrip pStripOwner)
    {
        pStrip = pStripOwner;
        lDeck = new LDeck(pStripOwner.LStrip);
        pStripOwner.LStrip.LStripTabAdd += PDeckAddHandle;
        pStripOwner.LStrip.LStripTabClose += PDeckCloseHandle;
        lDeck.LDeckHide += PDeckHideHandle;
        lDeck.LDeckShow += PDeckShowHandle;
        lDeck.LDeckEmptyChange += PDeckNoticeApply;
        PDeckNoticeApply();
    }

    private void PDeckUnloadHandle(object sender, RoutedEventArgs e)
    {
        pStrip.LStrip.LStripTabAdd -= PDeckAddHandle;
        pStrip.LStrip.LStripTabClose -= PDeckCloseHandle;
        lDeck.LDeckHide -= PDeckHideHandle;
        lDeck.LDeckShow -= PDeckShowHandle;
        lDeck.LDeckEmptyChange -= PDeckNoticeApply;
        lDeck.LDeckClose();
    }

    private void PDeckAddHandle(LStripTab lStripTab)
    {
        FrameworkElement pRoot = pStrip.PStripWorkspaceRead(lStripTab)!.PWorkspaceRoot;
        pRoot.Visibility = Visibility.Collapsed;
        pDeckGrid.Children.Add(pRoot);
    }

    private void PDeckCloseHandle(LStripTab lStripTab) =>
        pDeckGrid.Children.Remove(pStrip.PStripWorkspaceRead(lStripTab)!.PWorkspaceRoot);

    private void PDeckHideHandle(LStripTab lStripTab) =>
        pStrip.PStripWorkspaceRead(lStripTab)?.PWorkspaceRoot.SetValue(VisibilityProperty, Visibility.Collapsed);

    private void PDeckShowHandle(LStripTab lStripTab) =>
        pStrip.PStripWorkspaceRead(lStripTab)?.PWorkspaceRoot.SetValue(VisibilityProperty, Visibility.Visible);

    private void PDeckNoticeApply() => pDeckNotice.Visibility = PLook.PLookVisible[lDeck.LDeckEmpty];
}
