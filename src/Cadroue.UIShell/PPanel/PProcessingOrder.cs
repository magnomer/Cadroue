using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PProcessing
{
    private bool pProcessingOrdered;

    public void PProcessingOrderedSet(bool pOrderedRequest)
    {
        pProcessingOrdered = pOrderedRequest;
        pProcessingActionBar.Visibility = pOrderedRequest ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PProcessingStepMove(int pStepDelta)
    {
        if (pProcessingStepCurrent is null || pProcessingDisabledSteps.Contains(pProcessingStepCurrent))
        {
            return;
        }

        int pStepIndex = -1;
        for (int pIndex = 0; pIndex < pProcessingRowPanel.Children.Count; pIndex++)
        {
            if (pProcessingRowPanel.Children[pIndex] is Border { Tag: string pRowName }
                && pRowName == pProcessingStepCurrent)
            {
                pStepIndex = pIndex;
                break;
            }
        }

        if (pStepIndex < 0)
        {
            return;
        }

        int pTargetIndex = pStepIndex + pStepDelta;
        if (pTargetIndex < 0 || pTargetIndex >= pProcessingRowPanel.Children.Count)
        {
            return;
        }

        if (pProcessingRowPanel.Children[pTargetIndex] is Border { Tag: string pTargetName }
            && pProcessingDisabledSteps.Contains(pTargetName))
        {
            return;
        }

        UIElement pStepRow = pProcessingRowPanel.Children[pStepIndex];
        pProcessingRowPanel.Children.RemoveAt(pStepIndex);
        pProcessingRowPanel.Children.Insert(pTargetIndex, pStepRow);
        PProcessingNumbersUpdate();
        PProcessingOrderChange?.Invoke();
    }

    private void PProcessingNumbersUpdate()
    {
        if (!pProcessingOrdered)
        {
            return;
        }

        for (int pIndex = 0; pIndex < pProcessingRowPanel.Children.Count; pIndex++)
        {
            if (pProcessingRowPanel.Children[pIndex] is Border { Child: StackPanel pRowContent }
                && pRowContent.Children.Count > 0
                && pRowContent.Children[0] is Border { Child: TextBlock pNumber })
            {
                pNumber.Text = (pIndex + 1).ToString();
            }
        }
    }
}
