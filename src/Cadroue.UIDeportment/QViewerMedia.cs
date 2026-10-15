using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class QViewerMedia
{
    private readonly QViewer qViewer;
    private readonly LMediaLoader qViewerProbe = new();

    public QViewerMedia(QViewer lOwner)
    {
        qViewer = lOwner;
        QViewerLoupe = new QViewerLoupe(lOwner, this);
        QViewerTeardown = new QViewerTeardown(lOwner, this, qViewerProbe.Dispose, LTraceLog.LTraceErrorRecord);
    }

    public event Action? QViewerPlayerCreated;

    public QViewerLoupe QViewerLoupe { get; }

    public QViewerTeardown QViewerTeardown { get; }

    private QPlayer QPlayer => qViewer.QPlayer;

    private bool QViewerCropPersistent => qViewer.QCrop.QCropPersistent;

    private QViewerRenderer QViewerRenderer => qViewer.QViewerRenderer;

    private QViewerHost QViewerHost => qViewer.QViewerHost;

    public void QViewerPlayerRaise() => QViewerPlayerCreated?.Invoke();

    public void QViewerCommandApply(bool lCommandActive)
    {
        if (qViewer.QViewerUnloaded || qViewer.QViewerCommandActive == lCommandActive)
        {
            return;
        }

        QViewerHost.QViewerHostRecord($"command set {(lCommandActive ? "on" : "off")}");
        if (!lCommandActive)
        {
            qViewer.QViewerPlayback.QViewerSuspend();
            qViewer.QViewerCommandSet(false);
            qViewer.QViewerSerialChange();
            return;
        }

        qViewer.QViewerCommandSet(true);
        if (QViewerRenderer.QViewerEngineRestore())
        {
            return;
        }

        qViewer.QViewerPlayback.QViewerResume();
    }

    public event Action? QViewerLoupeClosed;

    public bool QViewerMediaClose(bool lForce)
    {
        if (qViewer.QViewerUnloaded || (!lForce && !qViewer.QViewerCommandActive))
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

        LTraceLog.LTraceInfoRecord(string.IsNullOrWhiteSpace(lClosedPath)
            ? "Media closed"
            : $"Media closed '{System.IO.Path.GetFileName(lClosedPath)}' [{lClosedPath}]");

        qViewer.QViewerMediaRaise(new LCargo(string.Empty, null, false, false, null, null));
        return true;
    }

    public bool QViewerLoadCancel()
    {
        if (!qViewer.QViewerWorkspace.CWorkspaceCancel())
        {
            return false;
        }

        qViewer.QViewerSerialChange();
        return true;
    }

    public void QViewerPlayerStop()
    {
        qViewer.QViewerPlayback.QViewerClockRun(false);
        QPlayer.QPlayerAccurateReset();
        qViewer.QViewerResumeSet(false);
        qViewer.QViewerPlaybackUpdate(false, null);
        LTraceLog.LTraceInfoRecord(
            $"Viewer host detached: player {(QPlayer.QPlayerReady ? "released" : "none")}");
        QPlayer.QPlayerDispose();
    }

    // An open: the workspace starts a new load of the path once the viewer may load.
    public Task QViewerLoadStart(string lSourcePath) => QViewerLoadRun(lSourcePath, true);

    // A restore: the workspace asks its pending load or its held source loaded again;
    // an inactive viewer leaves it pending.
    public void QViewerLoadRestore(string lLoadPath) => _ = QViewerLoadRun(lLoadPath, false);

    private Task QViewerLoadRun(string lSourcePath, bool lFresh)
    {
        if (!qViewer.QViewerCommandActive)
        {
            return Task.CompletedTask;
        }

        QViewerRenderer.QViewerEngineUpdate();
        int lSerial = qViewer.QViewerSerialChange();
        qViewer.QViewerPlayback.QViewerClockRun(false);
        qViewer.QViewerResumeSet(false);
        if (QPlayer.QPlayerReady)
        {
            QPlayer.QPlayerPause();
            qViewer.QViewerPlaybackUpdate(false, null);
        }

        if (!qViewer.QViewerSerialCheck(lSerial))
        {
            return Task.CompletedTask;
        }

        if (lFresh)
        {
            qViewer.QViewerWorkspace.CWorkspaceStart(lSourcePath);
        }

        qViewerProbe.LMediaLoadTail = !qViewer.QViewerMpvActive;
        LTraceLog.LTraceInfoRecord(
            $"Video load start serial={lSerial} '{System.IO.Path.GetFileName(lSourcePath)}'",
            $"engine={qViewer.QViewerEngine}, probing (ffprobe)…");
        return QViewerProbeRun(lSourcePath);
    }

    private async Task QViewerProbeRun(string lSourcePath)
    {
        LMediaLoadOutcome lOutcome = await qViewerProbe.LMediaLoadStart(lSourcePath);
        int lSerial = qViewer.QViewerLoadSerial;
        LTraceLog.LTraceInfoRecord(
            $"Video probe outcome {lOutcome.LMediaLoadKind} '{System.IO.Path.GetFileName(lOutcome.LMediaLoadPath)}'",
            lOutcome.LMediaLoadError);
        if (qViewer.QViewerUnloaded || !qViewer.QViewerWorkspace.CWorkspaceIntentMatch(lOutcome.LMediaLoadPath))
        {
            LTraceLog.LTraceInfoRecord(
                "Video probe outcome discarded (stale/superseded): "
                + $"serial={lSerial}, unloaded={qViewer.QViewerUnloaded}");
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
        if (qViewer.QViewerMpvActive)
        {
            qViewer.QViewerMpv.QViewerMpvCommit(lCargo);
            return;
        }

        QViewerFlyleafCommit(lCargo, false);
    }

    private Task QViewerMediaApply(string lSourcePath, LMediaInfo? lMediaInfo, string? lFfmpegError, int lSerial) =>
        qViewer.QViewerMpvActive
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
        string? lPreviewError = null;
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
            lPreviewError = lException.Message;
            QPlayer.QPlayerDispose();
        }

        if (!qViewer.QViewerSerialCheck(lSerial))
        {
            if (lCreated)
            {
                QPlayer.QPlayerDispose();
            }

            LTraceLog.LTraceInfoRecord(
                "Player open discarded (stale/superseded): "
                + $"serial got={lSerial} now={qViewer.QViewerLoadSerial}, unloaded={qViewer.QViewerUnloaded}");
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

    private void QViewerFlyleafCommit(LCargo lCargo, bool lReused)
    {
        LMediaTrace.LMediaOpenRecord(lCargo, QPlayer.QPlayerReady);
        QPlayer.QPlayerAccurateReset();
        if (lReused && QPlayer.QPlayerReady)
        {
            LTraceLog.LTraceInfoRecord("Viewer player reused: same player kept on the host, no swap chain rebuild");
        }
        else
        {
            LTraceLog.LTraceInfoRecord(
                $"Viewer player swapped: next {(QPlayer.QPlayerReady ? "ready" : "none")}");
            if (!QPlayer.QPlayerReady)
            {
                QPlayer.QPlayerDispose();
            }
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
        LTraceLog.LTraceInfoRecord(
            $"Viewer preview restored: rotate {lRotate.LRotateKind}, "
            + $"H {lRotate.LRotateFlipHorizontal}, V {lRotate.LRotateFlipVertical}");
        QPlayer.QPlayerPreviewApply(qViewer.QViewerPreview, "preview restored");
        QViewerCommitApply();
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

    // The player holds the committed media: the workspace spends its intent, and the autoplay
    // preference, read at each commit, decides an intent that left the play state open.
    public void QViewerCommitApply() =>
        qViewer.QViewerWorkspace.CWorkspaceCommitApply(LPreference.LPreferenceStateCurrent.LPreferenceAutoplay);

    // The workspace spent the committed intent: the player starts at its place, playing.
    public void QViewerCommitPlay(TimeSpan lPosition)
    {
        QViewerCommitSeek(lPosition);
        QPlayer.QPlayerPlay();
        qViewer.QViewerPlaybackUpdate(true, lPosition);
        qViewer.QViewerPlayback.QViewerClockRun(true);
    }

    // The workspace spent the committed intent: the player waits at its place, paused.
    public void QViewerCommitPause(TimeSpan lPosition)
    {
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
