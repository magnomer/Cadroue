using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PHouse;

public sealed class PGhost
{
    private const double PGhostOpacity = 0.72;

    private static readonly Type[] PGhostDecoratorTypes = [typeof(AdornerDecorator)];

    private readonly AdornerLayer pGhostLayer;
    private readonly PGhostAdorner pGhostAdorner;
    private readonly UIElement pGhostRoot;
    private readonly LGhost lGhost;

    private PGhost(AdornerLayer pLayer, PGhostAdorner pAdorner, UIElement pRoot, LGhost lOwner)
    {
        pGhostLayer = pLayer;
        pGhostAdorner = pAdorner;
        pGhostRoot = pRoot;
        lGhost = lOwner;
    }

    public static PGhost PGhostShow(FrameworkElement pSourceElement, Point pGrabOffset)
    {
        UIElement pRoot = PGhostRootRead(pSourceElement);
        AdornerLayer pLayer = AdornerLayer.GetAdornerLayer(pRoot)!;
        var lGhost = new LGhost(pGrabOffset.X, pGrabOffset.Y);
        var pAdorner = new PGhostAdorner(
            pRoot,
            PGhostImageCreate(pSourceElement),
            pSourceElement.ActualWidth,
            pSourceElement.ActualHeight,
            lGhost);
        pLayer.Add(pAdorner);

        var pGhost = new PGhost(pLayer, pAdorner, pRoot, lGhost);
        pGhost.PGhostCursorSync();
        return pGhost;
    }

    public void PGhostPointSet(Point pRootPoint)
    {
        lGhost.LGhostPointSet(pRootPoint.X, pRootPoint.Y);
        pGhostAdorner.PGhostAdornerUpdate();
    }

    public void PGhostCursorSync()
    {
        _ = PGhostCursorRead(out PGhostPoint pCursor);
        PGhostPointSet(pGhostRoot.PointFromScreen(new Point(pCursor.PGhostX, pCursor.PGhostY)));
    }

    public void PGhostClear()
    {
        _ = lGhost.LGhostClear();
        pGhostLayer.Remove(pGhostAdorner);
    }

    public static DragDropEffects PGhostDragRun(
        FrameworkElement pSourceElement,
        Point pGrabOffset,
        Func<DragDropEffects> pDragBody)
    {
        PGhost pGhost = PGhostShow(pSourceElement, pGrabOffset);
        GiveFeedbackEventHandler pFeedbackHandle = pGhost.PGhostFeedbackHandle;
        pSourceElement.GiveFeedback += pFeedbackHandle;
        try
        {
            return pDragBody();
        }
        finally
        {
            pSourceElement.GiveFeedback -= pFeedbackHandle;
            pGhost.PGhostClear();
        }
    }

    private void PGhostFeedbackHandle(object pFeedbackSender, GiveFeedbackEventArgs pFeedbackEvent)
    {
        PGhostCursorSync();
        pFeedbackEvent.UseDefaultCursors = true;
        pFeedbackEvent.Handled = true;
    }

    private static UIElement PGhostRootRead(FrameworkElement pSourceElement) =>
        ((AdornerDecorator)PWalk.PWalkParentFind(pSourceElement, PGhostDecoratorMatch)!).Child;

    private static bool PGhostDecoratorMatch(DependencyObject pNode) =>
        PWalk.PWalkTypeCheck(pNode, PGhostDecoratorTypes);

    private static ImageSource PGhostImageCreate(FrameworkElement pSourceElement)
    {
        DpiScale pDpi = VisualTreeHelper.GetDpi(pSourceElement);
        LGhostSize lSize = LGhost.LGhostSizeResolve(
            pSourceElement.ActualWidth,
            pSourceElement.ActualHeight,
            pDpi.DpiScaleX,
            pDpi.DpiScaleY);
        var pBitmap = new RenderTargetBitmap(
            lSize.LGhostPixelWidth,
            lSize.LGhostPixelHeight,
            lSize.LGhostDpiX,
            lSize.LGhostDpiY,
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
        private readonly LGhost lGhost;

        internal PGhostAdorner(
            UIElement pAdornedElement,
            ImageSource pImage,
            double pWidth,
            double pHeight,
            LGhost lOwner)
            : base(pAdornedElement)
        {
            lGhost = lOwner;
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

        internal void PGhostAdornerUpdate()
        {
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
            pGhostImage.Arrange(new Rect(lGhost.LGhostLeft, lGhost.LGhostTop, pGhostImage.Width, pGhostImage.Height));
            return pFinalSize;
        }
    }
}
