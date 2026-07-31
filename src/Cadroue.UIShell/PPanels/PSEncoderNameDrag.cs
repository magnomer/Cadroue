using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private void PSNameDragRun(FrameworkElement pChip, string pToken, Point pGrabOffset)
    {
        var pRoot = Content as UIElement;
        AdornerLayer? pLayer = pRoot is null ? null : AdornerLayer.GetAdornerLayer(pRoot);
        PSNameDragAdorner? pDragAdorner = null;
        if (pRoot is not null && pLayer is not null)
        {
            pDragAdorner = new PSNameDragAdorner(pRoot, pChip, pGrabOffset);
            pLayer.Add(pDragAdorner);
        }

        void PSNameFeedbackHandle(object pSender, GiveFeedbackEventArgs pEvent)
        {
            if (pDragAdorner is null || pRoot is null || !PSNameCursorRead(out PSNamePoint pCursor))
            {
                return;
            }

            pDragAdorner.PSNameDragMove(pRoot.PointFromScreen(new Point(pCursor.PSNameX, pCursor.PSNameY)));
            pEvent.UseDefaultCursors = true;
            pEvent.Handled = true;
        }

        pChip.GiveFeedback += PSNameFeedbackHandle;
        try
        {
            var pData = new DataObject();
            pData.SetData(PToken.PTokenDataKind, pToken);
            pData.SetData(DataFormats.Text, pToken);
            _ = DragDrop.DoDragDrop(pChip, pData, DragDropEffects.Copy);
        }
        finally
        {
            pChip.GiveFeedback -= PSNameFeedbackHandle;
            if (pDragAdorner is not null)
            {
                pLayer?.Remove(pDragAdorner);
            }
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
