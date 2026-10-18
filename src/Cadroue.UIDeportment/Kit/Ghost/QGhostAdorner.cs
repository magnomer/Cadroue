using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

internal sealed class QGhostAdorner : Adorner
{
    private readonly VisualCollection qGhostAdornerVisuals;
    private readonly FrameworkElement qGhostAdornerImage;
    private readonly QGhost qGhostAdornerOwner;

    internal QGhostAdorner(UIElement lRoot, FrameworkElement lSource, FrameworkElement lImage, QGhost lOwner)
        : base(lRoot)
    {
        qGhostAdornerOwner = lOwner;
        qGhostAdornerImage = lImage;
        QGhostAdornerSource = lSource;
        qGhostAdornerVisuals = new VisualCollection(this) { lImage };
        IsHitTestVisible = false;
    }

    internal FrameworkElement QGhostAdornerSource { get; }

    internal void QGhostAdornerSync()
    {
        _ = QGhostPoint.QGhostPointRead(out QGhostPoint lCursor);
        Point lRootPoint = AdornedElement.PointFromScreen(new Point(lCursor.QGhostPointX, lCursor.QGhostPointY));
        qGhostAdornerOwner.QGhostPointSet(lRootPoint.X, lRootPoint.Y);
        InvalidateArrange();
        (Parent as AdornerLayer)?.Update(AdornedElement);
    }

    internal void QGhostAdornerRemove()
    {
        _ = qGhostAdornerOwner.QGhostClear();
        (Parent as AdornerLayer)?.Remove(this);
    }

    protected override int VisualChildrenCount => qGhostAdornerVisuals.Count;

    protected override Visual GetVisualChild(int index) => qGhostAdornerVisuals[index];

    protected override Size MeasureOverride(Size constraint)
    {
        qGhostAdornerImage.Measure(constraint);
        return qGhostAdornerImage.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        qGhostAdornerImage.Arrange(new Rect(
            qGhostAdornerOwner.QGhostLeft,
            qGhostAdornerOwner.QGhostTop,
            qGhostAdornerImage.Width,
            qGhostAdornerImage.Height));
        return finalSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct QGhostPoint
    {
        public int QGhostPointX;
        public int QGhostPointY;

        [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QGhostPointRead(out QGhostPoint lPoint);
    }
}
