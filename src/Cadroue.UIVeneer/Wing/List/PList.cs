using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PList : UserControl
{
    public const double PListStripWidth = 48;
    public const string PListDragKind = "CadrouePaths";

    private readonly PListStack pListStack;
    private readonly UIElement pListFullBody;
    private readonly UIElement pListStripBody;

    public QList QList { get; }

    public PList(QList lList)
    {
        QPanel.QPanelAttach(this);
        QList = lList;
        pListStack = new PListStack(this, lList);
        var pChrome = new PListChrome(lList, new PListDialog(lList));
        pListFullBody = PListFullBuild(pChrome);
        pListStripBody = pChrome.PListStripBuild();
        PListHostBuild();
        PListAttach();
        pListStack.PListRowsRebuild();
    }

    private DockPanel PListFullBuild(PListChrome pChrome)
    {
        var pScroll = new ScrollViewer
        {
            Content = pListStack.PListStackBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pHeader = pChrome.PListHeaderBuild();
        DockPanel.SetDock(pHeader, Dock.Top);
        UIElement pActionBar = pChrome.PListActionBuild();
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);
        return pRoot;
    }

    private void PListHostBuild()
    {
        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pListFullBody);
        pBodyHost.Children.Add(pListStripBody);

        FocusVisualStyle = null;
        Focusable = true;
        Content = QPanel.QPanelBuild(pBodyHost);
    }

    private void PListAttach()
    {
        KeyDown += PListKeyHandle;
        QList.QListChanged += pListStack.PListRowsRebuild;
        QList.QListPathChanged += pListStack.PListSelectionUpdate;
        QList.QListMinimizeChanged += PListMinimizeHandle;
    }

    public void PListClose() => QList.QListClose();

    private void PListMinimizeHandle(bool pListMinimized)
    {
        pListFullBody.Visibility = PLook.PLookVisible[!pListMinimized];
        pListStripBody.Visibility = PLook.PLookVisible[pListMinimized];
    }

    public bool PListMinimizedCheck() => QList.QListMinimized;

    public void PListMinimizeSet(bool pListMinimizeRequest) => QList.QListMinimizedSet(pListMinimizeRequest);

    private void PListKeyHandle(object pKeySender, KeyEventArgs pKeyEvent) =>
        pKeyEvent.Handled = QList.QListKeyRun(
            pKeyEvent.Key.ToString(), Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
}
