using System.Globalization;
using System.Windows;

using Cadroue.Application;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private static LCropbox PInspectorCropboxResolve(Rect pRect) =>
        new LCropbox(pRect.X, pRect.Y, pRect.Width, pRect.Height);

    private static Rect PInspectorRectResolve(LCropbox pCropbox) =>
        new Rect(pCropbox.LCropboxX, pCropbox.LCropboxY, pCropbox.LCropboxWidth, pCropbox.LCropboxHeight);

    private Rect? PInspectorRatioResolve(Rect pDesired, int pDriveAxis, int pAnchorX, int pAnchorY)
    {
        if (pInspectorRatioFixed.IsChecked != true
            || pDesired.Width <= 0 || pDesired.Height <= 0
            || pInspectorSourceWidth <= 0 || pInspectorSourceHeight <= 0)
        {
            return null;
        }

        int pRatioWidth = (int)Math.Round(PInspectorNumberRead(pInspectorRatioWidth));
        int pRatioHeight = (int)Math.Round(PInspectorNumberRead(pInspectorRatioHeight));
        if (pRatioWidth <= 0 || pRatioHeight <= 0)
        {
            return null;
        }

        LCropbox pBounds = new LCropbox(0, 0, pInspectorSourceWidth, pInspectorSourceHeight);
        LCropbox? pFit = pInspectorRatioLenient.IsChecked == true
            ? LCropbox.LCropboxLenientResolve(
                PInspectorCropboxResolve(pDesired),
                pBounds,
                pRatioWidth,
                pRatioHeight,
                pDriveAxis,
                pAnchorX,
                pAnchorY,
                PInspectorRatioTolerance)
            : LCropbox.LCropboxAnchorResolve(
                PInspectorCropboxResolve(pDesired),
                pBounds,
                pRatioWidth,
                pRatioHeight,
                pDriveAxis,
                pAnchorX,
                pAnchorY);
        return pFit is { } pCropbox ? PInspectorRectResolve(pCropbox) : null;
    }

    private void PCropRatioHandle()
    {
        if (pInspectorCropSuppress || pInspectorRatioSuppress)
        {
            return;
        }

        pInspectorRatioSuppress = true;
        try
        {
            PInspectorCropClear();
            PInspectorRatioRaise();
            PInspectorRatioUpdate();
        }
        finally
        {
            pInspectorRatioSuppress = false;
        }
    }

    private void PInspectorRatioResolve(int pEdge)
    {
        (double Left, double Top, double Right, double Bottom)? pFit = LCropbox.LCropboxLockResolve(
            pInspectorSourceWidth,
            pInspectorSourceHeight,
            Math.Max(0, PInspectorNumberRead(pInspectorInsetLeft)),
            Math.Max(0, PInspectorNumberRead(pInspectorInsetTop)),
            Math.Max(0, PInspectorNumberRead(pInspectorInsetRight)),
            Math.Max(0, PInspectorNumberRead(pInspectorInsetBottom)),
            pInspectorEdgeLocked[0],
            pInspectorEdgeLocked[1],
            pInspectorEdgeLocked[2],
            pInspectorEdgeLocked[3],
            PInspectorNumberRead(pInspectorRatioWidth),
            PInspectorNumberRead(pInspectorRatioHeight),
            pEdge is 0 or 2);
        if (pFit is not { } pEdges)
        {
            return;
        }

        bool pCropSuppressPrevious = pInspectorCropSuppress;
        pInspectorCropSuppress = true;
        try
        {
            pInspectorInsetLeft.Text = PInspectorEdgeFormat(pEdges.Left);
            pInspectorInsetTop.Text = PInspectorEdgeFormat(pEdges.Top);
            pInspectorInsetRight.Text = PInspectorEdgeFormat(pEdges.Right);
            pInspectorInsetBottom.Text = PInspectorEdgeFormat(pEdges.Bottom);
        }
        finally
        {
            pInspectorCropSuppress = pCropSuppressPrevious;
        }
    }

    private void PInspectorRatioCommit()
    {
        pInspectorRatioLenient.IsEnabled = pInspectorRatioFixed.IsChecked == true;
        PInspectorEdgeClear();
        PInspectorRatioUpdate();
        PInspectorRatioRaise();
    }

    private void PInspectorRatioHandle()
    {
        if (pInspectorRatioSuppress || pInspectorRatioPreset.SelectedIndex < 0)
        {
            return;
        }

        bool pCustom = pInspectorRatioPreset.SelectedIndex == 0;
        pInspectorCustomPanel.Visibility = pCustom ? Visibility.Visible : Visibility.Collapsed;
        if (pCustom)
        {
            PInspectorRatioReset();
            return;
        }

        (int pRatioWidth, int pRatioHeight) = pInspectorRatioPreset.SelectedIndex switch
        {
            1 => (16, 9),
            2 => (9, 16),
            3 => (4, 3),
            4 => (3, 4),
            5 => (1, 1),
            6 => (21, 9),
            _ => (0, 0)
        };
        if (!pInspectorSourcePresent || pRatioWidth <= 0 || pRatioHeight <= 0)
        {
            return;
        }

        double pLeft = Math.Max(0, PInspectorNumberRead(pInspectorInsetLeft));
        double pTop = Math.Max(0, PInspectorNumberRead(pInspectorInsetTop));
        double pRight = Math.Max(0, PInspectorNumberRead(pInspectorInsetRight));
        double pBottom = Math.Max(0, PInspectorNumberRead(pInspectorInsetBottom));
        double pBoundsWidth = pInspectorSourceWidth - pLeft - pRight;
        double pBoundsHeight = pInspectorSourceHeight - pTop - pBottom;
        if (pBoundsWidth <= 0 || pBoundsHeight <= 0)
        {
            PInspectorRatioUpdate();
            return;
        }

        LCropbox? pPresetCrop = LCropbox.LCropboxRatioResolve(
            new LCropbox(pLeft, pTop, pBoundsWidth, pBoundsHeight),
            pRatioWidth,
            pRatioHeight);
        if (pPresetCrop is not { } pCropbox)
        {
            PInspectorRatioUpdate();
            return;
        }

        Rect pCrop = PInspectorRectResolve(pCropbox);
        bool pCropSuppressPrevious = pInspectorCropSuppress;
        pInspectorCropSuppress = true;
        pInspectorRatioSuppress = true;
        try
        {
            pInspectorRatioWidth.Text = pRatioWidth.ToString(CultureInfo.InvariantCulture);
            pInspectorRatioHeight.Text = pRatioHeight.ToString(CultureInfo.InvariantCulture);
            pInspectorRatioFixed.IsChecked = true;
            pInspectorInsetLeft.Text = PInspectorEdgeFormat(pCrop.X);
            pInspectorInsetTop.Text = PInspectorEdgeFormat(pCrop.Y);
            pInspectorInsetRight.Text = PInspectorEdgeFormat(pInspectorSourceWidth - pCrop.Right);
            pInspectorInsetBottom.Text = PInspectorEdgeFormat(pInspectorSourceHeight - pCrop.Bottom);
        }
        finally
        {
            pInspectorRatioSuppress = false;
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorRatioRaise();
        PInspectorCropRaise();
        PInspectorRatioUpdate();
    }

    private void PInspectorRatioRaise()
    {
        if (pInspectorCropSuppress)
        {
            return;
        }

        if (pInspectorRatioFixed.IsChecked != true)
        {
            PInspectorRatioChange?.Invoke(null);
            return;
        }

        double pRatioWidth = PInspectorNumberRead(pInspectorRatioWidth);
        double pRatioHeight = PInspectorNumberRead(pInspectorRatioHeight);
        PInspectorRatioChange?.Invoke(pRatioWidth > 0 && pRatioHeight > 0
            ? new Size(pRatioWidth, pRatioHeight)
            : null);
    }

    private void PInspectorRatioUpdate()
    {
        PInspectorResolutionUpdate();
        if (!pInspectorSourcePresent)
        {
            pInspectorRatioNotice.Visibility = Visibility.Collapsed;
            return;
        }

        double pCropWidth = pInspectorSourceWidth
            - PInspectorNumberRead(pInspectorInsetLeft)
            - PInspectorNumberRead(pInspectorInsetRight);
        double pCropHeight = pInspectorSourceHeight
            - PInspectorNumberRead(pInspectorInsetTop)
            - PInspectorNumberRead(pInspectorInsetBottom);

        if (pCropWidth <= 0 || pCropHeight <= 0)
        {
            PInspectorNoticeShow(LLocalization.LLocalizationTextRead("Inspector.Crop.FrameError"));
            return;
        }

        if (pInspectorRatioFixed.IsChecked != true)
        {
            if (pInspectorCropPresent && !pInspectorRatioSuppress)
            {
                PInspectorRatioFormat(pCropWidth, pCropHeight);
            }

            pInspectorRatioNotice.Visibility = Visibility.Collapsed;
            return;
        }

        double pRatioWidth = PInspectorNumberRead(pInspectorRatioWidth);
        double pRatioHeight = PInspectorNumberRead(pInspectorRatioHeight);
        if (pRatioWidth <= 0 || pRatioHeight <= 0)
        {
            PInspectorNoticeShow(LLocalization.LLocalizationTextRead("Inspector.Crop.RatioError"));
            return;
        }

        if (pInspectorRatioLenient.IsChecked == true
            && LCropbox.LCropboxErrorResolve(pCropWidth, pCropHeight, pRatioWidth, pRatioHeight) <= PInspectorRatioTolerance)
        {
            pInspectorRatioNotice.Visibility = Visibility.Collapsed;
            return;
        }

        (int pExcessPixels, bool pWide) = LCropbox.LCropboxExcessResolve(
            pCropWidth,
            pCropHeight,
            pRatioWidth,
            pRatioHeight);
        if (pExcessPixels <= 0)
        {
            pInspectorRatioNotice.Visibility = Visibility.Collapsed;
            return;
        }

        PInspectorNoticeShow(pWide
            ? LLocalization.LLocalizationFormat("Inspector.Crop.WidthMismatch", pExcessPixels)
            : LLocalization.LLocalizationFormat("Inspector.Crop.HeightMismatch", pExcessPixels));
    }

    private void PInspectorResolutionUpdate()
    {
        if (!pInspectorSourcePresent)
        {
            pInspectorResolution.Text = "—";
            return;
        }

        double pWidth = pInspectorSourceWidth
            - PInspectorEvenClamp(pInspectorInsetLeft)
            - PInspectorEvenClamp(pInspectorInsetRight);
        double pHeight = pInspectorSourceHeight
            - PInspectorEvenClamp(pInspectorInsetTop)
            - PInspectorEvenClamp(pInspectorInsetBottom);
        pInspectorResolution.Text = pWidth > 0 && pHeight > 0
            ? $"{Math.Round(pWidth).ToString(CultureInfo.InvariantCulture)} × {Math.Round(pHeight).ToString(CultureInfo.InvariantCulture)}"
            : "—";
    }

    private void PInspectorRatioFormat(double pCropWidth, double pCropHeight)
    {
        (int pRatioWidth, int pRatioHeight) = LCropbox.LCropboxRatioNormalize(
            (int)Math.Round(pCropWidth),
            (int)Math.Round(pCropHeight));
        pInspectorRatioWidth.Text = pRatioWidth.ToString(CultureInfo.InvariantCulture);
        pInspectorRatioHeight.Text = pRatioHeight.ToString(CultureInfo.InvariantCulture);
    }

    private void PInspectorNoticeShow(string pNoticeText)
    {
        pInspectorRatioNotice.Text = pNoticeText;
        pInspectorRatioNotice.Visibility = Visibility.Visible;
    }
}
