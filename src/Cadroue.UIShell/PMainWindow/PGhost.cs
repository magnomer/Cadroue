using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cadroue.UIShell.PMainWindow;

public sealed class PGhost
{
    private const double PGhostOpacity = 0.72;

    private readonly AdornerLayer pGhostLayer;
    private readonly PGhostAdorner pGhostAdorner;
    private readonly UIElement pGhostRoot;
    private bool pGhostGone;

    private PGhost(AdornerLayer pLayer, PGhostAdorner pAdorner, UIElement pRoot)
    {
        pGhostLayer = pLayer;
        pGhostAdorner = pAdorner;
        pGhostRoot = pRoot;
    }

    public static PGhost? PGhostShow(FrameworkElement pSourceElement, Point pGrabOffset)
    {
        if (pSourceElement.ActualWidth <= 0 || pSourceElement.ActualHeight <= 0)
        {
            return null;
        }

        UIElement? pRoot = PGhostRootRead(pSourceElement);
        if (pRoot is null)
        {
            return null;
        }

        AdornerLayer? pLayer = AdornerLayer.GetAdornerLayer(pRoot);
        if (pLayer is null)
        {
            return null;
        }

        ImageSource? pImage = PGhostImageCreate(pSourceElement);
        if (pImage is null)
        {
            return null;
        }

        var pAdorner = new PGhostAdorner(
            pRoot,
            pImage,
            pSourceElement.ActualWidth,
            pSourceElement.ActualHeight,
            pGrabOffset);
        pLayer.Add(pAdorner);

        var pGhost = new PGhost(pLayer, pAdorner, pRoot);
        pGhost.PGhostCursorSync();
        return pGhost;
    }

    public void PGhostPointSet(Point pRootPoint)
    {
        if (pGhostGone)
        {
            return;
        }

        pGhostAdorner.PGhostAdornerSet(pRootPoint);
    }

    public void PGhostCursorSync()
    {
        if (pGhostGone || !PGhostCursorRead(out PGhostPoint pCursor))
        {
            return;
        }

        PGhostPointSet(pGhostRoot.PointFromScreen(new Point(pCursor.PGhostX, pCursor.PGhostY)));
    }

    public void PGhostClear()
    {
        if (pGhostGone)
        {
            return;
        }

        pGhostGone = true;
        pGhostLayer.Remove(pGhostAdorner);
    }

    public static DragDropEffects PGhostDragRun(
        FrameworkElement pSourceElement,
        Point pGrabOffset,
        Func<DragDropEffects> pDragBody)
    {
        PGhost? pGhost = PGhostShow(pSourceElement, pGrabOffset);

        void PGhostFeedbackHandle(object pFeedbackSender, GiveFeedbackEventArgs pFeedbackEvent)
        {
            pGhost?.PGhostCursorSync();
            pFeedbackEvent.UseDefaultCursors = true;
            pFeedbackEvent.Handled = true;
        }

        if (pGhost is not null)
        {
            pSourceElement.GiveFeedback += PGhostFeedbackHandle;
        }

        try
        {
            return pDragBody();
        }
        finally
        {
            if (pGhost is not null)
            {
                pSourceElement.GiveFeedback -= PGhostFeedbackHandle;
                pGhost.PGhostClear();
            }
        }
    }

    private static UIElement? PGhostRootRead(FrameworkElement pSourceElement)
    {
        if (AdornerLayer.GetAdornerLayer(pSourceElement) is not null)
        {
            DependencyObject? pWalk = pSourceElement;
            while (pWalk is not null)
            {
                if (pWalk is AdornerDecorator { Child: UIElement pDecorated })
                {
                    return pDecorated;
                }

                pWalk = VisualTreeHelper.GetParent(pWalk);
            }
        }

        return Window.GetWindow(pSourceElement)?.Content as UIElement;
    }

    private static ImageSource? PGhostImageCreate(FrameworkElement pSourceElement)
    {
        DpiScale pDpi = VisualTreeHelper.GetDpi(pSourceElement);
        int pPixelWidth = (int)Math.Ceiling(pSourceElement.ActualWidth * pDpi.DpiScaleX);
        int pPixelHeight = (int)Math.Ceiling(pSourceElement.ActualHeight * pDpi.DpiScaleY);
        if (pPixelWidth <= 0 || pPixelHeight <= 0)
        {
            return null;
        }

        var pBitmap = new RenderTargetBitmap(
            pPixelWidth,
            pPixelHeight,
            96 * pDpi.DpiScaleX,
            96 * pDpi.DpiScaleY,
            PixelFormats.Pbgra32);
        pBitmap.Render(pSourceElement);
        pBitmap.Freeze();
        return pBitmap;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PGhostPoint
    {
        public int PGhostX;
        public int PGhostY;
    }

    [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PGhostCursorRead(out PGhostPoint pointScreen);

    private sealed class PGhostAdorner : Adorner
    {
        private readonly VisualCollection pGhostVisuals;
        private readonly System.Windows.Shapes.Rectangle pGhostImage;
        private readonly Point pGhostGrabOffset;
        private Point pGhostPoint;

        internal PGhostAdorner(
            UIElement pAdornedElement,
            ImageSource pImage,
            double pWidth,
            double pHeight,
            Point pGrabOffset)
            : base(pAdornedElement)
        {
            pGhostGrabOffset = pGrabOffset;
            pGhostImage = new System.Windows.Shapes.Rectangle
            {
                Width = pWidth,
                Height = pHeight,
                Fill = new ImageBrush(pImage) { Stretch = Stretch.Fill },
                Opacity = PGhostOpacity,
                IsHitTestVisible = false
            };
            pGhostVisuals = new VisualCollection(this) { pGhostImage };
            IsHitTestVisible = false;
        }

        internal void PGhostAdornerSet(Point pPoint)
        {
            pGhostPoint = pPoint;
            InvalidateArrange();
            (Parent as AdornerLayer)?.Update(AdornedElement);
        }

        protected override int VisualChildrenCount => pGhostVisuals.Count;

        protected override Visual GetVisualChild(int pIndex) => pGhostVisuals[pIndex];

        protected override Size MeasureOverride(Size pConstraint)
        {
            pGhostImage.Measure(pConstraint);
            return pGhostImage.DesiredSize;
        }

        protected override Size ArrangeOverride(Size pFinalSize)
        {
            pGhostImage.Arrange(new Rect(
                pGhostPoint.X - pGhostGrabOffset.X,
                pGhostPoint.Y - pGhostGrabOffset.Y,
                pGhostImage.Width,
                pGhostImage.Height));
            return pFinalSize;
        }
    }
}
