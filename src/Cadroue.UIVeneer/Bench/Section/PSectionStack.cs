using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PSectionStack
{
    private const double PSectionDragOpacity = 0.72;

    private readonly StackPanel pSectionRowPanel = new();
    private readonly QSection lSection;

    public PSectionStack(QSection lSectionOwner)
    {
        lSection = lSectionOwner;
        PSectionPanelAttach();
    }

    public UIElement PSectionStackPanel => pSectionRowPanel;

    private void PSectionPanelAttach()
    {
        pSectionRowPanel.PreviewMouseMove += PSectionMoveHandle;
        pSectionRowPanel.MouseLeftButtonUp += PSectionUpHandle;
        pSectionRowPanel.LostMouseCapture += PSectionLostHandle;
    }

    internal int PSectionIndexRead(PSectionRow pRow) => pSectionRowPanel.Children.IndexOf(pRow.PSectionRowBorder);

    internal void PSectionCaptureRelease() => pSectionRowPanel.ReleaseMouseCapture();

    internal void PSectionCaptureClaim() => pSectionRowPanel.CaptureMouse();

    internal Point PSectionPointRead(MouseEventArgs pEvent) => pEvent.GetPosition(pSectionRowPanel);

    public void PSectionPanelRebuild()
    {
        pSectionRowPanel.Children.Clear();
        lSection.QSectionRowsRead()
            .Select(PSectionRowBuild)
            .ToList()
            .ForEach(pRow => pSectionRowPanel.Children.Add(pRow));
    }

    private Border PSectionRowBuild(QSectionRow lRow) => new PSectionRow(this, lSection, lRow).PSectionRowBorder;

    private List<PSectionRow> PSectionRowsRead() =>
        pSectionRowPanel.Children.OfType<Border>().Select(PSectionRowRead).ToList();

    private static PSectionRow PSectionRowRead(Border pBorder) => (PSectionRow)pBorder.Tag;

    private PSectionRow PSectionRowRead(int lIndex) => PSectionRowRead((Border)pSectionRowPanel.Children[lIndex]);

    public void PSectionSelectApply() => PSectionRowsRead().ForEach(PSectionSelectedApply);

    private void PSectionSelectedApply(PSectionRow pRow) =>
        pRow.PSectionSelectedSet(lSection.QSectionSelectedCheck(PSectionIndexRead(pRow)));

    public void PSectionDragShow(int lIndex)
    {
        Border pRow = PSectionRowRead(lIndex).PSectionRowBorder;
        pRow.Opacity = PSectionDragOpacity;
        QGhostLayer.QGhostLayerShow(pRow, lSection.QSectionDrag.QSectionGhost);
    }

    public void PSectionDragHide(int lIndex)
    {
        Border pRow = PSectionRowRead(lIndex).PSectionRowBorder;
        pRow.Opacity = 1;
        QGhostLayer.QGhostLayerClear(pRow);
    }

    public void PSectionRowMove(int lSource, int lInsert)
    {
        PSectionRowInsert(lSource, lInsert);
        PSectionRowsRead().ForEach(PSectionNumberApply);
    }

    private void PSectionRowInsert(int lSource, int lInsert)
    {
        UIElement pRow = pSectionRowPanel.Children[lSource];
        pSectionRowPanel.Children.RemoveAt(lSource);
        pSectionRowPanel.Children.Insert(lInsert, pRow);
    }

    private void PSectionNumberApply(PSectionRow pRow) =>
        pRow.PSectionNumberSet(QSectionRow.QSectionNumberRead(PSectionIndexRead(pRow)));

    private Border? PSectionDragRead() =>
        pSectionRowPanel.Children.OfType<Border>().ElementAtOrDefault(lSection.QSectionDrag.QSectionDragRead());

    private void PSectionMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        Point pCurrent = pEvent.GetPosition(pSectionRowPanel);
        bool pHandled = lSection.QSectionDrag.QSectionMoveHandle(
            pCurrent.X,
            pCurrent.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance,
            PLook.PLookPressed[pEvent.LeftButton],
            PSectionRowsRead().Select(PSectionTopRead).ToList(),
            PSectionRowsRead().Select(PSectionHeightRead).ToList());
        QGhostLayer.QGhostLayerSync(PSectionDragRead());
        pEvent.Handled = pHandled;
    }

    private double PSectionTopRead(PSectionRow pRow) =>
        pRow.PSectionRowBorder.TransformToAncestor(pSectionRowPanel).Transform(new Point(0, 0)).Y;

    private static double PSectionHeightRead(PSectionRow pRow) => pRow.PSectionRowBorder.ActualHeight;

    private void PSectionUpHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        bool pHandled = lSection.QSectionDrag.QSectionReleaseHandle(
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift), Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
        pSectionRowPanel.ReleaseMouseCapture();
        pEvent.Handled = pHandled;
    }

    private void PSectionLostHandle(object pSender, MouseEventArgs pEvent) =>
        lSection.QSectionDrag.QSectionLostHandle();
}
