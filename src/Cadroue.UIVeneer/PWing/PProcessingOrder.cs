using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PProcessing
{
    public void PProcessingOrderedSet(bool pOrderedRequest) => LProcessing.LProcessingOrderedSet(pOrderedRequest);

    private void PProcessingStepMove(int pStepDelta)
    {
        if (LProcessing.LProcessingStepMove(pStepDelta))
        {
            PProcessingOrderChange?.Invoke();
        }
    }

    private void PProcessingOrderUpdate()
    {
        IReadOnlyList<string> pSteps = LProcessing.LProcessingSteps;
        for (int pIndex = 0; pIndex < pSteps.Count; pIndex++)
        {
            if (!pProcessingRows.TryGetValue(pSteps[pIndex], out Border? pRow))
            {
                continue;
            }

            int pCurrent = pProcessingRowPanel.Children.IndexOf(pRow);
            if (pCurrent != pIndex)
            {
                pProcessingRowPanel.Children.RemoveAt(pCurrent);
                pProcessingRowPanel.Children.Insert(pIndex, pRow);
            }
        }

        PProcessingNumbersUpdate();
    }

    private void PProcessingNumbersUpdate()
    {
        if (!LProcessing.LProcessingOrdered)
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
