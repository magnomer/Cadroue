using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainArea;

internal sealed class PResizableColumnLayout
{
    private const double pSplitterWidth = 6;

    private readonly Grid pGrid;
    private readonly IReadOnlyList<ColumnDefinition> pPanelColumns;
    private readonly double[] pPanelMinimumWidths;
    private readonly double[] pPanelWeights;
    private readonly double[] pPanelStoredWeights;
    private readonly bool[] pPanelHiddenFlags;
    private bool pApplyBusy;

    private PResizableColumnLayout(
        Grid pGrid,
        IReadOnlyList<ColumnDefinition> pPanelColumns,
        IReadOnlyList<double>? pStoredWidths)
    {
        this.pGrid = pGrid;
        this.pPanelColumns = pPanelColumns;
        pPanelMinimumWidths = pPanelColumns.Select(pColumn => pColumn.MinWidth).ToArray();
        pPanelWeights = PWeightCreate(pStoredWidths, pPanelColumns.Count);
        pPanelStoredWeights = pPanelWeights.ToArray();
        pPanelHiddenFlags = new bool[pPanelColumns.Count];

        pGrid.Loaded += (_, _) => PWidthsApply();
        pGrid.SizeChanged += (_, _) => PWidthsApply();
    }

    public static PResizableColumnLayout PAttach(
        Grid pGrid,
        IReadOnlyList<ColumnDefinition> pPanelColumns,
        IReadOnlyList<double>? pStoredWidths)
    {
        return new PResizableColumnLayout(pGrid, pPanelColumns, pStoredWidths);
    }

    public Thumb PSplitterBuild(int pLeftPanelIndex)
    {
        var pThumb = new Thumb
        {
            Cursor = Cursors.SizeWE,
            Background = Brushes.Transparent,
            Focusable = false,
            Template = PThumbTemplateCreate()
        };
        pThumb.DragDelta += (_, pEvent) => PDragApply(pLeftPanelIndex, pEvent.HorizontalChange);
        return pThumb;
    }

    public IReadOnlyList<double> PWidthsRead()
    {
        return pPanelColumns
            .Select(PWidthRead)
            .Where(pWidth => pWidth > 0)
            .ToArray();
    }

    public void PPanelHide(int pPanelIndex)
    {
        if (pPanelIndex < 0 || pPanelIndex >= pPanelColumns.Count || pPanelHiddenFlags[pPanelIndex])
        {
            return;
        }

        pPanelStoredWeights[pPanelIndex] = pPanelWeights[pPanelIndex] > 0 ? pPanelWeights[pPanelIndex] : pPanelStoredWeights[pPanelIndex];
        pPanelHiddenFlags[pPanelIndex] = true;
        pPanelWeights[pPanelIndex] = 0;
        pPanelColumns[pPanelIndex].MinWidth = 0;
        pPanelColumns[pPanelIndex].Width = new GridLength(0);
        PWidthsApply();
    }

    public void PPanelShow(int pPanelIndex)
    {
        if (pPanelIndex < 0 || pPanelIndex >= pPanelColumns.Count || !pPanelHiddenFlags[pPanelIndex])
        {
            return;
        }

        pPanelHiddenFlags[pPanelIndex] = false;
        pPanelWeights[pPanelIndex] = pPanelStoredWeights[pPanelIndex] > 0 ? pPanelStoredWeights[pPanelIndex] : 1;
        pPanelColumns[pPanelIndex].MinWidth = pPanelMinimumWidths[pPanelIndex];
        PWidthsApply();
    }

    private void PDragApply(int pLeftPanelIndex, double pDelta)
    {
        if (pApplyBusy
            || pLeftPanelIndex < 0
            || pLeftPanelIndex >= pPanelColumns.Count - 1
            || Math.Abs(pDelta) <= 0)
        {
            return;
        }

        double pAvailableWidth = PAvailableWidthRead();
        if (pAvailableWidth <= 0)
        {
            return;
        }

        double[] pWidths = PCurrentWidthsRead(pAvailableWidth);
        double[] pMinimumWidths = PMinimumWidthsRead(pAvailableWidth);
        double pLeftWidth = pWidths[pLeftPanelIndex];
        double pRightWidth = pWidths[pLeftPanelIndex + 1];
        double pMinimumDelta = pMinimumWidths[pLeftPanelIndex] - pLeftWidth;
        double pMaximumDelta = pRightWidth - pMinimumWidths[pLeftPanelIndex + 1];
        double pClampedDelta = Math.Clamp(pDelta, pMinimumDelta, pMaximumDelta);
        if (Math.Abs(pClampedDelta) <= 0)
        {
            return;
        }

        pWidths[pLeftPanelIndex] += pClampedDelta;
        pWidths[pLeftPanelIndex + 1] -= pClampedDelta;
        PWidthsCommit(pWidths, pAvailableWidth);
    }

    private void PWidthsApply()
    {
        if (pApplyBusy)
        {
            return;
        }

        double pAvailableWidth = PAvailableWidthRead();
        if (pAvailableWidth <= 0)
        {
            return;
        }

        double[] pMinimumWidths = PMinimumWidthsRead(pAvailableWidth);
        double[] pWidths = new double[pPanelColumns.Count];
        double pWeightTotal = pPanelWeights.Sum(pWeight => Math.Max(0, pWeight));
        if (pWeightTotal <= 0)
        {
            pWeightTotal = pPanelColumns.Count;
            for (int index = 0; index < pPanelWeights.Length; index++)
            {
                pPanelWeights[index] = 1;
            }
        }

        for (int index = 0; index < pWidths.Length; index++)
        {
            pWidths[index] = pPanelHiddenFlags[index]
                ? 0
                : Math.Max(pMinimumWidths[index], pAvailableWidth * pPanelWeights[index] / pWeightTotal);
        }

        PWidthsFitToAvailable(pWidths, pMinimumWidths, pAvailableWidth);
        PWidthsCommit(pWidths, pAvailableWidth);
    }

    private void PWidthsCommit(double[] pWidths, double pAvailableWidth)
    {
        pApplyBusy = true;
        try
        {
            for (int index = 0; index < pWidths.Length; index++)
            {
                pPanelColumns[index].Width = new GridLength(pWidths[index], GridUnitType.Pixel);
                pPanelWeights[index] = pAvailableWidth > 0 ? pWidths[index] / pAvailableWidth : 0;
            }
        }
        finally
        {
            pApplyBusy = false;
        }
    }

    private double[] PCurrentWidthsRead(double pAvailableWidth)
    {
        double[] pWidths = pPanelColumns.Select(PWidthRead).ToArray();
        double pWidthTotal = pWidths.Sum();
        if (pWidthTotal <= 0)
        {
            double pEqualWidth = pAvailableWidth / pWidths.Length;
            for (int index = 0; index < pWidths.Length; index++)
            {
                pWidths[index] = pEqualWidth;
            }
        }

        return pWidths;
    }

    private double[] PMinimumWidthsRead(double pAvailableWidth)
    {
        double[] pMinimumWidths = pPanelMinimumWidths.ToArray();
        for (int index = 0; index < pMinimumWidths.Length; index++)
        {
            if (pPanelHiddenFlags[index])
            {
                pMinimumWidths[index] = 0;
            }
        }

        double pMinimumWidthTotal = pMinimumWidths.Sum();
        if (pMinimumWidthTotal <= pAvailableWidth || pMinimumWidthTotal <= 0)
        {
            return pMinimumWidths;
        }

        double pScale = pAvailableWidth / pMinimumWidthTotal;
        for (int index = 0; index < pMinimumWidths.Length; index++)
        {
            pMinimumWidths[index] *= pScale;
        }

        return pMinimumWidths;
    }

    private double PAvailableWidthRead()
    {
        int pVisiblePanelCount = pPanelHiddenFlags.Count(pHidden => !pHidden);
        return Math.Max(0, pGrid.ActualWidth - pSplitterWidth * Math.Max(0, pVisiblePanelCount - 1));
    }

    private static void PWidthsFitToAvailable(double[] pWidths, double[] pMinimumWidths, double pAvailableWidth)
    {
        double pWidthTotal = pWidths.Sum();
        if (pWidthTotal > pAvailableWidth)
        {
            double pOverflow = pWidthTotal - pAvailableWidth;
            while (pOverflow > 0.5)
            {
                double pReducibleTotal = 0;
                for (int index = 0; index < pWidths.Length; index++)
                {
                    pReducibleTotal += Math.Max(0, pWidths[index] - pMinimumWidths[index]);
                }

                if (pReducibleTotal <= 0)
                {
                    double pScale = pAvailableWidth / pWidthTotal;
                    for (int index = 0; index < pWidths.Length; index++)
                    {
                        pWidths[index] *= pScale;
                    }
                    return;
                }

                double pReduce = Math.Min(pOverflow, pReducibleTotal);
                for (int index = 0; index < pWidths.Length; index++)
                {
                    double pReducible = Math.Max(0, pWidths[index] - pMinimumWidths[index]);
                    if (pReducible <= 0)
                    {
                        continue;
                    }

                    pWidths[index] -= pReduce * pReducible / pReducibleTotal;
                }

                pOverflow = pWidths.Sum() - pAvailableWidth;
            }
        }
        else if (pWidthTotal < pAvailableWidth)
        {
            double pWeightTotal = pWidths.Sum();
            double pSpare = pAvailableWidth - pWidthTotal;
            if (pWeightTotal <= 0)
            {
                double pEqualAdd = pSpare / pWidths.Length;
                for (int index = 0; index < pWidths.Length; index++)
                {
                    pWidths[index] += pEqualAdd;
                }
            }
            else
            {
                for (int index = 0; index < pWidths.Length; index++)
                {
                    pWidths[index] += pSpare * pWidths[index] / pWeightTotal;
                }
            }
        }
    }

    private static double[] PWeightCreate(IReadOnlyList<double>? pStoredWidths, int pCount)
    {
        if (pStoredWidths is null || pStoredWidths.Count != pCount)
        {
            return Enumerable.Repeat(1d, pCount).ToArray();
        }

        double pWidthTotal = pStoredWidths.Sum(pWidth => Math.Max(0, pWidth));
        if (pWidthTotal <= 0)
        {
            return Enumerable.Repeat(1d, pCount).ToArray();
        }

        return pStoredWidths
            .Select(pWidth => Math.Max(0, pWidth) / pWidthTotal)
            .ToArray();
    }

    private static double PWidthRead(ColumnDefinition pColumn)
    {
        if (pColumn.ActualWidth > 0)
        {
            return pColumn.ActualWidth;
        }

        return pColumn.Width.IsAbsolute ? pColumn.Width.Value : 0;
    }

    private static ControlTemplate PThumbTemplateCreate()
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        return new ControlTemplate(typeof(Thumb))
        {
            VisualTree = pBorder
        };
    }
}
