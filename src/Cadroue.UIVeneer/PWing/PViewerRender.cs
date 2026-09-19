using System;

using Cadroue.Core;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PViewer
{
    private void PViewerPreviewApply()
    {
        if (LViewer.LViewerMpvActive)
        {
            PViewerMpvUpdate();
            LViewer.LViewerPreviewRaise();
            return;
        }

        LPreview.LPreviewApply(pViewerPlayer.PPlayerFlyleafPlayer, PViewerRenderRead());
        PPlayerFactsRecord(pViewerPlayer.PPlayerFlyleafPlayer, "Preview color applied");
        LViewer.LViewerPreviewRaise();
    }

    public LPreviewState PViewerRenderRead() =>
        PCropActive
            ? LViewer.LViewerPreview
            : LViewer.LViewerPreview
                .LRotateFlipChange(LRotateFlip.LRotateDefaultCreate())
                .LCropboxChange(null);

    public string PViewerAudioRead() => LViewer.LViewerAudioResolve();

    private void PViewerPreviewRestore()
    {
        LRotateFlip pViewerRotate = LViewer.LViewerPreview.LRotateFlip;
        LTraceLog.LTraceInfoRecord(
            $"Viewer preview restored: rotate {pViewerRotate.LRotateKind}, "
            + $"H {pViewerRotate.LRotateFlipHorizontal}, V {pViewerRotate.LRotateFlipVertical}");
        LPreview.LPreviewRestore(pViewerPlayer.PPlayerFlyleafPlayer, LViewer.LViewerPreview);
    }

    public TimeSpan PViewerDurationRead() => LViewer.LViewerDuration;

    public void PViewerRotateSet(LRotateFlip pRotateFlip)
    {
        LViewer.LViewerPreviewSet(LViewer.LViewerPreview.LRotateFlipChange(pRotateFlip));
        LTraceLog.LTraceInfoRecord(
            $"Viewer rotate/flip set: rotate {pRotateFlip.LRotateKind}, "
            + $"H {pRotateFlip.LRotateFlipHorizontal}, V {pRotateFlip.LRotateFlipVertical}, "
            + $"player {(pViewerPlayer.PPlayerReady ? "ready" : "none")}, overlay remapped");
        PViewerPreviewApply();
        PCropOverlayUpdate();
    }

    public void PViewerColorSet(LColor pColor)
    {
        LViewer.LViewerPreviewSet(LViewer.LViewerPreview.LColorChange(pColor));
        PViewerPreviewApply();
    }
}
