using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PProcessing
{
    private void PProcessingUpdate()
    {
        pProcessingActionBar.Visibility = LProcessing.LProcessingOrdered ? Visibility.Visible : Visibility.Collapsed;
        foreach ((string pRowName, Border pRowBorder) in pProcessingRows)
        {
            bool pEnabled = LProcessing.LProcessingEnabledCheck(pRowName);
            pRowBorder.IsEnabled = pEnabled;
            pRowBorder.Opacity = pEnabled ? 1 : 0.4;
            pRowBorder.Cursor = pEnabled ? Cursors.Hand : Cursors.Arrow;
            pRowBorder.Background = LProcessing.LProcessingSelectedCheck(pRowName)
                ? pProcessingSelectBrush
                : Brushes.White;
            if (pRowBorder.Child is StackPanel pRowContent)
            {
                PProcessingRowApply(pRowContent, LProcessing.LProcessingActiveCheck(pRowName));
            }

            if (!pEnabled && ReferenceEquals(pProcessingRowDragging, pRowBorder))
            {
                Mouse.Capture(null);
                PProcessingDragClear();
            }
        }

        bool pSkipActive = LProcessing.LProcessingSkipActive;
        pProcessingSkipRow.Background = LProcessing.LProcessingStep == LProcessing.LProcessingSkipStep
            ? pProcessingSelectBrush
            : Brushes.White;
        if (pProcessingSkipRow.Child is StackPanel pSkipContent)
        {
            PProcessingRowApply(pSkipContent, pSkipActive);
        }

        pProcessingRowPanel.Opacity = pSkipActive ? 0.4 : 1;
        pProcessingActionBar.IsEnabled = !pSkipActive;
        pProcessingActionBar.Opacity = pSkipActive ? 0.4 : 1;
    }
}
