using System.Windows;
using System.Windows.Input;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PViewer
{
    public event Action<LNeutralSample>? PViewerNeutralChange;
    public event Action<bool, LNeutralTarget>? PViewerToolChange;

    public void PViewerNeutralSet(bool pNeutralArmed, LNeutralTarget pNeutralTarget)
    {
        if (pNeutralArmed)
        {
            if (!LViewer.LViewerNeutralSet(pNeutralTarget))
            {
                return;
            }

            if (LViewer.LViewerNeutralPlaying)
            {
                LViewer.LViewerPlayback.LViewerPause();
            }

            pViewerOverlay.Focus();
            LViewer.LViewerNeutralRaise(true);
            return;
        }

        if (LViewer.LViewerNeutralCancel())
        {
            PViewerNeutralReset();
        }
    }

    public void PViewerNeutralCancel() => PViewerNeutralSet(false, LViewer.LViewerNeutralTarget);

    private void PViewerNeutralReset()
    {
        if (pViewerOverlay.IsMouseCaptured)
        {
            pViewerOverlay.ReleaseMouseCapture();
        }

        if (LViewer.LViewerNeutralReset())
        {
            LViewer.LViewerPlayback.LViewerPlay();
        }

        LViewer.LViewerNeutralRaise(false);
    }

    private void PViewerToolHandle(bool pNeutralArmed, LNeutralTarget pNeutralTarget)
    {
        pViewerOverlay.Cursor = pNeutralArmed ? Cursors.Cross : null;
        if (pNeutralArmed)
        {
            pViewerCropBox.Cursor = null;
        }

        PCropOverlayUpdate();
        PViewerToolChange?.Invoke(pNeutralArmed, pNeutralTarget);
    }

    private void PViewerKeyHandle(object sender, KeyEventArgs keyEvent)
    {
        if (LViewer.LViewerTool == LViewerTool.LViewerToolNeutral && keyEvent.Key == Key.Escape)
        {
            PViewerNeutralCancel();
            keyEvent.Handled = true;
        }
    }

    private void PViewerPressHandle(MouseButtonEventArgs mouseEvent)
    {
        mouseEvent.Handled = true;
        if (LViewer.LViewerMediaInfo is not { LMediaVideoPresent: true } pViewerMediaInfo)
        {
            return;
        }

        int pViewerSourceWidth = pViewerMediaInfo.LMediaVideoWidth;
        int pViewerSourceHeight = pViewerMediaInfo.LMediaVideoHeight;
        if (pViewerSourceWidth <= 0 || pViewerSourceHeight <= 0)
        {
            return;
        }

        Point pViewerClick = mouseEvent.GetPosition(pViewerOverlay);
        (Rect pViewerDisplay, Rect pViewerShown) = PViewerGeometryRead();
        LRotateFlip pViewerRotate = LViewer.LViewerPreview.LRotateFlip;
        LNeutralPoint pViewerPoint = LNeutral.LNeutralPointResolve(
            pViewerClick.X, pViewerClick.Y,
            pViewerDisplay.X, pViewerDisplay.Y, pViewerDisplay.Width, pViewerDisplay.Height,
            pViewerShown.X, pViewerShown.Y, pViewerShown.Width, pViewerShown.Height,
            pViewerRotate.LRotateKind,
            pViewerRotate.LRotateFlipHorizontal,
            pViewerRotate.LRotateFlipVertical,
            pViewerSourceWidth, pViewerSourceHeight);

        if (!pViewerPoint.LNeutralPointInside)
        {
            return;
        }

        string? pViewerPath = PViewerSourcePath;
        if (string.IsNullOrWhiteSpace(pViewerPath))
        {
            return;
        }

        TimeSpan pViewerTime = LViewer.LViewerTimeRead();
        int pViewerLoadClaim = LViewer.LViewerLoadSerial;
        int pViewerNeutralClaim = LViewer.LViewerNeutralSerial;
        int pViewerPixelX = pViewerPoint.LNeutralPointX;
        int pViewerPixelY = pViewerPoint.LNeutralPointY;
        LNeutralTarget pViewerTarget = LViewer.LViewerNeutralTarget;

        PViewerNeutralReset();
        PViewerNeutralRead(
            pViewerPath, pViewerTime, pViewerSourceWidth, pViewerSourceHeight,
            pViewerPixelX, pViewerPixelY, pViewerLoadClaim, pViewerNeutralClaim, pViewerTarget);
    }

    private async void PViewerNeutralRead(
        string sourcePath,
        TimeSpan position,
        int width,
        int height,
        int pixelX,
        int pixelY,
        int loadSerial,
        int neutralSerial,
        LNeutralTarget target)
    {
        LMediaFrame? pViewerFrame = await LMedia.LMediaFrameStart(sourcePath, position, width, height);

        if (LViewer.LViewerUnloaded
            || loadSerial != LViewer.LViewerLoadSerial
            || neutralSerial != LViewer.LViewerNeutralSerial)
        {
            return;
        }

        if (pViewerFrame is null)
        {
            PViewerNeutralChange?.Invoke(
                new LNeutralSample(LNeutralOutcome.LNeutralOutcomeDecode, 0, 0, 0, 1, 1, 1));
            return;
        }

        LNeutralSample pViewerSample = LNeutral.LNeutralResolve(
            pViewerFrame.LMediaFramePixels,
            pViewerFrame.LMediaFrameWidth,
            pViewerFrame.LMediaFrameHeight,
            pixelX, pixelY, target);
        PViewerNeutralChange?.Invoke(pViewerSample);
    }

    public async void PViewerEstimateRead(LWhitebalanceMethod pMethod, Action<LNeutralWheel> pEstimate)
    {
        if (LViewer.LViewerMediaInfo is not { LMediaVideoPresent: true } pViewerMediaInfo)
        {
            pEstimate(new LNeutralWheel(0, 0, false));
            return;
        }

        int pViewerSourceWidth = pViewerMediaInfo.LMediaVideoWidth;
        int pViewerSourceHeight = pViewerMediaInfo.LMediaVideoHeight;
        string? pViewerPath = PViewerSourcePath;
        if (pViewerSourceWidth <= 0 || pViewerSourceHeight <= 0 || string.IsNullOrWhiteSpace(pViewerPath))
        {
            pEstimate(new LNeutralWheel(0, 0, false));
            return;
        }

        TimeSpan pViewerTime = LViewer.LViewerTimeRead();
        int pViewerLoadClaim = LViewer.LViewerLoadSerial;

        LMediaFrame? pViewerFrame = await LMedia.LMediaFrameStart(
            pViewerPath, pViewerTime, pViewerSourceWidth, pViewerSourceHeight);

        if (LViewer.LViewerUnloaded || pViewerLoadClaim != LViewer.LViewerLoadSerial)
        {
            return;
        }

        pEstimate(pViewerFrame is null
            ? new LNeutralWheel(0, 0, false)
            : LNeutral.LNeutralAnalyzeResolve(
                pViewerFrame.LMediaFramePixels,
                pViewerFrame.LMediaFrameWidth,
                pViewerFrame.LMediaFrameHeight,
                pMethod));
    }

    private (Rect, Rect) PViewerGeometryRead()
    {
        Size pViewerRotated = PCropDisplayRead();
        return (PCropRectRead(), new Rect(0, 0, pViewerRotated.Width, pViewerRotated.Height));
    }
}
