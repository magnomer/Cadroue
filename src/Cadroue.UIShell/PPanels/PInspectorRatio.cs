using System.Globalization;
using System.Windows;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private Rect? PInspectorRatioClamp(Rect pCropRect)
    {
        if (pInspectorRatioFixed.IsChecked != true)
        {
            return null;
        }

        int pRatioWidth = (int)Math.Round(PInspectorNumberRead(pInspectorRatioWidth));
        int pRatioHeight = (int)Math.Round(PInspectorNumberRead(pInspectorRatioHeight));
        if (pRatioWidth <= 0 || pRatioHeight <= 0)
        {
            return null;
        }

        int pDivisor = PInspectorDivisorRead(pRatioWidth, pRatioHeight);
        int pUnitWidth = pRatioWidth / pDivisor;
        int pUnitHeight = pRatioHeight / pDivisor;

        int pScale = (int)Math.Floor(Math.Min(
            pCropRect.Width / pUnitWidth,
            pCropRect.Height / pUnitHeight));

        while (pScale > 0 && ((pScale * pUnitWidth % 2) != 0 || (pScale * pUnitHeight % 2) != 0))
        {
            pScale--;
        }

        if (pScale <= 0)
        {
            return null;
        }

        double pSnapWidth = pScale * pUnitWidth;
        double pSnapHeight = pScale * pUnitHeight;
        double pSnapX = PInspectorEvenNormalize(pCropRect.X + ((pCropRect.Width - pSnapWidth) / 2));
        double pSnapY = PInspectorEvenNormalize(pCropRect.Y + ((pCropRect.Height - pSnapHeight) / 2));
        pSnapX = Math.Clamp(pSnapX, 0, Math.Max(0, PInspectorEvenNormalize(pInspectorSourceWidth - pSnapWidth)));
        pSnapY = Math.Clamp(pSnapY, 0, Math.Max(0, PInspectorEvenNormalize(pInspectorSourceHeight - pSnapHeight)));
        return new Rect(pSnapX, pSnapY, pSnapWidth, pSnapHeight);
    }

    private static double PInspectorEvenNormalize(double pValue)
    {
        int pWhole = (int)Math.Floor(pValue);
        return pWhole <= 0 ? 0 : pWhole - (pWhole % 2);
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

    private void PInspectorRatioCommit()
    {
        PInspectorRatioUpdate();
        PInspectorRatioRaise();
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
