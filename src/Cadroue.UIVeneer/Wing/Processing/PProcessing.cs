using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PProcessing : UserControl
{
    public const double PProcessingStripWidth = 48;

    public event Action<string?>? PProcessingStepChanged;
    public event Action<string>? PProcessingStepOpened;
    public event Action<bool>? PProcessingMinimizeChanged;
    public event Action? PProcessingMonitorShown;

    private readonly PProcessingChrome pProcessingChrome;
    private readonly PProcessingStack pProcessingStack;
    private readonly UIElement pProcessingFullBody;
    private readonly UIElement pProcessingStripBody;

    public QProcessing QProcessing { get; }

    public PProcessing(QProcessing lProcessing)
    {
        QPanel.QPanelAttach(this);
        QProcessing = lProcessing;
        PProcessingAttach();
        pProcessingChrome = new PProcessingChrome(lProcessing, () => PProcessingMonitorShown?.Invoke());
        pProcessingStack = new PProcessingStack(lProcessing);
        pProcessingFullBody = PProcessingFullBuild();
        pProcessingStripBody = pProcessingChrome.PProcessingStrip;
        PProcessingHostBuild();
        PProcessingUpdate();
    }

    private void PProcessingAttach()
    {
        QProcessing.QProcessingChanged += PProcessingUpdate;
        QProcessing.QProcessingOrderChanged += PProcessingOrderUpdate;
        QProcessing.QProcessingMinimizeChanged += PProcessingMinimizeHandle;
        QProcessing.QProcessingStepChanged += PProcessingStepHandle;
        QProcessing.QProcessingDragCanceled += PProcessingStack.PProcessingCaptureClear;
    }

    private DockPanel PProcessingFullBuild()
    {
        var pScroll = new ScrollViewer
        {
            Content = pProcessingStack.PProcessingRowPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pHeader = pProcessingChrome.PProcessingHeader;
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        UIElement pActionBar = pProcessingChrome.PProcessingActionBar;
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pActionBar);
        Border pSkipBorder = pProcessingStack.PProcessingSkipBorder;
        DockPanel.SetDock(pSkipBorder, Dock.Bottom);
        pRoot.Children.Add(pSkipBorder);
        pRoot.Children.Add(pScroll);
        return pRoot;
    }

    private void PProcessingHostBuild()
    {
        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pProcessingFullBody);
        pBodyHost.Children.Add(pProcessingStripBody);

        FocusVisualStyle = null;
        Content = QPanel.QPanelBuild(pBodyHost);
    }

    public bool PProcessingMinimizedCheck() => QProcessing.QProcessingMinimized;

    public void PProcessingMinimizeSet(bool pProcessingMinimizeRequest) =>
        QProcessing.QProcessingMinimizedSet(pProcessingMinimizeRequest);

    public void PProcessingMonitorSet() => pProcessingChrome.PProcessingMonitorSet();

    public void PProcessingRowAdd(QProcessingRow lRow) => pProcessingStack.PProcessingRowAdd(lRow);

    private void PProcessingStepHandle(string pStepName)
    {
        PProcessingStepChanged?.Invoke(pStepName);
        PProcessingStepOpened?.Invoke(pStepName);
    }

    private void PProcessingMinimizeHandle(bool pProcessingMinimized)
    {
        PProcessingBodyApply(pProcessingMinimized);
        PProcessingMinimizeChanged?.Invoke(pProcessingMinimized);
    }

    private void PProcessingBodyApply(bool pProcessingMinimized)
    {
        pProcessingFullBody.Visibility = PLook.PLookVisible[!pProcessingMinimized];
        pProcessingStripBody.Visibility = PLook.PLookVisible[pProcessingMinimized];
    }

    private void PProcessingOrderUpdate()
    {
        pProcessingStack.PProcessingPanelRebuild();
        PProcessingUpdate();
    }

    private void PProcessingUpdate()
    {
        pProcessingChrome.PProcessingActionShow(QProcessing.QProcessingOrdered);
        pProcessingStack.PProcessingStackUpdate();
        pProcessingChrome.PProcessingActionApply(QProcessing.QProcessingSkipActive, QProcessing.QProcessingSkipOpacity);
    }
}
