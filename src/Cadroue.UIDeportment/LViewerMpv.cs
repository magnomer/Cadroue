using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class LViewerMpv
{
    private readonly LViewer lViewer;

    public LViewerMpv(LViewer lOwner)
    {
        lViewer = lOwner;
    }


    private LPlayer LPlayer => lViewer.LPlayer;

    private bool LViewerCropPersistent => lViewer.LCrop.LCropPersistent;

    public async Task LViewerMpvApply(string lSourcePath, LMediaInfo? lMediaInfo, string? lFfmpegError, int lSerial)
    {
        if (lMediaInfo is { LMediaAudioOnly: true } && !lViewer.LViewerAudioAllowed)
        {
            string lAudioError = LLocalization.LLocalizationTextRead("Viewer.Error.AudioOnlyTab");
            LViewerMpvCommit(new LCargo(lSourcePath, null, false, false, lAudioError, lAudioError));
            return;
        }

        string? lPreviewError = null;
        try
        {
            if (!LPlayer.LPlayerReady)
            {
                lViewer.LViewerMedia.LViewerPlayerRaise();
                LPlayer.LPlayerAppliedReset();
            }

            LPlayer.LPlayerVolumeSet(lViewer.LViewerVolume);
            if (!lViewer.LViewerSerialCheck(lSerial))
            {
                return;
            }

            if (LViewerCropPersistent)
            {
                LPlayer.LPlayerFilterApply(LPreview.LPreviewFilterResolve(lViewer.LViewerRenderRead()));
            }

            await LPlayer.LPlayerOpenStart(lSourcePath);
        }
        catch (Exception lException)
        {
            lPreviewError = lException.Message;
        }

        if (!lViewer.LViewerSerialCheck(lSerial))
        {
            LViewerMpvRestore();
            return;
        }

        if (lPreviewError is not null)
        {
            LPlayer.LPlayerDispose();
            await LViewerMpvRebuild(lSourcePath, lMediaInfo, lFfmpegError, lSerial, lPreviewError);
            return;
        }

        LViewerMpvCommit(new LCargo(
            lSourcePath,
            lMediaInfo,
            lMediaInfo is not null,
            LPlayer.LPlayerReady,
            lFfmpegError,
            null));
    }

    private void LViewerMpvRestore()
    {
        if (lViewer.LViewerUnloaded || !lViewer.LViewerMpvActive || lViewer.LViewerIntent is not null)
        {
            return;
        }

        if (lViewer.LViewerSourcePath is { } lKeptPath)
        {
            var lRequest = new LViewerIntent(lKeptPath, lViewer.LViewerPosition, false);
            if (lViewer.LViewerCommandActive)
            {
                _ = lViewer.LViewerMedia.LViewerLoadStart(lRequest);
                return;
            }

            lViewer.LViewerIntentSet(lRequest);
            return;
        }

        if (LPlayer.LPlayerReady)
        {
            LPlayer.LPlayerStop();
            lViewer.LViewerMedia.LViewerHostShow(false);
        }
    }

    public void LViewerMpvCommit(LCargo lCargo)
    {
        lViewer.LViewerNeutral.LViewerNeutralCancel();
        bool lHasPreview = LPlayer.LPlayerReady && lCargo.LCargoPreviewAvailable;
        LMediaTrace.LMediaMpvRecord(lCargo);

        LPlayer.LPlayerAccurateReset();
        lViewer.LViewerMedia.LViewerHostShow(lHasPreview);
        LViewerIntent? lRequest = lViewer.LViewerMedia.LViewerCargoApply(lCargo);
        if (!lHasPreview)
        {
            LPlayer.LPlayerDispose();
            lViewer.LViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        lViewer.LViewerFilterUpdate();
        lViewer.LViewerAudioApply();
        lViewer.LViewerMedia.LViewerIntentApply(lRequest);
    }

    private async Task LViewerMpvRebuild(
        string lSourcePath,
        LMediaInfo? lMediaInfo,
        string? lFfmpegError,
        int lSerial,
        string lMpvReason)
    {
        LMediaTrace.LMediaRebuildRecord(lSourcePath, lMpvReason, lViewer.LViewerEditEligible);
        if (lViewer.LViewerEditEligible)
        {
            LViewerMpvCommit(
                new LCargo(lSourcePath, lMediaInfo, lMediaInfo is not null, false, lFfmpegError, lMpvReason));
            return;
        }

        lViewer.LViewerRenderer.LViewerEngineApply(false);
        if (!lViewer.LViewerSerialCheck(lSerial))
        {
            return;
        }

        await lViewer.LViewerMedia.LViewerFlyleafApply(lSourcePath, lMediaInfo, lFfmpegError, lSerial);
    }
}
