using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIVeneer.PCabin;

internal sealed partial class PColumn
{
    public Thumb PColumnSplitterBuild(int pLeftPanelIndex)
    {
        var pThumb = new Thumb
        {
            Cursor = Cursors.SizeWE,
            Background = Brushes.Transparent,
            Focusable = false,
            Template = PColumnThumbCreate()
        };
        pThumb.DragDelta += (_, pEvent) => PColumnDragApply(pLeftPanelIndex, pEvent.HorizontalChange);
        return pThumb;
    }

    private void PColumnDragApply(int pLeftPanelIndex, double pDelta)
    {
        if (pLeftPanelIndex < 0
            || pLeftPanelIndex >= pColumnItems.Count - 1
            || Math.Abs(pDelta) <= 0)
        {
            return;
        }

        double pAvailableWidth = PColumnAvailableRead();
        if (pAvailableWidth <= 0)
        {
            return;
        }

        double[] pWidths = PColumnCurrentRead(pAvailableWidth);
        double[] pMinimumWidths = LColumn.LColumnMinimumRead(pAvailableWidth);
        if (LColumn.LColumnDragResolve(pLeftPanelIndex, pDelta, pWidths, pMinimumWidths))
        {
            PColumnWeightsCommit(pWidths);
        }
    }

    private static ControlTemplate PColumnThumbCreate()
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        return new ControlTemplate(typeof(Thumb))
        {
            VisualTree = pBorder
        };
    }
}
