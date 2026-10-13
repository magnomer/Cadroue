using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class QViewerMpv
{
    private readonly QViewer qViewer;

    public QViewerMpv(QViewer lOwner)
    {
        qViewer = lOwner;
    }


    private QPlayer QPlayer => qViewer.QPlayer;

    private bool QViewerCropPersistent => qViewer.QCrop.QCropPersistent;

    public async Task QViewerMpvApply(string lSourcePath, LMediaInfo? lMediaInfo, string? lFfmpegError, int lSerial)
    {
        if (lMediaInfo is { LMediaAudioOnly: true } && !qViewer.QViewerAudioAllowed)
        {
            string lAudioError = LLocalization.LLocalizationTextRead("Viewer.Error.AudioOnlyTab");
            QViewerMpvCommit(new LCargo(lSourcePath, null, false, false, lAudioError, lAudioError));
            return;
        }

        string? lPreviewError = null;
        try
        {
            if (!QPlayer.QPlayerReady)
            {
                qViewer.QViewerMedia.QViewerPlayerRaise();
                QPlayer.QPlayerAppliedReset();
            }

            QPlayer.QPlayerVolumeSet(qViewer.QViewerAudio.QViewerVolume);
            if (!qViewer.QViewerSerialCheck(lSerial))
            {
                return;
            }

            if (QViewerCropPersistent)
            {
                QPlayer.QPlayerFilterApply(LPreview.LPreviewFilterResolve(qViewer.QViewerRenderRead()));
            }

            await QPlayer.QPlayerOpenStart(lSourcePath);
        }
        catch (Exception lException)
        {
            lPreviewError = lException.Message;
        }

        if (!qViewer.QViewerSerialCheck(lSerial))
        {
            QViewerMpvRestore();
            return;
        }

        if (lPreviewError is not null)
        {
            QPlayer.QPlayerDispose();
            await QViewerMpvRebuild(lSourcePath, lMediaInfo, lFfmpegError, lSerial, lPreviewError);
            return;
        }

        QViewerMpvCommit(new LCargo(
            lSourcePath,
            lMediaInfo,
            lMediaInfo is not null,
            QPlayer.QPlayerReady,
            lFfmpegError,
            null));
    }

    private void QViewerMpvRestore()
    {
        if (qViewer.QViewerUnloaded || !qViewer.QViewerMpvActive || qViewer.QViewerIntent is not null)
        {
            return;
        }

        if (qViewer.QViewerSourcePath is { } lKeptPath)
        {
            var lRequest = new QViewerIntent(lKeptPath, qViewer.QViewerPosition, false);
            if (qViewer.QViewerCommandActive)
            {
                _ = qViewer.QViewerMedia.QViewerLoadStart(lRequest);
                return;
            }

            qViewer.QViewerIntentSet(lRequest);
            return;
        }

        if (QPlayer.QPlayerReady)
        {
            QPlayer.QPlayerStop();
            qViewer.QViewerHost.QViewerHostShow(false);
        }
    }

    public void QViewerMpvCommit(LCargo lCargo)
    {
        qViewer.QViewerNeutral.QViewerNeutralCancel();
        bool lHasPreview = QPlayer.QPlayerReady && lCargo.LCargoPreviewAvailable;
        LMediaTrace.LMediaMpvRecord(lCargo);

        QPlayer.QPlayerAccurateReset();
        qViewer.QViewerHost.QViewerHostShow(lHasPreview);
        QViewerIntent? lRequest = qViewer.QViewerMedia.QViewerCargoApply(lCargo);
        if (!lHasPreview)
        {
            QPlayer.QPlayerDispose();
            qViewer.QViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        qViewer.QViewerFilterUpdate();
        qViewer.QViewerAudio.QViewerAudioApply(qViewer.QViewerMpvActive);
        qViewer.QViewerMedia.QViewerIntentApply(lRequest);
    }

    private async Task QViewerMpvRebuild(
        string lSourcePath,
        LMediaInfo? lMediaInfo,
        string? lFfmpegError,
        int lSerial,
        string lMpvReason)
    {
        LMediaTrace.LMediaRebuildRecord(lSourcePath, lMpvReason, qViewer.QViewerEditEligible);
        if (qViewer.QViewerEditEligible)
        {
            QViewerMpvCommit(
                new LCargo(lSourcePath, lMediaInfo, lMediaInfo is not null, false, lFfmpegError, lMpvReason));
            return;
        }

        qViewer.QViewerRenderer.QViewerEngineApply(false);
        if (!qViewer.QViewerSerialCheck(lSerial))
        {
            return;
        }

        await qViewer.QViewerMedia.QViewerFlyleafApply(lSourcePath, lMediaInfo, lFfmpegError, lSerial);
    }
}
