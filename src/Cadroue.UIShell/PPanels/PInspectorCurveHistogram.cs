using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const double PCurveHistogramHeadroom = 0.94;

    private static readonly Brush[] PCurveHistogramBrush =
    {
        new SolidColorBrush(Color.FromArgb(0x30, 0x8A, 0x94, 0x9E)),
        new SolidColorBrush(Color.FromArgb(0x30, 0xD1, 0x3A, 0x3A)),
        new SolidColorBrush(Color.FromArgb(0x30, 0x2F, 0x9E, 0x44)),
        new SolidColorBrush(Color.FromArgb(0x30, 0x2B, 0x6C, 0xB0))
    };

    private LHistogramCounts? pCurveHistogram;

    // Store the current frame's counts (or clear) and repaint. The counts feed only
    // the behind-the-curve guide; they never touch the FFmpeg command.
    public void PCurveHistogramApply(LHistogramCounts? pHistogram)
    {
        pCurveHistogram = pHistogram;
        PCurveRebuild();
    }

    // Faint filled area behind the grid for the active channel: luminance for Master,
    // else that channel. Non-interactive so it never intercepts point editing.
    private void PCurveHistogramDraw()
    {
        if (pCurveHistogram is not { } pHistogram)
        {
            return;
        }

        int pChannel = Math.Clamp(pCurveChannel.SelectedIndex, 0, PCurveHistogramBrush.Length - 1);
        int[] pBins = pChannel switch
        {
            1 => pHistogram.LHistogramRed,
            2 => pHistogram.LHistogramGreen,
            3 => pHistogram.LHistogramBlue,
            _ => pHistogram.LHistogramLuminance
        };

        int pPeak = pBins.Length == 0 ? 0 : pBins.Max();
        if (pPeak <= 0)
        {
            return;
        }

        var pArea = new Polygon
        {
            Fill = PCurveHistogramBrush[pChannel],
            IsHitTestVisible = false,
            Points = new PointCollection { new Point(0, PCurveCanvasSize) }
        };

        for (int pBin = 0; pBin < pBins.Length; pBin++)
        {
            double pX = (double)pBin / (pBins.Length - 1) * PCurveCanvasSize;
            double pY = PCurveCanvasSize
                - ((double)pBins[pBin] / pPeak * PCurveCanvasSize * PCurveHistogramHeadroom);
            pArea.Points.Add(new Point(pX, pY));
        }

        pArea.Points.Add(new Point(PCurveCanvasSize, PCurveCanvasSize));
        pCurveCanvas.Children.Add(pArea);
    }
}
