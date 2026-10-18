using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

// The Flyleaf engine's open and commit of a loaded media.
public sealed class QViewerFlyleaf
{
    private readonly QViewer qViewer;

    public QViewerFlyleaf(QViewer lOwner)
    {
        qViewer = lOwner;
    }

    private QPlayer QPlayer => qViewer.QPlayer;

    public async Task QViewerFlyleafApply(string lSourcePath, LMediaInfo? lMediaInfo, string? lFfmpegError, int lSerial)
    {
        if (lMediaInfo is { LMediaAudioOnly: true } && !qViewer.QViewerAudioAllowed)
        {
            string lAudioOnlyError = QLocalizationCatalog.QLocalizationTextRead("Viewer.Error.AudioOnlyTab");
            QViewerFlyleafCommit(new LCargo(lSourcePath, null, false, false, lAudioOnlyError, lAudioOnlyError), false);
            return;
        }

        bool lCreated = !QPlayer.QPlayerReady;
        string? lPreviewError = await QViewerPlayerOpen(lSourcePath, lCreated);
        if (!qViewer.QViewerTrace.CViewerSerialCheck(lSerial))
        {
            if (lCreated)
            {
                QPlayer.QPlayerDispose();
            }

            qViewer.QViewerTrace.CViewerDiscardedRecord(
                lSerial, qViewer.QViewerTrace.CViewerSerial, qViewer.QViewerTrace.CViewerUnloaded);
            return;
        }

        if (QPlayer.QPlayerReady)
        {
            QPlayer.QPlayerRendererSet(true);
            QPlayer.QPlayerPause();
            QPlayer.QPlayerSeek(TimeSpan.Zero);
        }

        QViewerFlyleafCommit(
            new LCargo(
                lSourcePath, lMediaInfo, lMediaInfo is not null, QPlayer.QPlayerReady, lFfmpegError, lPreviewError),
            !lCreated);
    }

    // The Flyleaf player opens the path, timed for its record; the failure's message comes back, the player disposed.
    private async Task<string?> QViewerPlayerOpen(string lSourcePath, bool lCreated)
    {
        var lChronometer = new QChronometer();
        lChronometer.QChronometerStart();
        try
        {
            if (lCreated)
            {
                qViewer.QViewerMedia.QViewerPlayerRaise();
            }

            QPlayer.QPlayerVolumeSet(qViewer.QViewerAudio.QViewerVolume);
            double lBeforeOpen = lChronometer.QChronometerRead();
            await QPlayer.QPlayerOpenStart(lSourcePath);
            QPlayer.QPlayerFactsRecord(
                $"Player opened '{System.IO.Path.GetFileName(lSourcePath)}'",
                lChronometer.QChronometerRead() - lBeforeOpen);
        }
        catch (Exception lException)
        {
            string lPreviewError = lException.Message;
            QPlayer.QPlayerDispose();
            return lPreviewError;
        }

        return null;
    }

    public void QViewerFlyleafCommit(LCargo lCargo, bool lReused)
    {
        LMediaTrace.LMediaOpenRecord(lCargo, QPlayer.QPlayerReady);
        QPlayer.QPlayerAccurateReset();
        qViewer.QViewerTrace.CViewerPlayerRecord(lReused, QPlayer.QPlayerReady);
        if (!QPlayer.QPlayerReady)
        {
            QPlayer.QPlayerDispose();
        }

        qViewer.QViewerHost.QViewerHostShow(QPlayer.QPlayerReady);
        QPlayer.QPlayerEndSet(lCargo.LCargoMediaInfo?.LMediaVideoEnd);
        qViewer.QViewerMedia.QViewerCargoApply(lCargo);
        if (!QPlayer.QPlayerReady)
        {
            qViewer.QViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        LRotateFlip lRotate = qViewer.QViewerPreview.LRotateFlip;
        qViewer.QViewerTrace.CViewerPreviewRecord(
            lRotate.LRotateKind.ToString(), lRotate.LRotateFlipHorizontal, lRotate.LRotateFlipVertical);
        QPlayer.QPlayerPreviewApply(qViewer.QViewerPreview, "preview restored");
        qViewer.QViewerMedia.QViewerCommitApply(null);
    }
}
