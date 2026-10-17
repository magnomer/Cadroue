using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class QViewerMedia
{
    private readonly QViewer qViewer;
    private readonly LMediaLoader qViewerProbe = new();

    public QViewerMedia(QViewer lOwner)
    {
        qViewer = lOwner;
        QViewerLoupe = new QViewerLoupe(lOwner);
        QViewerTeardown = new QViewerTeardown(lOwner, this);
    }

    public event Action? QViewerPlayerCreated;

    public QViewerLoupe QViewerLoupe { get; }

    public QViewerTeardown QViewerTeardown { get; }

    private QPlayer QPlayer => qViewer.QPlayer;

    private bool QViewerCropPersistent => qViewer.QCrop.QCropPersistent;

    private QViewerRenderer QViewerRenderer => qViewer.QViewerRenderer;

    private QViewerHost QViewerHost => qViewer.QViewerHost;

    public void QViewerPlayerRaise() => QViewerPlayerCreated?.Invoke();

    public void QViewerProbeDispose() => qViewerProbe.Dispose();

    // The gate decides and orders the change; the player's suspend, the renderer's restore and the player's resume
    // stay here as the commands it runs.
    public void QViewerCommandApply(bool lCommandActive) =>
        qViewer.QViewerTrace.CViewerCommandSet(
            lCommandActive,
            qViewer.QViewerPlayback.QViewerSuspend,
            QViewerRenderer.QViewerEngineRestore,
            qViewer.QViewerPlayback.QViewerResume);

    public event Action? QViewerLoupeClosed;

    public bool QViewerMediaClose(bool lForce)
    {
        if (qViewer.QViewerTrace.CViewerUnloaded || (!lForce && !qViewer.QViewerTrace.CViewerCommandActive))
        {
            return false;
        }

        if (qViewer.QViewerLoupeActive)
        {
            QViewerLoupeClosed?.Invoke();
        }

        bool lLoadClosed = qViewerProbe.LMediaLoadClose();
        if (!lLoadClosed && string.IsNullOrWhiteSpace(qViewer.QViewerWorkspace.CWorkspaceSource)
            && !qViewer.QViewerWorkspace.CWorkspaceMedia)
        {
            return false;
        }

        string lClosedPath = qViewer.QViewerWorkspace.CWorkspaceSource ?? string.Empty;
        qViewer.QViewerMediaClose();
        QViewerHost.QViewerHostShow(false);
        QViewerPlayerStop();
        qViewer.QCrop.QCropHide();
        qViewer.QViewerPreviewApply();

        qViewer.QViewerTrace.CViewerClosedRecord(lClosedPath);

        qViewer.QViewerMediaRaise(new LCargo(string.Empty, null, false, false, null, null));
        return true;
    }

    public bool QViewerLoadCancel()
    {
        if (!qViewer.QViewerWorkspace.CWorkspaceCancel())
        {
            return false;
        }

        qViewer.QViewerTrace.CViewerSerialChange();
        return true;
    }

    public void QViewerPlayerStop()
    {
        qViewer.QViewerPlayback.QViewerClockRun(false);
        QPlayer.QPlayerAccurateReset();
        qViewer.QViewerResumeSet(false);
        qViewer.QViewerPlaybackUpdate(false, null);
        qViewer.QViewerTrace.CViewerDetachedRecord(QPlayer.QPlayerReady);
        QPlayer.QPlayerDispose();
    }

    // An open: the workspace starts a new load of the path once the viewer may load.
    public Task QViewerLoadStart(string lSourcePath) => QViewerLoadRun(lSourcePath, true);

    // A restore: the workspace asks its pending load or its held source loaded again;
    // an inactive viewer leaves it pending.
    public void QViewerLoadRestore(string lLoadPath) => _ = QViewerLoadRun(lLoadPath, false);

    private Task QViewerLoadRun(string lSourcePath, bool lFresh)
    {
        if (!qViewer.QViewerTrace.CViewerCommandActive)
        {
            return Task.CompletedTask;
        }

        QViewerRenderer.QViewerEngineUpdate();
        int lSerial = qViewer.QViewerTrace.CViewerSerialChange();
        qViewer.QViewerPlayback.QViewerClockRun(false);
        qViewer.QViewerResumeSet(false);
        if (QPlayer.QPlayerReady)
        {
            QPlayer.QPlayerPause();
            qViewer.QViewerPlaybackUpdate(false, null);
        }

        if (!qViewer.QViewerTrace.CViewerSerialCheck(lSerial))
        {
            return Task.CompletedTask;
        }

        qViewerProbe.LMediaLoadTail = !qViewer.QViewerTrace.CViewerMpvActive;
        qViewer.QViewerWorkspace.CWorkspaceLoadStart(lSourcePath, lFresh, lSerial, qViewer.QViewerEngine.ToString());
        return QViewerProbeRun(lSourcePath);
    }

    private async Task QViewerProbeRun(string lSourcePath)
    {
        LMediaLoadOutcome lOutcome = await qViewerProbe.LMediaLoadStart(lSourcePath);
        int lSerial = qViewer.QViewerTrace.CViewerSerial;
        if (!qViewer.QViewerWorkspace.CWorkspaceOutcomeMatch(
                lOutcome.LMediaLoadKind.ToString(),
                lOutcome.LMediaLoadPath,
                lOutcome.LMediaLoadError,
                lSerial,
                qViewer.QViewerTrace.CViewerUnloaded))
        {
            return;
        }

        if (lOutcome.LMediaLoadKind == LMediaLoadKind.LMediaLoadSuccess)
        {
            await QViewerMediaApply(lOutcome.LMediaLoadPath, lOutcome.LMediaLoadInfo, null, lSerial);
            return;
        }

        if (lOutcome.LMediaLoadKind == LMediaLoadKind.LMediaLoadFailure)
        {
            QViewerCargoCommit(new LCargo(
                lOutcome.LMediaLoadPath,
                null,
                false,
                false,
                lOutcome.LMediaLoadError,
                null));
        }
    }

    private void QViewerCargoCommit(LCargo lCargo)
    {
        if (qViewer.QViewerTrace.CViewerMpvActive)
        {
            qViewer.QViewerMpv.QViewerMpvCommit(lCargo);
            return;
        }

        QViewerFlyleafCommit(lCargo, false);
    }

    private Task QViewerMediaApply(string lSourcePath, LMediaInfo? lMediaInfo, string? lFfmpegError, int lSerial) =>
        qViewer.QViewerTrace.CViewerMpvActive
            ? qViewer.QViewerMpv.QViewerMpvApply(lSourcePath, lMediaInfo, lFfmpegError, lSerial)
            : QViewerFlyleafApply(lSourcePath, lMediaInfo, lFfmpegError, lSerial);

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
                QViewerPlayerCreated?.Invoke();
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

    private void QViewerFlyleafCommit(LCargo lCargo, bool lReused)
    {
        LMediaTrace.LMediaOpenRecord(lCargo, QPlayer.QPlayerReady);
        QPlayer.QPlayerAccurateReset();
        qViewer.QViewerTrace.CViewerPlayerRecord(lReused, QPlayer.QPlayerReady);
        if (!QPlayer.QPlayerReady)
        {
            QPlayer.QPlayerDispose();
        }

        QViewerHost.QViewerHostShow(QPlayer.QPlayerReady);
        QPlayer.QPlayerEndSet(lCargo.LCargoMediaInfo?.LMediaVideoEnd);
        QViewerCargoApply(lCargo);
        if (!QPlayer.QPlayerReady)
        {
            qViewer.QViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        LRotateFlip lRotate = qViewer.QViewerPreview.LRotateFlip;
        qViewer.QViewerTrace.CViewerPreviewRecord(
            lRotate.LRotateKind.ToString(), lRotate.LRotateFlipHorizontal, lRotate.LRotateFlipVertical);
        QPlayer.QPlayerPreviewApply(qViewer.QViewerPreview, "preview restored");
        QViewerCommitApply(null);
    }

    public void QViewerCargoApply(LCargo lCargo)
    {
        qViewer.QViewerMediaCommit(lCargo, QViewerCropPersistent);
        if (!QViewerCropPersistent)
        {
            qViewer.QCrop.QCropHide();
        }

        qViewer.QViewerMediaRaise(lCargo);
    }

    // The player holds the committed media: the workspace records a preview refresh mpv rejected on the way and
    // spends its intent, and the autoplay preference, read at each commit, decides an intent that left the play
    // state open.
    public void QViewerCommitApply(Exception? lRefreshException) =>
        qViewer.QViewerWorkspace.CWorkspaceCommitApply(
            LPreference.LPreferenceStateCurrent.LPreferenceAutoplay, lRefreshException);

    // The workspace spent the committed intent: the player starts at its place, playing, with the mpv audio
    // filter applied first (nothing for Flyleaf).
    public void QViewerCommitPlay(TimeSpan lPosition)
    {
        qViewer.QViewerAudio.QViewerAudioApply(qViewer.QViewerTrace.CViewerMpvActive);
        QViewerCommitSeek(lPosition);
        QPlayer.QPlayerPlay();
        qViewer.QViewerPlaybackUpdate(true, lPosition);
        qViewer.QViewerPlayback.QViewerClockRun(true);
    }

    // The workspace spent the committed intent: the player waits at its place, paused, with the mpv audio filter
    // applied first (nothing for Flyleaf).
    public void QViewerCommitPause(TimeSpan lPosition)
    {
        qViewer.QViewerAudio.QViewerAudioApply(qViewer.QViewerTrace.CViewerMpvActive);
        QViewerCommitSeek(lPosition);
        QPlayer.QPlayerPause();
        qViewer.QViewerPlaybackUpdate(false, lPosition);
    }

    private void QViewerCommitSeek(TimeSpan lPosition)
    {
        if (lPosition > TimeSpan.Zero)
        {
            qViewer.QViewerPlayback.QViewerAccurateSeek(lPosition);
        }

        qViewer.QViewerResumeSet(false);
    }
}
