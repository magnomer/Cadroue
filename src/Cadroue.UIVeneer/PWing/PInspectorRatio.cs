using System.Globalization;
using System.Windows;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private static LCropbox PInspectorCropboxResolve(Rect pRect) =>
        new LCropbox(pRect.X, pRect.Y, pRect.Width, pRect.Height);

    private static Rect PInspectorRectResolve(LCropbox pCropbox) =>
        new Rect(pCropbox.LCropboxX, pCropbox.LCropboxY, pCropbox.LCropboxWidth, pCropbox.LCropboxHeight);

    private Rect? PInspectorRatioResolve(Rect pDesired, int pDriveAxis, int pAnchorX, int pAnchorY)
    {
        LCropbox? pFit = LCropbox.LCropboxFitResolve(
            PInspectorCropboxResolve(pDesired),
            new LCropbox(0, 0, LInspector.LInspectorSourceWidth, LInspector.LInspectorSourceHeight),
            LCropboxState.LCropboxStateRatio,
            pDriveAxis,
            pAnchorX,
            pAnchorY);
        return pFit is { } pCropbox ? PInspectorRectResolve(pCropbox) : null;
    }

    private void PCropRatioHandle()
    {
        LCropboxRatio pRatio = LCropboxState.LCropboxStateRatio;
        LCropboxState.LCropboxRatioSet(
            pRatio.LCropboxRatioFixed,
            pRatio.LCropboxRatioLenient,
            PInspectorWholeRead(pInspectorRatioWidth),
            PInspectorWholeRead(pInspectorRatioHeight));
    }

    private static int PInspectorWholeRead(System.Windows.Controls.TextBox pBox) =>
        (int)Math.Round(PInspectorNumberRead(pBox));

    private void PInspectorRatioCommit(bool? pFixed, bool? pLenient)
    {
        LCropboxRatio pRatio = LCropboxState.LCropboxStateRatio;
        bool pRatioFixed = pFixed ?? pRatio.LCropboxRatioFixed;
        LInspector.LInspectorEdgeLock.LCropboxEdgeClear();
        LCropboxState.LCropboxRatioSet(
            pRatioFixed,
            pRatioFixed && (pLenient ?? pRatio.LCropboxRatioLenient),
            pRatio.LCropboxRatioWidth,
            pRatio.LCropboxRatioHeight);
    }

    private void PInspectorRatioHandle()
    {
        int pIndex = pInspectorRatioPreset.SelectedIndex;
        if (pIndex < 0)
        {
            return;
        }

        LCropboxRatio pRatio = LCropboxState.LCropboxStateRatio;
        if (pIndex == 0)
        {
            if (pRatio.LCropboxRatioFixed
                && PInspectorPresetResolve(pRatio.LCropboxRatioWidth, pRatio.LCropboxRatioHeight) != 0)
            {
                PInspectorRatioReset();
            }

            return;
        }

        (int pRatioWidth, int pRatioHeight) = PCropPresetRead(pIndex);
        if (pRatioWidth == pRatio.LCropboxRatioWidth && pRatioHeight == pRatio.LCropboxRatioHeight
            && pRatio.LCropboxRatioFixed)
        {
            return;
        }

        if (!LInspector.LInspectorSourcePresent || pRatioWidth <= 0 || pRatioHeight <= 0)
        {
            return;
        }

        LWorkCrop pCrop = LCropboxState.LCropboxStateCrop;
        double pBoundsWidth = LInspector.LInspectorSourceWidth - pCrop.LWorkCropLeft - pCrop.LWorkCropRight;
        double pBoundsHeight = LInspector.LInspectorSourceHeight - pCrop.LWorkCropTop - pCrop.LWorkCropBottom;
        LCropbox? pPresetCrop = pBoundsWidth > 0 && pBoundsHeight > 0
            ? LCropbox.LCropboxRatioResolve(
                new LCropbox(pCrop.LWorkCropLeft, pCrop.LWorkCropTop, pBoundsWidth, pBoundsHeight),
                pRatioWidth,
                pRatioHeight)
            : null;
        LWorkCrop pFitted = pPresetCrop is { } pCropbox
            ? pCrop with
            {
                LWorkCropLeft = PInspectorEdgeResolve(pCropbox.LCropboxX),
                LWorkCropTop = PInspectorEdgeResolve(pCropbox.LCropboxY),
                LWorkCropRight = PInspectorEdgeResolve(LInspector.LInspectorSourceWidth - pCropbox.LCropboxRight),
                LWorkCropBottom = PInspectorEdgeResolve(LInspector.LInspectorSourceHeight - pCropbox.LCropboxBottom)
            }
            : pCrop;
        LInspector.LInspectorEdgeLock.LCropboxEdgeClear();
        LCropboxState.LCropboxStateSet(
            pFitted,
            LCropboxState.LCropboxStateActive,
            true,
            pRatio.LCropboxRatioLenient,
            pRatioWidth,
            pRatioHeight);
    }

    private static (int, int) PCropPresetRead(int pIndex) => pIndex switch
    {
        1 => (16, 9),
        2 => (9, 16),
        3 => (4, 3),
        4 => (3, 4),
        5 => (1, 1),
        6 => (21, 9),
        _ => (0, 0)
    };

    private void PInspectorRatioUpdate()
    {
        LCropboxRatio pRatio = LCropboxState.LCropboxStateRatio;
        int pPresetIndex = PInspectorPresetResolve(pRatio.LCropboxRatioWidth, pRatio.LCropboxRatioHeight);
        if (!pRatio.LCropboxRatioFixed)
        {
            pPresetIndex = 0;
        }

        if (pInspectorRatioPreset.SelectedIndex != pPresetIndex)
        {
            pInspectorRatioPreset.SelectedIndex = pPresetIndex;
        }

        pInspectorCustomPanel.Visibility = pPresetIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        PInspectorSwitchUpdate(pInspectorRatioFixed, pRatio.LCropboxRatioFixed, false);
        PInspectorSwitchUpdate(pInspectorRatioLenient, pRatio.LCropboxRatioLenient, false);
        pInspectorRatioLenient.IsEnabled = pRatio.LCropboxRatioFixed;
        PInspectorResolutionUpdate();
        if (!LInspector.LInspectorSourcePresent)
        {
            PInspectorWholeSet(pInspectorRatioWidth, pRatio.LCropboxRatioWidth);
            PInspectorWholeSet(pInspectorRatioHeight, pRatio.LCropboxRatioHeight);
            pInspectorRatioNotice.Visibility = Visibility.Collapsed;
            return;
        }

        LWorkCrop pCrop = LCropboxState.LCropboxStateCrop;
        double pCropWidth = LInspector.LInspectorSourceWidth - pCrop.LWorkCropLeft - pCrop.LWorkCropRight;
        double pCropHeight = LInspector.LInspectorSourceHeight - pCrop.LWorkCropTop - pCrop.LWorkCropBottom;

        if (!pRatio.LCropboxRatioFixed && pRatio.LCropboxRatioWidth == 0 && pRatio.LCropboxRatioHeight == 0
            && PInspectorRectRead() is not null)
        {
            (int pShownWidth, int pShownHeight) = LCropbox.LCropboxRatioNormalize(
                (int)Math.Round(pCropWidth),
                (int)Math.Round(pCropHeight));
            PInspectorWholeSet(pInspectorRatioWidth, pShownWidth);
            PInspectorWholeSet(pInspectorRatioHeight, pShownHeight);
        }
        else
        {
            PInspectorWholeSet(pInspectorRatioWidth, pRatio.LCropboxRatioWidth);
            PInspectorWholeSet(pInspectorRatioHeight, pRatio.LCropboxRatioHeight);
        }

        if (pCropWidth <= 0 || pCropHeight <= 0)
        {
            PInspectorNoticeShow(LLocalization.LLocalizationTextRead("Inspector.Crop.FrameError"));
            return;
        }

        if (!pRatio.LCropboxRatioFixed)
        {
            pInspectorRatioNotice.Visibility = Visibility.Collapsed;
            return;
        }

        if (pRatio.LCropboxRatioWidth <= 0 || pRatio.LCropboxRatioHeight <= 0)
        {
            PInspectorNoticeShow(LLocalization.LLocalizationTextRead("Inspector.Crop.RatioError"));
            return;
        }

        if (pRatio.LCropboxRatioLenient
            && LCropbox.LCropboxToleranceCheck(
                pCropWidth, pCropHeight, pRatio.LCropboxRatioWidth, pRatio.LCropboxRatioHeight))
        {
            pInspectorRatioNotice.Visibility = Visibility.Collapsed;
            return;
        }

        (int pExcessPixels, bool pWide) = LCropbox.LCropboxExcessResolve(
            pCropWidth,
            pCropHeight,
            pRatio.LCropboxRatioWidth,
            pRatio.LCropboxRatioHeight);
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
        if (!LInspector.LInspectorSourcePresent)
        {
            pInspectorResolution.Text = "—";
            return;
        }

        LWorkCrop pCropCanonical = PInspectorCropRead();
        double pWidth = LInspector.LInspectorSourceWidth - pCropCanonical.LWorkCropLeft - pCropCanonical.LWorkCropRight;
        double pHeight =
            LInspector.LInspectorSourceHeight - pCropCanonical.LWorkCropTop - pCropCanonical.LWorkCropBottom;
        pInspectorResolution.Text = pWidth > 0 && pHeight > 0
            ? $"{Math.Round(pWidth).ToString(CultureInfo.InvariantCulture)} × "
                + $"{Math.Round(pHeight).ToString(CultureInfo.InvariantCulture)}"
            : "—";
    }

    private void PInspectorNoticeShow(string pNoticeText)
    {
        pInspectorRatioNotice.Text = pNoticeText;
        pInspectorRatioNotice.Visibility = Visibility.Visible;
    }

    public void PInspectorRatioApply(bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) =>
        LInspector.LInspectorRatioApply(pRatioFixed, pRatioLenient, pRatioWidth, pRatioHeight);

    public void PInspectorRatioReset()
    {
        LInspector.LInspectorEdgeLock.LCropboxEdgeClear();
        LCropboxState.LCropboxRatioSet(false, false, 0, 0);
    }

    private static int PInspectorPresetResolve(int pRatioWidth, int pRatioHeight) =>
        pRatioWidth <= 0 || pRatioHeight <= 0
            ? 0
            : (pRatioWidth, pRatioHeight) switch
            {
                (16, 9) => 1,
                (9, 16) => 2,
                (4, 3) => 3,
                (3, 4) => 4,
                (1, 1) => 5,
                (21, 9) => 6,
                _ => 0
            };
}
