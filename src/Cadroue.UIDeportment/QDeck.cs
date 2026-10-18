using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QDeck
{
    private readonly QStrip qStrip;
    private readonly Func<string, string> qDeckText;
    private QStripTab? qDeckShown;
    private UserControl qDeckSurface = null!;

    internal QDeck(QStrip lStripOwner, Func<string, string> lLocalization)
    {
        qStrip = lStripOwner;
        qDeckText = lLocalization;
        QDeckIndex = new QDeckIndex(lStripOwner);
        qStrip.QStripSelectChanged += QDeckSelectHandle;
    }

    public event Action<QStripTab>? QDeckHidden;
    public event Action<QStripTab>? QDeckRevealed;
    public event Action? QDeckEmptyChanged;

    public bool QDeckEmpty => qStrip.QStripSelected is null;

    public QStripTab? QDeckShown => qDeckShown;

    public QDeckCatalog QDeckCatalog { get; } = new();

    public QDeckIndex QDeckIndex { get; }

    private Grid QDeckGrid => QContract.QContractFind<Grid>(qDeckSurface, "pDeckGrid");

    private TextBlock QDeckNotice => QContract.QContractFind<TextBlock>(qDeckSurface, "pDeckNotice");

    public void QDeckAttach(UserControl lSurface)
    {
        QDeckSurfaceSet(lSurface);
        QDeckNoticeSet();
        QDeckHandlersAttach();
        QDeckNoticeApply();
    }

    private void QDeckSurfaceSet(UserControl lSurface) => qDeckSurface = lSurface;

    private void QDeckNoticeSet() => QDeckNotice.Text = qDeckText("Deck.Empty.Notice");

    private void QDeckHandlersAttach()
    {
        qDeckSurface.Unloaded += QDeckUnloadHandle;
        qStrip.QStripTabChanged += QDeckTabHandle;
        qStrip.QStripTabAdded += QDeckAddHandle;
        qStrip.QStripClosure.QStripTabClosed += QDeckCloseHandle;
        QDeckHidden += QDeckHideHandle;
        QDeckRevealed += QDeckShowHandle;
        QDeckEmptyChanged += QDeckNoticeApply;
    }

    public void QDeckSurfaceClose(QStripTab lStripTab)
    {
        if (QDeckIndex.QDeckSurfaceRead<FrameworkElement>(lStripTab) is null)
        {
            return;
        }

        lStripTab.QStripTabWorkspace.QWorkspaceFrame?.QWorkspaceFrameClose();
    }

    public void QDeckAllClose() => qStrip.QStripTabs.ToList().ForEach(QDeckSurfaceClose);

    public void QDeckClose() => qStrip.QStripSelectChanged -= QDeckSelectHandle;

    private void QDeckSelectHandle(QStripTab? lTab)
    {
        if (qDeckShown is { } lPrevious && !ReferenceEquals(lPrevious, lTab))
        {
            QDeckHidden?.Invoke(lPrevious);
        }

        QDeckShownSet(lTab);
        if (lTab is not null)
        {
            QDeckRevealed?.Invoke(lTab);
        }

        QDeckEmptyChanged?.Invoke();
    }

    private void QDeckShownSet(QStripTab? lTab) => qDeckShown = lTab;

    private void QDeckUnloadHandle(object sender, RoutedEventArgs e)
    {
        qStrip.QStripTabChanged -= QDeckTabHandle;
        qStrip.QStripTabAdded -= QDeckAddHandle;
        qStrip.QStripClosure.QStripTabClosed -= QDeckCloseHandle;
        QDeckHidden -= QDeckHideHandle;
        QDeckRevealed -= QDeckShowHandle;
        QDeckEmptyChanged -= QDeckNoticeApply;
        QDeckClose();
    }

    private void QDeckAddHandle(QStripTab lStripTab)
    {
        FrameworkElement lWorkspace = lStripTab.QStripTabWorkspace.QWorkspaceView!;
        QDeckWorkspaceHide(lWorkspace);
        QDeckGrid.Children.Add(lWorkspace);
    }

    private static void QDeckWorkspaceHide(FrameworkElement lWorkspace) =>
        lWorkspace.Visibility = Visibility.Collapsed;

    private void QDeckCloseHandle(QStripTab lStripTab)
    {
        QDeckSurfaceClose(lStripTab);
        QDeckGrid.Children.Remove(lStripTab.QStripTabWorkspace.QWorkspaceView!);
    }

    private void QDeckTabHandle(QStripTab lStripTab) =>
        lStripTab.QStripTabWorkspace.QWorkspaceView?.SetValue(
            UIElement.IsEnabledProperty, !lStripTab.QStripTabPending);

    private void QDeckHideHandle(QStripTab lStripTab) =>
        lStripTab.QStripTabWorkspace.QWorkspaceView?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);

    private void QDeckShowHandle(QStripTab lStripTab) =>
        lStripTab.QStripTabWorkspace.QWorkspaceView?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);

    private void QDeckNoticeApply() => QDeckNotice.Visibility = QDeckNoticeResolve();

    private Visibility QDeckNoticeResolve() => QDeckEmpty ? Visibility.Visible : Visibility.Collapsed;
}
