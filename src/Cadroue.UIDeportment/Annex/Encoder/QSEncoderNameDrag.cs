using System.Windows;

namespace Cadroue.UIDeportment;

public static class QSNameDrag
{
    public static void QSNameDragRun(FrameworkElement lChip, string lToken)
    {
        var lData = new DataObject();
        lData.SetData(QTokenControl.QTokenControlKind, lToken);
        lData.SetData(DataFormats.Text, lToken);
        _ = DragDrop.DoDragDrop(lChip, lData, DragDropEffects.Copy);
    }
}
