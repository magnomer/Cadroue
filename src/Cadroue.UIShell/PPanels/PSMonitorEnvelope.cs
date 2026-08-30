using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSMonitor
{
    private void PSMonitorUpdate()
    {
        PSMonitorEnvelopeDraw(psMonitorBeforeCanvas);
        PSMonitorEnvelopeDraw(psMonitorAfterCanvas);
        PSMonitorHeadPlace();
    }

    private void PSMonitorReadyHandle(LSMonitorEstimate pEstimate)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => PSMonitorReadyHandle(pEstimate));
            return;
        }

        psMonitorEstimate = pEstimate;
        psMonitorTimer.Stop();
        psMonitorTimer.Start();
    }

    private void PSMonitorTickHandle(object? pSender, EventArgs pEvent)
    {
        psMonitorTimer.Stop();
        PSMonitorRailApply(psMonitorBeforeCanvas, psMonitorBeforeStatus, psMonitorEstimate.LSMonitorBefore, false);
        PSMonitorRailApply(psMonitorAfterCanvas, psMonitorAfterStatus, psMonitorEstimate.LSMonitorAfter, psMonitorSource.LSMonitorScanning);
    }

    private static double PSMonitorLevelRead(double pPeak) => Math.Clamp(pPeak, 0, 1);

    private void PSMonitorRailApply(Canvas pCanvas, TextBlock pStatus, double[] pEnvelope, bool pScanning)
    {
        if (pEnvelope.Length == 0)
        {
            pStatus.Text = LLocalization.LLocalizationTextRead(
                psMonitorSource.LSMonitorScanning ? "NormalizePreview.Loading" : "NormalizePreview.Empty");
            pCanvas.DataContext = null;
            PSMonitorEnvelopeDraw(pCanvas);
            return;
        }

        pStatus.Text = pScanning ? LLocalization.LLocalizationTextRead("NormalizePreview.Updating") : string.Empty;
        pCanvas.DataContext = pEnvelope;
        PSMonitorEnvelopeDraw(pCanvas);
    }

    private void PSMonitorEnvelopeDraw(Canvas pCanvas)
    {
        pCanvas.Children.Clear();
        double pWidth = pCanvas.ActualWidth;
        double pHeight = pCanvas.ActualHeight;
        if (pWidth <= 0 || pHeight <= 0)
        {
            return;
        }

        double pMid = pHeight / 2;
        double pPlotWidth = pWidth - PSMonitorGutter;
        PSMonitorAxisDraw(pCanvas, pWidth, pMid);
        if (pCanvas.DataContext is not double[] pEnvelope || pEnvelope.Length == 0 || pPlotWidth <= 1)
        {
            return;
        }

        var pFill = pCanvas.Tag as Brush ?? Brushes.Gray;
        int pColumns = Math.Max(1, (int)pPlotWidth);
        var pGeometry = new StreamGeometry();
        using (StreamGeometryContext pContext = pGeometry.Open())
        {
            pContext.BeginFigure(new Point(PSMonitorGutter, pMid), true, true);
            for (int pColumn = 0; pColumn < pColumns; pColumn++)
            {
                double pLevel = PSMonitorLevelRead(PSMonitorColumnRead(pEnvelope, pColumn, pColumns));
                pContext.LineTo(new Point(PSMonitorGutter + pColumn, pMid - pLevel * pMid), true, false);
            }

            for (int pColumn = pColumns - 1; pColumn >= 0; pColumn--)
            {
                double pLevel = PSMonitorLevelRead(PSMonitorColumnRead(pEnvelope, pColumn, pColumns));
                pContext.LineTo(new Point(PSMonitorGutter + pColumn, pMid + pLevel * pMid), true, false);
            }
        }

        pGeometry.Freeze();
        pCanvas.Children.Add(new System.Windows.Shapes.Path { Data = pGeometry, Fill = pFill });
    }

    private static void PSMonitorAxisDraw(Canvas pCanvas, double pWidth, double pMid)
    {
        foreach (double pFraction in new[] { 1.0, 0.5 })
        {
            double pDb = 20.0 * Math.Log10(pFraction);
            PSMonitorGridDraw(pCanvas, pWidth, pMid - pFraction * pMid, $"{pDb:0} dB");
            PSMonitorGridDraw(pCanvas, pWidth, pMid + pFraction * pMid, null);
        }

        PSMonitorGridDraw(pCanvas, pWidth, pMid, "-∞");
    }

    private static void PSMonitorGridDraw(Canvas pCanvas, double pWidth, double pY, string? pLabel)
    {
        pCanvas.Children.Add(new System.Windows.Shapes.Line
        {
            X1 = PSMonitorGutter,
            X2 = pWidth,
            Y1 = pY,
            Y2 = pY,
            Stroke = psMonitorGridFill,
            StrokeThickness = 1,
            IsHitTestVisible = false
        });

        if (pLabel is null)
        {
            return;
        }

        var pText = new TextBlock
        {
            Text = pLabel,
            FontSize = 10,
            Foreground = psMonitorAxisFill,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(pText, 4);
        Canvas.SetTop(pText, Math.Clamp(pY - 7, 0, Math.Max(0, pCanvas.ActualHeight - 14)));
        pCanvas.Children.Add(pText);
    }

    private double PSMonitorColumnRead(double[] pEnvelope, int pColumn, int pColumns)
    {
        double pViewport = 1.0 / psMonitorScale;
        int pLength = pEnvelope.Length;
        double pFromF = (psMonitorOffset + (double)pColumn / pColumns * pViewport) * pLength;
        double pToF = (psMonitorOffset + (double)(pColumn + 1) / pColumns * pViewport) * pLength;
        int pFrom = Math.Clamp((int)Math.Floor(pFromF), 0, pLength - 1);
        int pTo = Math.Clamp((int)Math.Ceiling(pToF), pFrom + 1, pLength);

        double pPeak = 0;
        for (int pIndex = pFrom; pIndex < pTo; pIndex++)
        {
            if (pEnvelope[pIndex] > pPeak)
            {
                pPeak = pEnvelope[pIndex];
            }
        }

        return pPeak;
    }
}
