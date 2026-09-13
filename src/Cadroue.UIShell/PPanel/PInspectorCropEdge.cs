using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private static int PInspectorWholeRead(TextBox pNumberBox) =>
        (int)Math.Round(PInspectorNumberRead(pNumberBox));

    private void PInspectorInsetChange(int pEdge)
    {
        if (pInspectorCropSuppress)
        {
            return;
        }

        TextBox pEdgeBox = pEdge switch
        {
            0 => pInspectorInsetLeft,
            2 => pInspectorInsetRight,
            1 => pInspectorInsetTop,
            _ => pInspectorInsetBottom
        };
        pInspectorEdgeLocked[pEdge] = !string.IsNullOrWhiteSpace(pEdgeBox.Text);

        if (pInspectorRatioFixed.IsChecked == true && pInspectorSourcePresent && !pInspectorRatioSuppress)
        {
            PInspectorRatioResolve(pEdge);
        }

        PInspectorRatioUpdate();
        PInspectorCropRaise();
    }

    private void PInspectorEdgeClear() => Array.Clear(pInspectorEdgeLocked);

    public void PInspectorCropSet(Rect? pCropVideo, int pDriveAxis, int pAnchorX, int pAnchorY)
    {
        PInspectorEdgeClear();
        Rect? pCropSnapped = pCropVideo is { Width: > 0, Height: > 0 } pCropDrawn
            ? PInspectorRatioResolve(pCropDrawn, pDriveAxis, pAnchorX, pAnchorY) ?? pCropDrawn
            : pCropVideo;

        bool pCropSuppressPrevious = pInspectorCropSuppress;
        pInspectorCropSuppress = true;
        pInspectorCropPresent = pCropSnapped is { Width: > 0, Height: > 0 };
        PInspectorToolUpdate();
        try
        {
            if (pCropSnapped is not { Width: > 0, Height: > 0 } pCropRect)
            {
                pInspectorInsetLeft.Text = "0";
                pInspectorInsetTop.Text = "0";
                pInspectorInsetRight.Text = "0";
                pInspectorInsetBottom.Text = "0";
            }
            else
            {
                pInspectorInsetLeft.Text = PInspectorEdgeFormat(pCropRect.X);
                pInspectorInsetTop.Text = PInspectorEdgeFormat(pCropRect.Y);
                pInspectorInsetRight.Text = PInspectorEdgeFormat(pInspectorSourceWidth - pCropRect.X - pCropRect.Width);
                pInspectorInsetBottom.Text = PInspectorEdgeFormat(
                    pInspectorSourceHeight - pCropRect.Y - pCropRect.Height);
            }

            PInspectorRatioUpdate();
        }
        finally
        {
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorCropRaise();
    }

    public Rect? PInspectorRectRead() => PInspectorRectResolve();

    private Rect? PInspectorRectResolve() =>
        LCropbox.LCropboxRectResolve(PInspectorCanonicalRead(), pInspectorSourceWidth, pInspectorSourceHeight)
            is { } pCropBox
            ? new Rect(pCropBox.LCropboxX, pCropBox.LCropboxY, pCropBox.LCropboxWidth, pCropBox.LCropboxHeight)
            : null;

    private void PInspectorCropClear()
    {
        if (pInspectorCropSuppress || !pInspectorCropPresent)
        {
            return;
        }

        PInspectorCropChange?.Invoke(null);
    }

    private void PInspectorEdgesReset()
    {
        PInspectorEdgeClear();
        bool pCropSuppressPrevious = pInspectorCropSuppress;
        pInspectorCropSuppress = true;
        try
        {
            pInspectorInsetLeft.Text = "0";
            pInspectorInsetTop.Text = "0";
            pInspectorInsetRight.Text = "0";
            pInspectorInsetBottom.Text = "0";
            pInspectorCropPresent = false;
        }
        finally
        {
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorCropChange?.Invoke(null);
        PInspectorRatioUpdate();
        PInspectorToolUpdate();
    }

    private void PInspectorCropRaise()
    {
        if (pInspectorCropSuppress)
        {
            return;
        }

        Rect? pCropRect = PInspectorRectResolve();
        pInspectorCropPresent = pCropRect is not null;
        PInspectorToolUpdate();
        PInspectorCropChange?.Invoke(pCropRect);
    }

    private static string PInspectorEdgeFormat(double pEdgeValue) =>
        Math.Max(0, Math.Round(pEdgeValue)).ToString(CultureInfo.InvariantCulture);

    private static double PInspectorNumberRead(TextBox pNumberBox) =>
        double.TryParse(pNumberBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out double pNumber)
            ? pNumber
            : 0;
}
