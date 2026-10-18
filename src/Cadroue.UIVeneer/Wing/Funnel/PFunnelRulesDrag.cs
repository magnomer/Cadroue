using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIVeneer;

internal sealed class PFunnelRulesDrag
{
    private static readonly IReadOnlyDictionary<bool, Action<PFunnelRulesDrag, PFunnelRuleRow, MouseEventArgs>>
        pFunnelMoves = new Dictionary<bool, Action<PFunnelRulesDrag, PFunnelRuleRow, MouseEventArgs>>
        {
            [true] = (pDrag, pRow, pEvent) => pDrag.PFunnelMoveRun(pRow, pEvent),
            [false] = (pDrag, pRow, pEvent) => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PFunnelRulesDrag, PFunnelRuleRow>> pFunnelGhosts =
        new Dictionary<bool, Action<PFunnelRulesDrag, PFunnelRuleRow>>
        {
            [true] = (pDrag, pRow) => pDrag.PFunnelGhostShow(pRow),
            [false] = (pDrag, pRow) => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PFunnelRulesDrag, PFunnelRuleRow>> pFunnelReleases =
        new Dictionary<bool, Action<PFunnelRulesDrag, PFunnelRuleRow>>
        {
            [true] = (pDrag, pRow) => pDrag.PFunnelDragClear(pRow),
            [false] = (pDrag, pRow) => { },
        };

    private readonly QFunnel lFunnel;
    private readonly Grid pFunnelRowGrid;
    private readonly IReadOnlyDictionary<QFunnelRule, PFunnelRuleRow> pFunnelRows;

    public PFunnelRulesDrag(
        QFunnel lFunnelOwner,
        Grid pRowGrid,
        IReadOnlyDictionary<QFunnelRule, PFunnelRuleRow> pRows)
    {
        lFunnel = lFunnelOwner;
        pFunnelRowGrid = pRowGrid;
        pFunnelRows = pRows;
    }

    public void PFunnelDragAttach(PFunnelRuleRow pRow)
    {
        pRow.PFunnelHeader.MouseLeftButtonDown += (_, pEvent) => PFunnelPressHandle(pRow, pEvent);
        pRow.PFunnelHeader.MouseMove += (_, pEvent) => PFunnelMoveHandle(pRow, pEvent);
        pRow.PFunnelHeader.MouseLeftButtonUp += (_, pEvent) => PFunnelUpHandle(pRow, pEvent);
        pRow.PFunnelHeader.LostMouseCapture += (_, _) => PFunnelReleaseHandle(pRow);
    }

    private void PFunnelPressHandle(PFunnelRuleRow pRow, MouseButtonEventArgs pEvent)
    {
        Point pGridPoint = pEvent.GetPosition(pFunnelRowGrid);
        Point pOffset = pEvent.GetPosition(pRow);
        lFunnel.QFunnelDrag.QFunnelPressHandle(pRow.PFunnelRule, pGridPoint.X, pGridPoint.Y, pOffset.X, pOffset.Y);
        pRow.PFunnelHeader.CaptureMouse();
    }

    private void PFunnelMoveHandle(PFunnelRuleRow pRow, MouseEventArgs pEvent) =>
        pFunnelMoves[lFunnel.QFunnelDrag.QFunnelMoveCheck(pRow.PFunnelRule, PLook.PLookPressed[pEvent.LeftButton])](
            this, pRow, pEvent);

    private void PFunnelMoveRun(PFunnelRuleRow pRow, MouseEventArgs pEvent)
    {
        Point pCurrent = pEvent.GetPosition(pFunnelRowGrid);
        bool pStart = lFunnel.QFunnelDrag.QFunnelDragResolve(
            pCurrent.X,
            pCurrent.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance);
        pFunnelGhosts[pStart](this, pRow);
        QGhostLayer.QGhostLayerSync(pRow);
        lFunnel.QFunnelDrag.QFunnelDragMove(pCurrent.Y, PFunnelCentersRead());
        pEvent.Handled = true;
    }

    private List<double> PFunnelCentersRead() => lFunnel.QFunnelRules.Select(PFunnelCenterRead).ToList();

    private void PFunnelGhostShow(PFunnelRuleRow pRow)
    {
        pRow.Opacity = 0.72;
        QGhostLayer.QGhostLayerShow(pRow, lFunnel.QFunnelDrag.QFunnelGhost);
    }

    private double PFunnelCenterRead(QFunnelRule lRule)
    {
        PFunnelRuleRow pRow = pFunnelRows[lRule];
        Point pRowPoint = pRow.TransformToAncestor(pFunnelRowGrid).Transform(new Point(0, 0));
        return QFunnelDrag.QFunnelCenterResolve(pRowPoint.Y, pRow.ActualHeight);
    }

    private void PFunnelUpHandle(PFunnelRuleRow pRow, MouseButtonEventArgs pEvent)
    {
        bool pReleased = lFunnel.QFunnelDrag.QFunnelReleaseCheck(pRow.PFunnelRule);
        pFunnelReleases[pReleased](this, pRow);
        pRow.PFunnelHeader.ReleaseMouseCapture();
        pEvent.Handled = pReleased;
    }

    private void PFunnelReleaseHandle(PFunnelRuleRow pRow) =>
        pFunnelReleases[lFunnel.QFunnelDrag.QFunnelReleaseCheck(pRow.PFunnelRule)](this, pRow);

    private void PFunnelDragClear(PFunnelRuleRow pRow)
    {
        pRow.Opacity = 1;
        QGhostLayer.QGhostLayerClear(pRow);
        lFunnel.QFunnelDrag.QFunnelDragClear();
    }
}
