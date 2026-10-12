using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed record QViewerHostFacts(
    bool QViewerFactsPanel,
    bool QViewerFactsHost,
    string? QViewerFactsState,
    double? QViewerFactsWidth,
    double? QViewerFactsHeight,
    nint QViewerFactsWindow,
    bool QViewerFactsDisposed,
    bool? QViewerFactsOverlay);

public sealed class QViewerMedia
{
    private readonly QViewer qViewer;
    private readonly LMediaLoader qViewerProbe = new();
    private Func<QViewerHostFacts>? qViewerFactsSource;
    private int qViewerHostStamp;

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

    public void QViewerPlayerRaise() => QViewerPlayerCreated?.Invoke();

    public void QViewerFactsAttach(Func<QViewerHostFacts> lFactsSource) => qViewerFactsSource = lFactsSource;

    public void QViewerCommandApply(bool lCommandActive)
    {
        if (qViewer.QViewerUnloaded || qViewer.QViewerCommandActive == lCommandActive)
        {
            return;
        }

        QViewerHostRecord($"command set {(lCommandActive ? "on" : "off")}");
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

    public void QViewerHostShow(bool lVisible)
    {
        bool lChanged = lVisible != qViewer.QViewerHostVisible;
        qViewer.QViewerHostSet(lVisible);
        QViewerRenderer.QViewerHostRaise();
        if (lChanged && !qViewer.QViewerMpvActive)
        {
            QViewerHostRecord($"host {(lVisible ? "shown" : "hidden")}");
        }
    }

    public void QViewerHostRecord(string lStage)
    {
        if (qViewerFactsSource is null || !LTrace.LTraceCheck(LTraceKind.LTraceUi))
        {
            qViewerHostStamp++;
            return;
        }

        QViewerHostFacts lFacts = qViewerFactsSource();
        string lSurface = lFacts.QViewerFactsState is null
            ? "none"
            : $"{lFacts.QViewerFactsState} {lFacts.QViewerFactsWidth:0}x{lFacts.QViewerFactsHeight:0}";
        string lOverlay = lFacts.QViewerFactsOverlay is not { } lTransparent
            ? "none"
            : $"present, AllowsTransparency {lTransparent} (software-composited layer over the video)";
        LTrace.LTraceRecord(
            LTraceKind.LTraceUi,
            $"Viewer host [{++qViewerHostStamp}] {lStage}",
            $"panel visible {lFacts.QViewerFactsPanel}, host visible {lFacts.QViewerFactsHost}, "
            + $"command {(qViewer.QViewerCommandActive ? "on" : "off")}\n"
            + $"surface {lSurface}, "
            + $"handle {(lFacts.QViewerFactsWindow == nint.Zero ? "none" : "set")}, "
            + $"disposed {lFacts.QViewerFactsDisposed}\n"
            + $"overlay {lOverlay}\n"
            + $"player {(QPlayer.QPlayerReady ? "ready" : "none")}");
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
        if (!lLoadClosed && string.IsNullOrWhiteSpace(qViewer.QViewerSourcePath) && qViewer.QViewerMediaInfo is null)
        {
            return false;
        }

        string lClosedPath = qViewer.QViewerSourcePath ?? string.Empty;
        qViewer.QViewerMediaClose();
        QViewerHostShow(false);
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
        if (qViewer.QViewerIntent is null)
        {
            return false;
        }

        qViewer.QViewerIntentSet(null);
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

    public Task QViewerLoadStart(QViewerIntent lRequest)
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

        string lSourcePath = lRequest.QViewerIntentPath;
        string lLoadPath = LMediaLoader.LMediaPathResolve(lSourcePath) ?? lSourcePath;
        qViewer.QViewerIntentSet(lRequest with { QViewerIntentPath = lLoadPath });
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
        if (qViewer.QViewerUnloaded
            || qViewer.QViewerIntent is not { } lPending
            || !string.Equals(lOutcome.LMediaLoadPath, lPending.QViewerIntentPath, StringComparison.OrdinalIgnoreCase))
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
            string lAudioOnlyError = LLocalization.LLocalizationTextRead("Viewer.Error.AudioOnlyTab");
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

            QPlayer.QPlayerVolumeSet(qViewer.QViewerVolume);
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

        QViewerHostShow(QPlayer.QPlayerReady);
        QPlayer.QPlayerEndSet(lCargo.LCargoMediaInfo?.LMediaVideoEnd);
        QViewerIntent? lRequest = QViewerCargoApply(lCargo);
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
        QViewerIntentApply(lRequest);
    }

    public QViewerIntent? QViewerCargoApply(LCargo lCargo)
    {
        qViewer.QViewerMediaCommit(lCargo, QViewerCropPersistent);
        if (!QViewerCropPersistent)
        {
            qViewer.QCrop.QCropHide();
        }

        QViewerIntent? lRequest = qViewer.QViewerIntent;
        qViewer.QViewerIntentSet(null);
        qViewer.QViewerMediaRaise(lCargo);
        return lRequest;
    }

    public void QViewerIntentApply(QViewerIntent? lRequest)
    {
        TimeSpan lPosition = lRequest?.QViewerIntentPosition ?? TimeSpan.Zero;
        if (lPosition > TimeSpan.Zero)
        {
            qViewer.QViewerPlayback.QViewerAccurateSeek(lPosition);
        }

        bool lPlaying = lRequest?.QViewerIntentPlaying ?? LPreference.LPreferenceStateCurrent.LPreferenceAutoplay;
        qViewer.QViewerResumeSet(false);
        if (!lPlaying)
        {
            QPlayer.QPlayerPause();
            qViewer.QViewerPlaybackUpdate(false, lPosition);
            return;
        }

        QPlayer.QPlayerPlay();
        qViewer.QViewerPlaybackUpdate(true, lPosition);
        qViewer.QViewerPlayback.QViewerClockRun(true);
    }
}
