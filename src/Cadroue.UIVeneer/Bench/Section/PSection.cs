using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PSection : UserControl
{
    internal const double PSectionNameSize = 12;
    public const double PSectionStripWidth = 48;

    private readonly PSectionChrome pSectionChrome;
    private readonly PSectionStack pSectionStack;
    private readonly UIElement pSectionFullBody;
    private readonly UIElement pSectionStripBody;

    public QSection QSection { get; }

    public PSection(QSection lSection)
    {
        QSection = lSection;
        pSectionChrome = new PSectionChrome(lSection);
        pSectionStack = new PSectionStack(lSection);
        pSectionFullBody = PSectionFullBuild();
        pSectionStripBody = pSectionChrome.PSectionStrip;
        PSectionHostBuild();
        PSectionRebuild();
        PSectionAttach();
    }

    private DockPanel PSectionFullBuild()
    {
        var pScroll = new ScrollViewer
        {
            Content = pSectionStack.PSectionStackPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pHeader = pSectionChrome.PSectionHeader;
        UIElement pActionBar = pSectionChrome.PSectionActionBar;
        DockPanel.SetDock(pHeader, Dock.Top);
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);
        return pRoot;
    }

    private void PSectionHostBuild()
    {
        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pSectionFullBody);
        pBodyHost.Children.Add(pSectionStripBody);

        FocusVisualStyle = null;
        QScrollbar.QScrollbarAttach(this);
        Content = QPanel.QPanelBuild(pBodyHost);
    }

    private void PSectionAttach()
    {
        QSection.QSectionChanged += PSectionRebuild;
        QSection.QSectionSelectApplied += pSectionStack.PSectionSelectApply;
        QSection.QSectionEnabledApplied += PSectionEnabledApply;
        QSection.QSectionMinimizeChanged += PSectionMinimizeHandle;
        QSection.QSectionDrag.QSectionDragStarted += pSectionStack.PSectionDragShow;
        QSection.QSectionDrag.QSectionDragEnd += pSectionStack.PSectionDragHide;
        QSection.QSectionDrag.QSectionRowMoved += pSectionStack.PSectionRowMove;
    }

    public bool PSectionMinimizedCheck() => QSection.QSectionMinimized;

    public void PSectionMinimizeSet(bool pSectionMinimizeRequest) =>
        QSection.QSectionMinimizedSet(pSectionMinimizeRequest);

    private void PSectionRebuild()
    {
        pSectionChrome.PSectionTitleApply();
        pSectionStack.PSectionPanelRebuild();
    }

    private void PSectionEnabledApply(bool lEditable)
    {
        pSectionChrome.PSectionActionApply(lEditable);
        pSectionStack.PSectionCaptureRelease();
    }

    private void PSectionMinimizeHandle(bool pSectionMinimized)
    {
        pSectionFullBody.Visibility = PLook.PLookVisible[!pSectionMinimized];
        pSectionStripBody.Visibility = PLook.PLookVisible[pSectionMinimized];
    }
}
