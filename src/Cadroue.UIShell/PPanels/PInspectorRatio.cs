using System.Globalization;
using System.Windows;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private static Rect? PInspectorRatioFit(Rect pBounds, int pRatioWidth, int pRatioHeight)
    {
        if (pBounds.Width <= 0 || pBounds.Height <= 0 || pRatioWidth <= 0 || pRatioHeight <= 0)
        {
            return null;
        }

        int pDivisor = PInspectorDivisorRead(pRatioWidth, pRatioHeight);
        int pUnitWidth = pRatioWidth / pDivisor;
        int pUnitHeight = pRatioHeight / pDivisor;
        int pScale = (int)Math.Floor(Math.Min(pBounds.Width / pUnitWidth, pBounds.Height / pUnitHeight));

        while (pScale > 0 && ((pScale * pUnitWidth % 2) != 0 || (pScale * pUnitHeight % 2) != 0))
        {
            pScale--;
        }

        if (pScale <= 0)
        {
            return null;
        }

        double pWidth = pScale * pUnitWidth;
        double pHeight = pScale * pUnitHeight;
        double pMinimumX = Math.Ceiling(pBounds.Left / 2) * 2;
        double pMinimumY = Math.Ceiling(pBounds.Top / 2) * 2;
        double pMaximumX = PInspectorEvenNormalize(pBounds.Right - pWidth);
        double pMaximumY = PInspectorEvenNormalize(pBounds.Bottom - pHeight);
        if (pMaximumX < pMinimumX || pMaximumY < pMinimumY)
        {
            return null;
        }

        double pX = Math.Clamp(
            PInspectorEvenNormalize(pBounds.X + ((pBounds.Width - pWidth) / 2)),
            pMinimumX,
            pMaximumX);
        double pY = Math.Clamp(
            PInspectorEvenNormalize(pBounds.Y + ((pBounds.Height - pHeight) / 2)),
            pMinimumY,
            pMaximumY);
        return new Rect(pX, pY, pWidth, pHeight);
    }

    private static double PInspectorEvenNormalize(double pValue)
    {
        int pWhole = (int)Math.Floor(pValue);
        return pWhole <= 0 ? 0 : pWhole - (pWhole % 2);
    }

    private Rect? PInspectorRatioAnchorFit(Rect pDesired, int pDriveAxis, int pAnchorX, int pAnchorY)
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

        return PInspectorRatioAnchorResolve(
            pDesired,
            new Rect(0, 0, pInspectorSourceWidth, pInspectorSourceHeight),
            pRatioWidth,
            pRatioHeight,
            pDriveAxis,
            pAnchorX,
            pAnchorY);
    }

    private static Rect? PInspectorRatioAnchorResolve(
        Rect pDesired,
        Rect pBounds,
        int pRatioWidth,
        int pRatioHeight,
        int pDriveAxis,
        int pAnchorX,
        int pAnchorY)
    {
        int pDivisor = PInspectorDivisorRead(pRatioWidth, pRatioHeight);
        int pUnitWidth = pRatioWidth / pDivisor;
        int pUnitHeight = pRatioHeight / pDivisor;

        double pScaleRaw = pDriveAxis switch
        {
            0 => pDesired.Width / pUnitWidth,
            1 => pDesired.Height / pUnitHeight,
            _ => Math.Min(pDesired.Width / pUnitWidth, pDesired.Height / pUnitHeight)
        };

        int pScale = (int)Math.Round(pScaleRaw);
        while (pScale > 0)
        {
            double pWidth = pScale * pUnitWidth;
            double pHeight = pScale * pUnitHeight;
            if ((pWidth % 2) != 0 || (pHeight % 2) != 0)
            {
                pScale--;
                continue;
            }

            double pMaximumX = PInspectorEvenNormalize(pBounds.Width - pWidth);
            double pMaximumY = PInspectorEvenNormalize(pBounds.Height - pHeight);
            if (pMaximumX < 0 || pMaximumY < 0)
            {
                pScale--;
                continue;
            }

            double pX = Math.Clamp(
                PInspectorEvenNormalize(PInspectorAnchorPlace(pDesired.X, pDesired.Width, pWidth, pAnchorX)),
                0,
                pMaximumX);
            double pY = Math.Clamp(
                PInspectorEvenNormalize(PInspectorAnchorPlace(pDesired.Y, pDesired.Height, pHeight, pAnchorY)),
                0,
                pMaximumY);
            return new Rect(pX, pY, pWidth, pHeight);
        }

        return null;
    }

    private static double PInspectorAnchorPlace(double pOrigin, double pDesiredSize, double pSize, int pAnchor) => pAnchor switch
    {
        < 0 => pOrigin,
        > 0 => pOrigin + pDesiredSize - pSize,
        _ => pOrigin + ((pDesiredSize - pSize) / 2)
    };

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

    private void PInspectorRatioCommit()
    {
        PInspectorRatioUpdate();
        PInspectorRatioRaise();
    }

    private void PInspectorRatioPresetHandle()
    {
        if (pInspectorRatioSuppress || pInspectorRatioPreset.SelectedIndex < 0)
        {
            return;
        }

        bool pCustom = pInspectorRatioPreset.SelectedIndex == 0;
        pInspectorRatioCustomPanel.Visibility = pCustom ? Visibility.Visible : Visibility.Collapsed;
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

        var pBounds = new Rect(pLeft, pTop, pBoundsWidth, pBoundsHeight);
        Rect? pPresetCrop = PInspectorRatioFit(pBounds, pRatioWidth, pRatioHeight);
        if (pPresetCrop is not { } pCrop)
        {
            PInspectorRatioUpdate();
            return;
        }

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

        double pWide = pCropWidth * pRatioHeight;
        double pTall = pCropHeight * pRatioWidth;
        double pExcess = pWide > pTall
            ? pCropWidth - (pCropHeight * pRatioWidth / pRatioHeight)
            : pCropHeight - (pCropWidth * pRatioHeight / pRatioWidth);

        int pExcessPixels = PInspectorEvenRead(pExcess);
        if (pExcessPixels <= 0)
        {
            pInspectorRatioNotice.Visibility = Visibility.Collapsed;
            return;
        }

        PInspectorNoticeShow(pWide > pTall
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
        int pWidthWhole = (int)Math.Round(pCropWidth);
        int pHeightWhole = (int)Math.Round(pCropHeight);
        int pDivisor = PInspectorDivisorRead(pWidthWhole, pHeightWhole);
        pInspectorRatioWidth.Text = (pWidthWhole / pDivisor).ToString(CultureInfo.InvariantCulture);
        pInspectorRatioHeight.Text = (pHeightWhole / pDivisor).ToString(CultureInfo.InvariantCulture);
    }

    private void PInspectorNoticeShow(string pNoticeText)
    {
        pInspectorRatioNotice.Text = pNoticeText;
        pInspectorRatioNotice.Visibility = Visibility.Visible;
    }

    private static int PInspectorDivisorRead(int pFirst, int pSecond)
    {
        while (pSecond != 0)
        {
            (pFirst, pSecond) = (pSecond, pFirst % pSecond);
        }

        return pFirst == 0 ? 1 : pFirst;
    }

    private static int PInspectorEvenRead(double pExcess)
    {
        if (pExcess < 1)
        {
            return 0;
        }

        int pWhole = (int)Math.Ceiling(pExcess - 0.001);
        return pWhole + (pWhole % 2);
    }
}
