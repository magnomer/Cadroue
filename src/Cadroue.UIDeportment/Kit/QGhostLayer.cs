using System.Windows;
using System.Windows.Documents;

namespace Cadroue.UIDeportment;

public static class QGhostLayer
{
    public static void QGhostLayerShow(FrameworkElement lSource, QGhost lGhost) =>
        _ = QGhostLayerAdd(lSource, lGhost);

    public static void QGhostLayerSync(FrameworkElement? lSource) =>
        QGhostLayerFind(lSource)?.QGhostAdornerSync();

    public static void QGhostLayerClear(FrameworkElement? lSource) =>
        QGhostLayerFind(lSource)?.QGhostAdornerRemove();

    public static DragDropEffects QGhostLayerRun(
        FrameworkElement lSource,
        QGhost lGhost,
        Func<DragDropEffects> lDragBody)
    {
        QGhostAdorner lAdorner = QGhostLayerAdd(lSource, lGhost);
        GiveFeedbackEventHandler lFeedback = (_, lEvent) =>
        {
            lAdorner.QGhostAdornerSync();
            lEvent.UseDefaultCursors = true;
            lEvent.Handled = true;
        };
        lSource.GiveFeedback += lFeedback;
        try
        {
            return lDragBody();
        }
        finally
        {
            lSource.GiveFeedback -= lFeedback;
            lAdorner.QGhostAdornerRemove();
        }
    }

    private static QGhostAdorner QGhostLayerAdd(FrameworkElement lSource, QGhost lGhost)
    {
        UIElement lRoot = ((AdornerDecorator)QWalk.QWalkParentFind(lSource, lNode => lNode is AdornerDecorator)!).Child;
        AdornerLayer lLayer = AdornerLayer.GetAdornerLayer(lRoot)!;
        var lAdorner = new QGhostAdorner(lRoot, lSource, QGhostImage.QGhostImageBuild(lSource), lGhost);
        lLayer.Add(lAdorner);
        lAdorner.QGhostAdornerSync();
        return lAdorner;
    }

    private static QGhostAdorner? QGhostLayerFind(FrameworkElement? lSource)
    {
        var lDecorator = QWalk.QWalkParentFind(lSource, lNode => lNode is AdornerDecorator) as AdornerDecorator;
        return lDecorator?.AdornerLayer?.GetAdorners(lDecorator!.Child)?
            .OfType<QGhostAdorner>()
            .FirstOrDefault(lAdorner => ReferenceEquals(lAdorner.QGhostAdornerSource, lSource));
    }
}
