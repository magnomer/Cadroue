using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

public sealed class QExportDrag
{
    private readonly QExport qExport;
    private readonly Func<string, int, bool> qExportDragMover;
    private StackPanel? qExportDragPanel;
    private Action<string>? qExportDragSelection;
    private Point? qExportDragOrigin;
    private Border? qExportDragRow;
    private double qExportDragOpacity;
    private string? qExportDragging;
    private bool qExportDragActive;
    private QGhost qExportGhost = new(0, 0);

    public QExportDrag(QExport lOwner, Func<string, int, bool> lMover)
    {
        qExport = lOwner;
        qExportDragMover = lMover;
    }

    public bool QExportDragActive => qExportDragActive;

    public void QExportDragAttach(StackPanel lPanel, Action<string> lSelection)
    {
        QExportDragDetach();
        qExportDragPanel = lPanel;
        qExportDragSelection = lSelection;
        lPanel.PreviewMouseMove += QExportDragMove;
        lPanel.MouseLeftButtonUp += QExportDragHandle;
        lPanel.LostMouseCapture += QExportDragClear;
    }

    public void QExportDragDetach()
    {
        if (qExportDragPanel is null)
        {
            return;
        }

        qExportDragPanel.PreviewMouseMove -= QExportDragMove;
        qExportDragPanel.MouseLeftButtonUp -= QExportDragHandle;
        qExportDragPanel.LostMouseCapture -= QExportDragClear;
        QExportDragRestore();
        QGhostLayer.QGhostLayerClear(qExportDragRow);
        _ = QExportDragReset();
        qExportDragPanel = null;
        qExportDragSelection = null;
    }

    public void QExportDragStart(string lPresetName, double lGrabX, double lGrabY)
    {
        qExportDragging = lPresetName;
        qExportDragActive = false;
        qExportGhost = new QGhost(lGrabX, lGrabY);
    }

    public void QExportDragSet(Border lRow)
    {
        qExportDragRow = lRow;
        qExportDragOpacity = lRow.Opacity;
    }

    public void QExportDragSet(Point? lOrigin) => qExportDragOrigin = lOrigin;

    public bool QExportDragMove()
    {
        if (qExportDragging is null || qExport.QExportEditing is not null)
        {
            return false;
        }

        qExportDragActive = true;
        return true;
    }

    public bool QExportDragMove(Point lCurrent, bool lLeftPressed) =>
        QExportDragMove(lCurrent, lLeftPressed, true);

    public bool QExportDragMove(Point lCurrent, bool lLeftPressed, bool lGhostShown)
    {
        if (qExportDragging is null
            || qExportDragRow is null
            || qExportDragPanel is null
            || qExport.QExportEditing is not null
            || qExportDragOrigin is null
            || !lLeftPressed
            || (!qExportDragActive && QExportDragCheck(lCurrent, qExportDragOrigin.Value)))
        {
            return false;
        }

        bool lStarted = !qExportDragActive;
        _ = QExportDragMove();
        if (lStarted)
        {
            QExportDragApply(qExportDragRow);
            if (lGhostShown)
            {
                QGhostLayer.QGhostLayerShow(qExportDragRow, qExportGhost);
            }
        }

        if (lGhostShown)
        {
            QGhostLayer.QGhostLayerSync(qExportDragRow);
        }

        QExportDragApply(qExportDragging, QExportDragResolve(lCurrent), qExportDragRow);
        return true;
    }

    public bool QExportDragClear()
    {
        QExportDragRestore();
        QGhostLayer.QGhostLayerClear(qExportDragRow);
        bool lMoved = QExportDragReset();
        if (lMoved)
        {
            qExport.QExportSync();
        }

        return lMoved;
    }

    private void QExportDragRestore()
    {
        if (qExportDragRow is not null)
        {
            qExportDragRow.Opacity = qExportDragOpacity;
        }
    }

    private bool QExportDragReset()
    {
        bool lMoved = qExportDragActive;
        qExportDragging = null;
        qExportDragActive = false;
        qExportDragRow = null;
        qExportDragOrigin = null;
        return lMoved;
    }

    private bool QExportDragHandle()
    {
        if (qExportDragRow is null || qExportDragging is null || qExportDragPanel is null)
        {
            return false;
        }

        bool lClicked = !qExportDragActive && !qExport.QExportEditingCheck(qExportDragging);
        string lPresetName = qExportDragging;
        QExportDragClear();
        qExportDragPanel.ReleaseMouseCapture();
        if (lClicked)
        {
            qExport.QExportName.QExportNameCommit();
            qExportDragSelection?.Invoke(lPresetName);
        }

        return true;
    }

    private void QExportDragMove(object sender, MouseEventArgs pEvent) =>
        pEvent.Handled = QExportDragMove(
            pEvent.GetPosition((StackPanel)sender),
            pEvent.LeftButton == MouseButtonState.Pressed) || pEvent.Handled;

    private void QExportDragHandle(object sender, MouseButtonEventArgs pEvent) =>
        pEvent.Handled = QExportDragHandle() || pEvent.Handled;

    private void QExportDragClear(object sender, MouseEventArgs pEvent) => QExportDragClear();

    private static bool QExportDragCheck(Point lCurrent, Point lStart) =>
        Math.Abs(lCurrent.X - lStart.X) < SystemParameters.MinimumHorizontalDragDistance
        && Math.Abs(lCurrent.Y - lStart.Y) < SystemParameters.MinimumVerticalDragDistance;

    private static void QExportDragApply(Border lRow) => lRow.Opacity = 0.42;

    private int QExportDragResolve(Point lPoint)
    {
        int lTargetIndex = 0;
        for (int lIndex = 0; lIndex < qExportDragPanel!.Children.Count; lIndex++)
        {
            if (qExportDragPanel.Children[lIndex] is not FrameworkElement lRow)
            {
                continue;
            }

            Point lRowPoint = lRow.TransformToAncestor(qExportDragPanel).Transform(new Point(0, 0));
            if (lPoint.Y > lRowPoint.Y + lRow.ActualHeight / 2)
            {
                lTargetIndex = lIndex + 1;
            }
        }

        return Math.Clamp(lTargetIndex, 0, qExportDragPanel.Children.Count);
    }

    private void QExportDragApply(string lPresetName, int lTargetIndex, UIElement lRow)
    {
        int lSourceIndex = qExportDragPanel!.Children.IndexOf(lRow);
        if (lSourceIndex < 0)
        {
            return;
        }

        (int lPresetInsertIndex, int lPresetTargetIndex) = QExportDragResolve(lSourceIndex, lTargetIndex);
        bool lPositionChanged = lSourceIndex != lPresetInsertIndex;
        bool lMoved = lPositionChanged && qExportDragMover(lPresetName, lPresetTargetIndex);
        if (lMoved)
        {
            qExportDragPanel.Children.RemoveAt(lSourceIndex);
            qExportDragPanel.Children.Insert(lPresetInsertIndex, lRow);
        }
    }

    private (int lPresetInsertIndex, int lPresetTargetIndex) QExportDragResolve(int lSourceIndex, int lTargetIndex)
    {
        lTargetIndex = Math.Clamp(lTargetIndex, 0, qExportDragPanel!.Children.Count);
        int lInsertIndex = lSourceIndex < lTargetIndex ? lTargetIndex - 1 : lTargetIndex;
        return (lInsertIndex, lTargetIndex - QExportDragRead(lTargetIndex));
    }

    private int QExportDragRead(int lChildIndex)
    {
        int lHeaderCount = 0;
        int lLimit = Math.Min(lChildIndex, qExportDragPanel!.Children.Count);
        for (int lIndex = 0; lIndex < lLimit; lIndex++)
        {
            if (qExportDragPanel.Children[lIndex] is Border { Tag: "Header" })
            {
                lHeaderCount++;
            }
        }

        return lHeaderCount;
    }
}
