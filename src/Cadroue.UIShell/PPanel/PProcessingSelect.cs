using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PProcessing
{
    private void PProcessingStepSelect(string pStepName)
    {
        if (pProcessingDisabledSteps.Contains(pStepName))
        {
            return;
        }

        pProcessingStepCurrent = pStepName;
        PProcessingSelectApply();
        PProcessingStepChange?.Invoke(pStepName);
        PProcessingStepOpen?.Invoke(pStepName);
    }

    private void PProcessingSelectApply()
    {
        foreach (UIElement pRow in pProcessingRowPanel.Children)
        {
            if (pRow is Border { Tag: string pRowName } pRowBorder)
            {
                pRowBorder.Background = pRowName == pProcessingStepCurrent
                    && !pProcessingDisabledSteps.Contains(pRowName)
                    ? pProcessingSelectBrush
                    : Brushes.White;
            }
        }

        pProcessingSkipRow.Background = pProcessingStepCurrent == PProcessingSkipStep
            ? pProcessingSelectBrush
            : Brushes.White;
    }
}
