using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PRailDrag
{
    private static readonly IReadOnlyDictionary<bool, Func<FrameworkElement, IInputElement?>> pRailCaptures =
        new Dictionary<bool, Func<FrameworkElement, IInputElement?>>
        {
            [true] = pElement => pElement,
            [false] = pElement => null,
        };

    private static readonly IReadOnlyDictionary<bool, Action<PRailDrag, MouseEventArgs>> pRailMoves =
        new Dictionary<bool, Action<PRailDrag, MouseEventArgs>>
        {
            [true] = (pRailDrag, pEvent) => pRailDrag.PTabMoveRun(pEvent),
            [false] = (pRailDrag, pEvent) => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PRailDrag, FrameworkElement?>> pRailGhosts =
        new Dictionary<bool, Action<PRailDrag, FrameworkElement?>>
        {
            [true] = (pRailDrag, pElement) => pRailDrag.PTabGhostShow(pElement!),
            [false] = (pRailDrag, pElement) => { },
        };

    private readonly QRail lRail;
    private readonly FrameworkElement pRailElement;
    private readonly ItemsControl pTabItems;
    private readonly PRailRelay pRailRelay;

    internal PRailDrag(QRail lRailOwner, FrameworkElement pRailOwner, ItemsControl pTabItemsOwner)
    {
        lRail = lRailOwner;
        pRailElement = pRailOwner;
        pTabItems = pTabItemsOwner;
        pRailRelay = new PRailRelay(lRailOwner.QRailRelay, pRailOwner);
    }

    internal void PTabPressHandle(object sender, MouseButtonEventArgs e)
    {
        var pTabElement = (FrameworkElement)sender;
        Point pRailPoint = e.GetPosition(pRailElement);
        Point pOffset = e.GetPosition(pTabElement);
        bool pDrag = lRail.QRailPressHandle(
            QSender.QSenderItemRead<QStripTab>(sender),
            e.ClickCount,
            PTabHitCheck(pTabElement, e),
            pRailPoint.X,
            pRailPoint.Y,
            pOffset.X,
            pOffset.Y);
        Mouse.Capture(pRailCaptures[pDrag](pTabElement));
        e.Handled = true;
    }

    private static bool PTabHitCheck(FrameworkElement pTabElement, MouseButtonEventArgs e)
    {
        var pPresenter = (ContentPresenter)pTabElement.TemplatedParent!;
        var pNameText = (FrameworkElement)pPresenter.ContentTemplate.FindName("pTabNameText", pPresenter);
        Point pPoint = e.GetPosition(pNameText);
        return QRail.QRailNameCheck(
            pNameText.IsVisible, pPoint.X, pPoint.Y, pNameText.ActualWidth, pNameText.ActualHeight);
    }

    internal void PTabMoveHandle(MouseEventArgs e) =>
        pRailMoves[lRail.QRailMoveCheck(PLook.PLookPressed[e.LeftButton])](this, e);

    private void PTabMoveRun(MouseEventArgs e)
    {
        Point pRailPoint = e.GetPosition(pRailElement);
        FrameworkElement? pTabElement = Mouse.Captured as FrameworkElement;
        bool pStart = lRail.QRailDragResolve(
            pRailPoint.X,
            pRailPoint.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance);
        pRailGhosts[pStart](this, pTabElement);
        QGhostLayer.QGhostLayerSync(pTabElement);
        Point pItemsPoint = e.GetPosition(pTabItems);
        lRail.QRailDragMove(
            lRail.QRailPointerResolve(pItemsPoint.X, pItemsPoint.Y),
            lRail.QStrip.QStripTabs.Select(PTabCenterRead).ToList());
        e.Handled = true;
    }

    private void PTabGhostShow(FrameworkElement pTabElement)
    {
        pTabElement.Opacity = 0.72;
        QGhostLayer.QGhostLayerShow(pTabElement, lRail.QRailGhost);
    }

    private double PTabCenterRead(QStripTab lStripTab)
    {
        var pItemElement = (FrameworkElement)pTabItems.ItemContainerGenerator.ContainerFromItem(lStripTab);
        Point pItemPoint = pItemElement.TransformToAncestor(pTabItems).Transform(new Point(0, 0));
        return QRail.QRailCenterResolve(
            lRail.QStrip.QStripSeparator.QStripVertical,
            pItemPoint.X,
            pItemPoint.Y,
            pItemElement.ActualWidth,
            pItemElement.ActualHeight);
    }

    internal void PTabReleaseHandle(MouseButtonEventArgs e)
    {
        QStripTab? lReleased = lRail.QRailReleaseResolve();
        Point pScreenPoint = pRailElement.PointToScreen(e.GetPosition(pRailElement));
        PTabDragClear(Mouse.Captured as FrameworkElement);
        e.Handled = true;
        pRailRelay.PTabRelayRun(lReleased, pScreenPoint);
    }

    internal static void PTabDragClear(FrameworkElement? pTabElement)
    {
        pTabElement?.SetValue(UIElement.OpacityProperty, 1.0);
        QGhostLayer.QGhostLayerClear(pTabElement);
        Mouse.Capture(null);
    }
}
