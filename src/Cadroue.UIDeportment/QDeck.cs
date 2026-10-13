using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QDeck
{
    public delegate FrameworkElement QDeckBuilder(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout);

    private sealed record QDeckEntry(
        FrameworkElement QDeckEntrySurface,
        Action QDeckEntryCloser,
        Action QDeckEntryFlow,
        Action QDeckEntryViewer);

    private readonly QStrip qStrip;
    private readonly Func<string, string> qDeckText;
    private readonly ConditionalWeakTable<QStripTab, QDeckEntry> qDeckSurfaces = new();
    private readonly Dictionary<string, QDeckBuilder> qDeckBuilders = [];
    private QStripTab? qDeckShown;
    private UserControl qDeckSurface = null!;

    internal QDeck(QStrip lStripOwner, Func<string, string> lLocalization)
    {
        qStrip = lStripOwner;
        qDeckText = lLocalization;
        qStrip.QStripSelectChanged += QDeckSelectHandle;
    }

    public event Action<QStripTab>? QDeckHidden;
    public event Action<QStripTab>? QDeckRevealed;
    public event Action? QDeckEmptyChanged;

    public bool QDeckEmpty => qStrip.QStripSelected is null;

    public QStripTab? QDeckShown => qDeckShown;

    private Grid QDeckGrid => QContract.QContractFind<Grid>(qDeckSurface, "pDeckGrid");

    private TextBlock QDeckNotice => QContract.QContractFind<TextBlock>(qDeckSurface, "pDeckNotice");

    public void QDeckAttach(UserControl lSurface)
    {
        qDeckSurface = lSurface;
        QDeckNotice.Text = qDeckText("Deck.Empty.Notice");
        qDeckSurface.Unloaded += QDeckUnloadHandle;
        qStrip.QStripTabChanged += QDeckTabHandle;
        qStrip.QStripTabAdded += QDeckAddHandle;
        qStrip.QStripClosure.QStripTabClosed += QDeckCloseHandle;
        QDeckHidden += QDeckHideHandle;
        QDeckRevealed += QDeckShowHandle;
        QDeckEmptyChanged += QDeckNoticeApply;
        QDeckNoticeApply();
    }

    public void QDeckBuilderAdd(string lKey, QDeckBuilder lBuilder) =>
        qDeckBuilders.Add(lKey, lBuilder);

    public FrameworkElement QDeckSurfaceBuild(QStripTab lStripTab, LSceneTabEntry? lPreferenceTabLayout) =>
        qDeckBuilders[lStripTab.QStripTabKey](lStripTab.QStripTabWorkspace, lPreferenceTabLayout);

    public void QDeckSurfaceAdd(
        QStripTab lStripTab, FrameworkElement lSurface, Action lSurfaceClose, Action lFlowClose, Action lViewerClose) =>
        qDeckSurfaces.Add(lStripTab, new QDeckEntry(lSurface, lSurfaceClose, lFlowClose, lViewerClose));

    public TSurface? QDeckSurfaceRead<TSurface>(QStripTab? lStripTab)
        where TSurface : FrameworkElement =>
        QDeckEntryRead(lStripTab)?.QDeckEntrySurface as TSurface;

    public void QDeckSurfaceClose(QStripTab lStripTab)
    {
        if (QDeckEntryRead(lStripTab) is not { } lEntry)
        {
            return;
        }

        lStripTab.QStripTabWorkspace.QWorkspaceFrame?.QWorkspaceFrameClose(
            lEntry.QDeckEntryCloser, lEntry.QDeckEntryFlow, lEntry.QDeckEntryViewer);
    }

    public void QDeckAllClose() => qStrip.QStripTabs.ToList().ForEach(QDeckSurfaceClose);

    public void QDeckClose() => qStrip.QStripSelectChanged -= QDeckSelectHandle;

    private void QDeckSelectHandle(QStripTab? lTab)
    {
        if (qDeckShown is { } lPrevious && !ReferenceEquals(lPrevious, lTab))
        {
            QDeckHidden?.Invoke(lPrevious);
        }

        qDeckShown = lTab;
        if (lTab is not null)
        {
            QDeckRevealed?.Invoke(lTab);
        }

        QDeckEmptyChanged?.Invoke();
    }

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
        lWorkspace.Visibility = Visibility.Collapsed;
        QDeckGrid.Children.Add(lWorkspace);
    }

    private void QDeckCloseHandle(QStripTab lStripTab)
    {
        QDeckSurfaceClose(lStripTab);
        QDeckGrid.Children.Remove(lStripTab.QStripTabWorkspace.QWorkspaceView!);
    }

    private void QDeckTabHandle(QStripTab lStripTab) =>
        lStripTab.QStripTabWorkspace.QWorkspaceView?.SetValue(
            UIElement.IsEnabledProperty, !lStripTab.QStripTabPending);

    private QDeckEntry? QDeckEntryRead(QStripTab? lStripTab) =>
        lStripTab is not null && qStrip.QStripTabs.Contains(lStripTab)
            && qDeckSurfaces.TryGetValue(lStripTab, out var lEntry)
                ? lEntry
                : null;

    private void QDeckHideHandle(QStripTab lStripTab) =>
        lStripTab.QStripTabWorkspace.QWorkspaceView?.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);

    private void QDeckShowHandle(QStripTab lStripTab) =>
        lStripTab.QStripTabWorkspace.QWorkspaceView?.SetValue(UIElement.VisibilityProperty, Visibility.Visible);

    private void QDeckNoticeApply() =>
        QDeckNotice.Visibility = QDeckEmpty ? Visibility.Visible : Visibility.Collapsed;
}
