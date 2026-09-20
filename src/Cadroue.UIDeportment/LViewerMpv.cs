using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class LViewerMpv
{
    private readonly LViewer lViewer;

    public LViewerMpv(LViewer lOwner)
    {
        lViewer = lOwner;
    }

    public event Action? LViewerToolCancel;

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
        LViewerToolCancel?.Invoke();
        bool lHasPreview = LPlayer.LPlayerReady && lCargo.LCargoPreviewAvailable;
        string lPath = lCargo.LCargoSourcePath ?? "(no path)";
        string lFileName = System.IO.Path.GetFileName(lPath);
        if (lCargo.LCargoMediaInfo is { } lInfo)
        {
            LTraceLog.LTraceInfoRecord(
                $"Media opened '{lFileName}': {lInfo.LMediaInfoDuration:hh\\:mm\\:ss\\.fff} (mpv preview) [{lPath}]");
        }
        else
        {
            LTraceLog.LTraceErrorRecord(
                $"Media rejected '{lFileName}': {lCargo.LCargoFfmpegError ?? "unreadable"} [{lPath}]");
        }

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
        string lName = System.IO.Path.GetFileName(lSourcePath);
        if (lViewer.LViewerEditEligible)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv could not open '{lName}': {lMpvReason}; preview unavailable "
                + $"(mpv is the Edit preview engine, no Flyleaf fallback) [{lSourcePath}]");
            LViewerMpvCommit(
                new LCargo(lSourcePath, lMediaInfo, lMediaInfo is not null, false, lFfmpegError, lMpvReason));
            return;
        }

        LTraceLog.LTraceErrorRecord(
            $"mpv could not open '{lName}': {lMpvReason}; "
            + $"falling back to the existing engine for this file [{lSourcePath}]");
        lViewer.LViewerRenderer.LViewerEngineApply(false);
        if (!lViewer.LViewerSerialCheck(lSerial))
        {
            return;
        }

        await lViewer.LViewerMedia.LViewerFlyleafApply(lSourcePath, lMediaInfo, lFfmpegError, lSerial);
    }
}
