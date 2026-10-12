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
    private QDeck lDeck = null!;

    public PDeck()
    {
        InitializeComponent();
        pDeckNotice.Text = LLocalization.LLocalizationTextRead("Deck.Empty.Notice");
        Unloaded += PDeckUnloadHandle;
    }

    public void PDeckAttach(PStrip pStripOwner)
    {
        pStrip = pStripOwner;
        lDeck = pStripOwner.QStrip.QStripDeck;
        pStripOwner.QStrip.QStripTabAdded += PDeckAddHandle;
        pStripOwner.QStrip.QStripClosure.QStripTabClosed += PDeckCloseHandle;
        lDeck.QDeckHidden += PDeckHideHandle;
        lDeck.QDeckRevealed += PDeckShowHandle;
        lDeck.QDeckEmptyChanged += PDeckNoticeApply;
        PDeckNoticeApply();
    }

    private void PDeckUnloadHandle(object sender, RoutedEventArgs e)
    {
        pStrip.QStrip.QStripTabAdded -= PDeckAddHandle;
        pStrip.QStrip.QStripClosure.QStripTabClosed -= PDeckCloseHandle;
        lDeck.QDeckHidden -= PDeckHideHandle;
        lDeck.QDeckRevealed -= PDeckShowHandle;
        lDeck.QDeckEmptyChanged -= PDeckNoticeApply;
        lDeck.QDeckClose();
    }

    private void PDeckAddHandle(QStripTab lStripTab)
    {
        FrameworkElement pRoot = pStrip.PStripWorkspaceRead(lStripTab)!.PWorkspaceRoot;
        pRoot.Visibility = Visibility.Collapsed;
        pDeckGrid.Children.Add(pRoot);
    }

    private void PDeckCloseHandle(QStripTab lStripTab) =>
        pDeckGrid.Children.Remove(pStrip.PStripWorkspaceRead(lStripTab)!.PWorkspaceRoot);

    private void PDeckHideHandle(QStripTab lStripTab) =>
        pStrip.PStripWorkspaceRead(lStripTab)?.PWorkspaceRoot.SetValue(VisibilityProperty, Visibility.Collapsed);

    private void PDeckShowHandle(QStripTab lStripTab) =>
        pStrip.PStripWorkspaceRead(lStripTab)?.PWorkspaceRoot.SetValue(VisibilityProperty, Visibility.Visible);

    private void PDeckNoticeApply() => pDeckNotice.Visibility = PLook.PLookVisible[lDeck.QDeckEmpty];
}
