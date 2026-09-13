using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PViewer
{
    public event Action<LNeutralSample>? PViewerNeutralChange;
    public event Action<bool, LNeutralTarget>? PViewerToolChange;

    public void PViewerNeutralSet(bool pNeutralArmed, LNeutralTarget pNeutralTarget)
    {
        if (pNeutralArmed)
        {
            if (pViewerTool == PViewerTool.PViewerToolNeutral)
            {
                pViewerNeutralTarget = pNeutralTarget;
                return;
            }

            pViewerNeutralSerial++;
            pViewerNeutralTarget = pNeutralTarget;
            pViewerNeutralPlaying = LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying;
            if (pViewerNeutralPlaying)
            {
                PViewerPause();
            }

            pViewerTool = PViewerTool.PViewerToolNeutral;
            pViewerOverlay.Cursor = Cursors.Cross;
            pViewerCropBox.Cursor = null;
            pViewerOverlay.Focus();
            PCropOverlayUpdate();
            PViewerToolChange?.Invoke(true, pNeutralTarget);
            return;
        }

        if (pViewerTool != PViewerTool.PViewerToolNeutral)
        {
            return;
        }

        pViewerNeutralSerial++;
        PViewerNeutralReset();
    }

    public void PViewerNeutralCancel() => PViewerNeutralSet(false, pViewerNeutralTarget);

    private void PViewerNeutralReset()
    {
        pViewerTool = PViewerTool.PViewerToolNone;
        pViewerOverlay.Cursor = null;
        if (pViewerOverlay.IsMouseCaptured)
        {
            pViewerOverlay.ReleaseMouseCapture();
        }

        bool pViewerResume = pViewerNeutralPlaying;
        pViewerNeutralPlaying = false;
        if (pViewerResume)
        {
            PViewerPlay();
        }

        PCropOverlayUpdate();
        PViewerToolChange?.Invoke(false, pViewerNeutralTarget);
    }

    private void PViewerKeyHandle(object sender, KeyEventArgs keyEvent)
    {
        if (pViewerTool == PViewerTool.PViewerToolNeutral && keyEvent.Key == Key.Escape)
        {
            PViewerNeutralSet(false, pViewerNeutralTarget);
            keyEvent.Handled = true;
        }
    }

    private void PViewerPressHandle(MouseButtonEventArgs mouseEvent)
    {
        mouseEvent.Handled = true;
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaVideoPresent)
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
        LRotateFlip pViewerRotate = LPreviewStateCurrent.LRotateFlip;
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

        TimeSpan pViewerTime = pViewerPlayer.PPlayerReady
            ? pViewerPlayer.PPlayerTimeRead()
            : LPreviewStateCurrent.LPlaybackState.LPlaybackPosition;
        int pViewerLoadClaim = pViewerLoadSerial;
        int pViewerNeutralClaim = pViewerNeutralSerial;
        int pViewerPixelX = pViewerPoint.LNeutralPointX;
        int pViewerPixelY = pViewerPoint.LNeutralPointY;
        LNeutralTarget pViewerTarget = pViewerNeutralTarget;

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
        LMediaFrame? pViewerFrame = await Task.Run(
            () => LMedia.LMediaFrameRead(sourcePath, position, width, height));

        if (pViewerUnloaded
            || loadSerial != pViewerLoadSerial
            || neutralSerial != pViewerNeutralSerial)
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
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaVideoPresent)
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

        TimeSpan pViewerTime = pViewerPlayer.PPlayerReady
            ? pViewerPlayer.PPlayerTimeRead()
            : LPreviewStateCurrent.LPlaybackState.LPlaybackPosition;
        int pViewerLoadClaim = pViewerLoadSerial;

        LMediaFrame? pViewerFrame = await Task.Run(
            () => LMedia.LMediaFrameRead(pViewerPath, pViewerTime, pViewerSourceWidth, pViewerSourceHeight));

        if (pViewerUnloaded || pViewerLoadClaim != pViewerLoadSerial)
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

    public async void PViewerFrameRead(Action<LMediaFrame?> pFrameReady)
    {
        if (pViewerMediaInfo is null || !pViewerMediaInfo.LMediaVideoPresent)
        {
            pFrameReady(null);
            return;
        }

        int pViewerSourceWidth = pViewerMediaInfo.LMediaVideoWidth;
        int pViewerSourceHeight = pViewerMediaInfo.LMediaVideoHeight;
        string? pViewerPath = PViewerSourcePath;
        if (pViewerSourceWidth <= 0 || pViewerSourceHeight <= 0 || string.IsNullOrWhiteSpace(pViewerPath))
        {
            pFrameReady(null);
            return;
        }

        TimeSpan pViewerTime = pViewerPlayer.PPlayerReady
            ? pViewerPlayer.PPlayerTimeRead()
            : LPreviewStateCurrent.LPlaybackState.LPlaybackPosition;
        int pViewerLoadClaim = pViewerLoadSerial;

        LMediaFrame? pViewerFrame = await Task.Run(
            () => LMedia.LMediaFrameRead(pViewerPath, pViewerTime, pViewerSourceWidth, pViewerSourceHeight));

        if (pViewerUnloaded || pViewerLoadClaim != pViewerLoadSerial)
        {
            return;
        }

        pFrameReady(pViewerFrame);
    }

    private (Rect, Rect) PViewerGeometryRead()
    {
        Size pViewerRotated = PCropDisplayRead();
        return (PCropRectRead(), new Rect(0, 0, pViewerRotated.Width, pViewerRotated.Height));
    }
}
