using Cadroue.Application;
using Cadroue.Conduct;
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
            string lAudioError = QLocalizationCatalog.QLocalizationTextRead("Viewer.Error.AudioOnlyTab");
            QViewerMpvCommit(new LCargo(lSourcePath, null, false, false, lAudioError, lAudioError));
            return;
        }

        (bool lCurrent, string? lPreviewError) = await QViewerMpvOpen(lSourcePath, lSerial);
        if (!lCurrent)
        {
            return;
        }

        if (!qViewer.QViewerTrace.CViewerSerialCheck(lSerial))
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

    // The mpv player opens the path; false when a newer load overtook it before the open, else the failure's message.
    private async Task<(bool, string?)> QViewerMpvOpen(string lSourcePath, int lSerial)
    {
        try
        {
            if (!QPlayer.QPlayerReady)
            {
                qViewer.QViewerMedia.QViewerPlayerRaise();
                QPlayer.QPlayerAppliedReset();
            }

            QPlayer.QPlayerVolumeSet(qViewer.QViewerAudio.QViewerVolume);
            if (!qViewer.QViewerTrace.CViewerSerialCheck(lSerial))
            {
                return (false, null);
            }

            if (QViewerCropPersistent)
            {
                QPlayer.QPlayerFilterApply(LPreviewFilter.LPreviewFilterResolve(qViewer.QViewerRenderRead()));
            }

            await QPlayer.QPlayerOpenStart(lSourcePath);
        }
        catch (Exception lException)
        {
            return (true, lException.Message);
        }

        return (true, null);
    }

    private void QViewerMpvRestore()
    {
        if (qViewer.QViewerTrace.CViewerUnloaded || !qViewer.QViewerTrace.CViewerMpvActive)
        {
            return;
        }

        if (qViewer.QViewerWorkspace.CWorkspaceIdleRestore(qViewer.QViewerPosition, false))
        {
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
        qViewer.QViewerMedia.QViewerCargoApply(lCargo);
        if (!lHasPreview)
        {
            QPlayer.QPlayerDispose();
            qViewer.QViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        // A refresh mpv rejects is recorded by the workspace as it spends the intent; the audio goes on as the
        // player starts.
        Exception? lRefreshException = QViewerRefreshApply();
        qViewer.QViewerMedia.QViewerCommitApply(lRefreshException);
    }

    // The preview filter goes to mpv and a paused player refreshes; the refused refresh comes back for the record.
    private Exception? QViewerRefreshApply()
    {
        if (!qViewer.QViewerFilter.QViewerFilterApply())
        {
            return null;
        }

        try
        {
            QPlayer.QPlayerUpdate();
        }
        catch (Exception lException)
        {
            return lException;
        }

        return null;
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
        if (!qViewer.QViewerTrace.CViewerSerialCheck(lSerial))
        {
            return;
        }

        await qViewer.QViewerMedia.QViewerFlyleaf.QViewerFlyleafApply(lSourcePath, lMediaInfo, lFfmpegError, lSerial);
    }
}
