using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSEncoderNameDrag
{
    internal static void PSNameDragRun(UIElement? pRoot, FrameworkElement pChip, string pToken, Point pGrabOffset)
    {
        AdornerLayer? pLayer = PSNameLayerRead(pRoot);
        PSNameDragAdorner? pDragAdorner = PSNameAdornerAdd(pRoot, pLayer, pChip, pGrabOffset);
        GiveFeedbackEventHandler pFeedback = PSNameFeedbackAttach(pChip, pRoot, pDragAdorner);
        try
        {
            PSNameDragStart(pChip, PSNameDataCreate(pToken));
        }
        finally
        {
            PSNameFeedbackDetach(pChip, pFeedback);
            PSNameAdornerRemove(pLayer, pDragAdorner);
        }
    }

    private static AdornerLayer? PSNameLayerRead(UIElement? pRoot) =>
        pRoot is null ? null : AdornerLayer.GetAdornerLayer(pRoot);

    private static PSNameDragAdorner? PSNameAdornerAdd(
        UIElement? pRoot,
        AdornerLayer? pLayer,
        FrameworkElement pChip,
        Point pGrabOffset)
    {
        if (pRoot is null || pLayer is null)
        {
            return null;
        }

        var pDragAdorner = new PSNameDragAdorner(pRoot, pChip, pGrabOffset);
        pLayer.Add(pDragAdorner);
        return pDragAdorner;
    }

    private static GiveFeedbackEventHandler PSNameFeedbackAttach(
        FrameworkElement pChip,
        UIElement? pRoot,
        PSNameDragAdorner? pDragAdorner)
    {
        GiveFeedbackEventHandler pFeedback = (_, pEvent) => PSNameFeedbackHandle(pRoot, pDragAdorner, pEvent);
        pChip.GiveFeedback += pFeedback;
        return pFeedback;
    }

    private static void PSNameFeedbackHandle(
        UIElement? pRoot,
        PSNameDragAdorner? pDragAdorner,
        GiveFeedbackEventArgs pEvent)
    {
        if (pDragAdorner is null || pRoot is null || !PSNameCursorRead(out PSNamePoint pCursor))
        {
            return;
        }

        pDragAdorner.PSNameDragMove(pRoot.PointFromScreen(new Point(pCursor.PSNameX, pCursor.PSNameY)));
        pEvent.UseDefaultCursors = true;
        pEvent.Handled = true;
    }

    private static DataObject PSNameDataCreate(string pToken)
    {
        var pData = new DataObject();
        pData.SetData(QTokenControl.QTokenControlKind, pToken);
        pData.SetData(DataFormats.Text, pToken);
        return pData;
    }

    private static void PSNameDragStart(FrameworkElement pChip, DataObject pData) =>
        _ = DragDrop.DoDragDrop(pChip, pData, DragDropEffects.Copy);

    private static void PSNameFeedbackDetach(FrameworkElement pChip, GiveFeedbackEventHandler pFeedback) =>
        pChip.GiveFeedback -= pFeedback;

    private static void PSNameAdornerRemove(AdornerLayer? pLayer, PSNameDragAdorner? pDragAdorner)
    {
        if (pDragAdorner is not null)
        {
            pLayer?.Remove(pDragAdorner);
        }
    }

    private sealed class PSNameDragAdorner : Adorner
    {
        private readonly VisualCollection psNameVisuals;
        private readonly System.Windows.Shapes.Rectangle psNameImage;
        private readonly Point psNameGrabOffset;
        private Point psNameDragPoint;

        internal PSNameDragAdorner(UIElement pAdornedElement, FrameworkElement pChip, Point pGrabOffset)
            : base(pAdornedElement)
        {
            psNameGrabOffset = pGrabOffset;
            psNameImage = new System.Windows.Shapes.Rectangle
            {
                Width = pChip.ActualWidth,
                Height = pChip.ActualHeight,
                Fill = new VisualBrush(pChip),
                Opacity = 0.85,
                IsHitTestVisible = false
            };
            psNameVisuals = new VisualCollection(this) { psNameImage };

            IsHitTestVisible = false;
        }

        internal void PSNameDragMove(Point pPoint)
        {
            psNameDragPoint = pPoint;
            InvalidateArrange();
            (Parent as AdornerLayer)?.Update(AdornedElement);
        }

        protected override int VisualChildrenCount => psNameVisuals.Count;

        protected override Visual GetVisualChild(int pIndex) => psNameVisuals[pIndex];

        protected override Size MeasureOverride(Size pConstraint)
        {
            psNameImage.Measure(pConstraint);
            return psNameImage.DesiredSize;
        }

        protected override Size ArrangeOverride(Size pFinalSize)
        {
            psNameImage.Arrange(new Rect(
                psNameDragPoint.X - psNameGrabOffset.X,
                psNameDragPoint.Y - psNameGrabOffset.Y,
                psNameImage.Width,
                psNameImage.Height));
            return pFinalSize;
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct PSNamePoint
    {
        public int PSNameX;
        public int PSNameY;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetCursorPos")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool PSNameCursorRead(out PSNamePoint pointScreen);
}
