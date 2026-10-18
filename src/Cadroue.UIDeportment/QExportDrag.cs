namespace Cadroue.UIDeportment;

// The preset row drag: which preset is held, whether it has moved, and its ghost. A held preset never moves while a
// name is being edited, and a drag that moved resyncs the rows when it ends.
public sealed class QExportDrag
{
    private readonly QExport qExport;
    private string? qExportDragging;
    private bool qExportDragActive;
    private QGhost qExportGhost = new(0, 0);

    public QExportDrag(QExport lOwner)
    {
        qExport = lOwner;
    }

    public string? QExportDragging => qExportDragging;

    public bool QExportDragActive => qExportDragActive;

    public QGhost QExportGhost => qExportGhost;

    public void QExportDragStart(string lPresetName, double lGrabX, double lGrabY)
    {
        qExportDragging = lPresetName;
        qExportDragActive = false;
        qExportGhost = new QGhost(lGrabX, lGrabY);
    }

    public bool QExportDragMove()
    {
        if (qExportDragging is null || qExport.QExportEditing is not null)
        {
            return false;
        }

        qExportDragActive = true;
        return true;
    }

    public bool QExportDragClear()
    {
        bool lMoved = QExportDragReset();
        if (lMoved)
        {
            qExport.QExportSync();
        }

        return lMoved;
    }

    // Ends the drag; answers whether it had moved.
    private bool QExportDragReset()
    {
        bool lMoved = qExportDragActive;
        qExportDragging = null;
        qExportDragActive = false;
        return lMoved;
    }
}
