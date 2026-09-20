using System.Diagnostics;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed record LViewerHostFacts(
    bool LViewerFactsPanel,
    bool LViewerFactsHost,
    string? LViewerFactsState,
    double? LViewerFactsWidth,
    double? LViewerFactsHeight,
    nint LViewerFactsWindow,
    bool LViewerFactsDisposed,
    bool? LViewerFactsOverlay);

public sealed class LViewerMedia
{
    private readonly LViewer lViewer;
    private readonly LMediaLoad lViewerProbe = new();
    private Func<LViewerHostFacts>? lViewerFactsSource;
    private int lViewerHostStamp;

    public LViewerMedia(LViewer lOwner)
    {
        lViewer = lOwner;
    }

    public event Action? LViewerPlayerCreate;
    public event Action? LViewerCropReset;

    private LPlayer LPlayer => lViewer.LPlayer;

    private bool LViewerCropPersistent => lViewer.LCrop.LCropPersistent;

    private LViewerRenderer LViewerRenderer => lViewer.LViewerRenderer;

    public void LViewerPlayerRaise() => LViewerPlayerCreate?.Invoke();

    public void LViewerFactsAttach(Func<LViewerHostFacts> lFactsSource) => lViewerFactsSource = lFactsSource;

    public void LViewerCommandApply(bool lCommandActive)
    {
        if (lViewer.LViewerUnloaded || lViewer.LViewerCommandActive == lCommandActive)
        {
            return;
        }

        LViewerHostRecord($"command set {(lCommandActive ? "on" : "off")}");
        if (!lCommandActive)
        {
            lViewer.LViewerPlayback.LViewerSuspend();
            lViewer.LViewerCommandSet(false);
            lViewer.LViewerSerialChange();
            return;
        }

        lViewer.LViewerCommandSet(true);
        if (LViewerRenderer.LViewerEngineRestore())
        {
            return;
        }

        lViewer.LViewerPlayback.LViewerResume();
    }

    public void LViewerLoupeAttach()
    {
        lViewer.LViewerLoupeSet(true);
        LViewerHostShow(false);
        LViewerCommandApply(false);
    }

    public void LViewerLoupeDetach(TimeSpan lPosition, bool lPlaying)
    {
        lViewer.LViewerLoupeSet(false);
        lViewer.LViewerResumeSet(lPlaying);
        LViewerCommandApply(true);
        LViewerHostShow(true);
        lViewer.LViewerPlayback.LViewerSeek(lPosition);
        if (lPlaying)
        {
            lViewer.LViewerPlayback.LViewerPlay();
            return;
        }

        lViewer.LViewerPlayback.LViewerPause();
    }

    public void LViewerHostShow(bool lVisible)
    {
        bool lChanged = lVisible != lViewer.LViewerHostVisible;
        lViewer.LViewerHostSet(lVisible);
        LViewerRenderer.LViewerHostRaise();
        if (lChanged && !lViewer.LViewerMpvActive)
        {
            LViewerHostRecord($"host {(lVisible ? "shown" : "hidden")}");
        }
    }

    public void LViewerHostRecord(string lStage)
    {
        if (lViewerFactsSource is null || !LTrace.LTraceCheck(LTraceKind.LTraceUi))
        {
            lViewerHostStamp++;
            return;
        }

        LViewerHostFacts lFacts = lViewerFactsSource();
        string lSurface = lFacts.LViewerFactsState is null
            ? "none"
            : $"{lFacts.LViewerFactsState} {lFacts.LViewerFactsWidth:0}x{lFacts.LViewerFactsHeight:0}";
        string lOverlay = lFacts.LViewerFactsOverlay is not { } lTransparent
            ? "none"
            : $"present, AllowsTransparency {lTransparent} (software-composited layer over the video)";
        LTrace.LTraceRecord(
            LTraceKind.LTraceUi,
            $"Viewer host [{++lViewerHostStamp}] {lStage}",
            $"panel visible {lFacts.LViewerFactsPanel}, host visible {lFacts.LViewerFactsHost}, "
            + $"command {(lViewer.LViewerCommandActive ? "on" : "off")}\n"
            + $"surface {lSurface}, "
            + $"handle {(lFacts.LViewerFactsWindow == nint.Zero ? "none" : "set")}, "
            + $"disposed {lFacts.LViewerFactsDisposed}\n"
            + $"overlay {lOverlay}\n"
            + $"player {(LPlayer.LPlayerReady ? "ready" : "none")}");
    }

    public void LViewerClose()
    {
        if (lViewer.LViewerUnloaded)
        {
            return;
        }

        lViewer.LViewerUnloadSet();
        lViewer.LViewerSerialChange();
        LViewerStageRun("media probe", lViewerProbe.Dispose);
        LViewerStageRun("player", LViewerPlayerStop);
    }

    private static void LViewerStageRun(string lStage, Action lAction)
    {
        try
        {
            lAction();
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Viewer close stage '{lStage}' failed", lException);
        }
    }

    public event Action? LViewerLoupeClose;

    public bool LViewerMediaClose(bool lForce)
    {
        if (lViewer.LViewerUnloaded || (!lForce && !lViewer.LViewerCommandActive))
        {
            return false;
        }

        if (lViewer.LViewerLoupeActive)
        {
            LViewerLoupeClose?.Invoke();
        }

        bool lLoadClosed = lViewerProbe.LMediaLoadClose();
        if (!lLoadClosed && string.IsNullOrWhiteSpace(lViewer.LViewerSourcePath) && lViewer.LViewerMediaInfo is null)
        {
            return false;
        }

        string lClosedPath = lViewer.LViewerSourcePath ?? string.Empty;
        lViewer.LViewerMediaClose();
        LViewerHostShow(false);
        LViewerPlayerStop();
        LViewerCropReset?.Invoke();
        lViewer.LViewerPreviewApply();

        LTraceLog.LTraceInfoRecord(string.IsNullOrWhiteSpace(lClosedPath)
            ? "Media closed"
            : $"Media closed '{System.IO.Path.GetFileName(lClosedPath)}' [{lClosedPath}]");

        lViewer.LViewerMediaRaise(new LCargo(string.Empty, null, false, false, null, null));
        return true;
    }

    public bool LViewerLoadCancel()
    {
        if (lViewer.LViewerIntent is null)
        {
            return false;
        }

        lViewer.LViewerIntentSet(null);
        lViewer.LViewerSerialChange();
        return true;
    }

    public void LViewerPlayerStop()
    {
        lViewer.LViewerPlayback.LViewerClockRun(false);
        LPlayer.LPlayerAccurateReset();
        lViewer.LViewerResumeSet(false);
        lViewer.LViewerPlaybackUpdate(false, null);
        LTraceLog.LTraceInfoRecord(
            $"Viewer host detached: player {(LPlayer.LPlayerReady ? "released" : "none")}");
        LPlayer.LPlayerDispose();
    }

    public Task LViewerLoadStart(LViewerIntent lRequest)
    {
        if (!lViewer.LViewerCommandActive)
        {
            return Task.CompletedTask;
        }

        LViewerRenderer.LViewerEngineUpdate();
        int lSerial = lViewer.LViewerSerialChange();
        lViewer.LViewerPlayback.LViewerClockRun(false);
        lViewer.LViewerResumeSet(false);
        if (LPlayer.LPlayerReady)
        {
            LPlayer.LPlayerPause();
            lViewer.LViewerPlaybackUpdate(false, null);
        }

        if (!lViewer.LViewerSerialCheck(lSerial))
        {
            return Task.CompletedTask;
        }

        string lSourcePath = lRequest.LViewerIntentPath;
        string lLoadPath = LMediaLoad.LMediaPathResolve(lSourcePath) ?? lSourcePath;
        lViewer.LViewerIntentSet(lRequest with { LViewerIntentPath = lLoadPath });
        lViewerProbe.LMediaLoadTail = !lViewer.LViewerMpvActive;
        LTraceLog.LTraceInfoRecord(
            $"Video load start serial={lSerial} '{System.IO.Path.GetFileName(lSourcePath)}'",
            $"engine={lViewer.LViewerEngine}, probing (ffprobe)…");
        return LViewerProbeRun(lSourcePath);
    }

    private async Task LViewerProbeRun(string lSourcePath)
    {
        LMediaLoadOutcome lOutcome = await lViewerProbe.LMediaLoadStart(lSourcePath);
        int lSerial = lViewer.LViewerLoadSerial;
        LTraceLog.LTraceInfoRecord(
            $"Video probe outcome {lOutcome.LMediaLoadKind} '{System.IO.Path.GetFileName(lOutcome.LMediaLoadPath)}'",
            lOutcome.LMediaLoadError);
        if (lViewer.LViewerUnloaded
            || lViewer.LViewerIntent is not { } lPending
            || !string.Equals(lOutcome.LMediaLoadPath, lPending.LViewerIntentPath, StringComparison.OrdinalIgnoreCase))
        {
            LTraceLog.LTraceInfoRecord(
                "Video probe outcome discarded (stale/superseded): "
                + $"serial={lSerial}, unloaded={lViewer.LViewerUnloaded}");
            return;
        }

        if (lOutcome.LMediaLoadKind == LMediaLoadKind.LMediaLoadSuccess)
        {
            await LViewerMediaApply(lOutcome.LMediaLoadPath, lOutcome.LMediaLoadInfo, null, lSerial);
            return;
        }

        if (lOutcome.LMediaLoadKind == LMediaLoadKind.LMediaLoadFailure)
        {
            LViewerCargoCommit(new LCargo(
                lOutcome.LMediaLoadPath,
                null,
                false,
                false,
                lOutcome.LMediaLoadError,
                null));
        }
    }

    private void LViewerCargoCommit(LCargo lCargo)
    {
        if (lViewer.LViewerMpvActive)
        {
            lViewer.LViewerMpv.LViewerMpvCommit(lCargo);
            return;
        }

        LViewerFlyleafCommit(lCargo, false);
    }

    private Task LViewerMediaApply(string lSourcePath, LMediaInfo? lMediaInfo, string? lFfmpegError, int lSerial) =>
        lViewer.LViewerMpvActive
            ? lViewer.LViewerMpv.LViewerMpvApply(lSourcePath, lMediaInfo, lFfmpegError, lSerial)
            : LViewerFlyleafApply(lSourcePath, lMediaInfo, lFfmpegError, lSerial);

    public async Task LViewerFlyleafApply(string lSourcePath, LMediaInfo? lMediaInfo, string? lFfmpegError, int lSerial)
    {
        if (lMediaInfo is { LMediaAudioOnly: true } && !lViewer.LViewerAudioAllowed)
        {
            string lAudioOnlyError = LLocalization.LLocalizationTextRead("Viewer.Error.AudioOnlyTab");
            LViewerFlyleafCommit(new LCargo(lSourcePath, null, false, false, lAudioOnlyError, lAudioOnlyError), false);
            return;
        }

        bool lCreated = !LPlayer.LPlayerReady;
        string? lPreviewError = null;
        var lClock = Stopwatch.StartNew();
        try
        {
            if (lCreated)
            {
                LViewerPlayerCreate?.Invoke();
            }

            LPlayer.LPlayerVolumeSet(lViewer.LViewerVolume);
            double lBeforeOpen = lClock.Elapsed.TotalMilliseconds;
            await LPlayer.LPlayerOpenStart(lSourcePath);
            LPlayer.LPlayerFactsRecord(
                $"Player opened '{System.IO.Path.GetFileName(lSourcePath)}'",
                lClock.Elapsed.TotalMilliseconds - lBeforeOpen);
        }
        catch (Exception lException)
        {
            lPreviewError = lException.Message;
            LPlayer.LPlayerDispose();
        }

        if (!lViewer.LViewerSerialCheck(lSerial))
        {
            if (lCreated)
            {
                LPlayer.LPlayerDispose();
            }

            LTraceLog.LTraceInfoRecord(
                "Player open discarded (stale/superseded): "
                + $"serial got={lSerial} now={lViewer.LViewerLoadSerial}, unloaded={lViewer.LViewerUnloaded}");
            return;
        }

        if (LPlayer.LPlayerReady)
        {
            LPlayer.LPlayerRendererSet(true);
            LPlayer.LPlayerPause();
            LPlayer.LPlayerSeek(TimeSpan.Zero);
        }

        LViewerFlyleafCommit(
            new LCargo(
                lSourcePath, lMediaInfo, lMediaInfo is not null, LPlayer.LPlayerReady, lFfmpegError, lPreviewError),
            !lCreated);
    }

    private void LViewerFlyleafCommit(LCargo lCargo, bool lReused)
    {
        LViewerMediaRecord(lCargo, LPlayer.LPlayerReady);
        LPlayer.LPlayerAccurateReset();
        if (lReused && LPlayer.LPlayerReady)
        {
            LTraceLog.LTraceInfoRecord("Viewer player reused: same player kept on the host, no swap chain rebuild");
        }
        else
        {
            LTraceLog.LTraceInfoRecord(
                $"Viewer player swapped: next {(LPlayer.LPlayerReady ? "ready" : "none")}");
            if (!LPlayer.LPlayerReady)
            {
                LPlayer.LPlayerDispose();
            }
        }

        LViewerHostShow(LPlayer.LPlayerReady);
        LPlayer.LPlayerEndSet(lCargo.LCargoMediaInfo?.LMediaVideoEnd);
        LViewerIntent? lRequest = LViewerCargoApply(lCargo);
        if (!LPlayer.LPlayerReady)
        {
            lViewer.LViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        LRotateFlip lRotate = lViewer.LViewerPreview.LRotateFlip;
        LTraceLog.LTraceInfoRecord(
            $"Viewer preview restored: rotate {lRotate.LRotateKind}, "
            + $"H {lRotate.LRotateFlipHorizontal}, V {lRotate.LRotateFlipVertical}");
        LPlayer.LPlayerPreviewApply(lViewer.LViewerPreview, "preview restored");
        LViewerIntentApply(lRequest);
    }

    public LViewerIntent? LViewerCargoApply(LCargo lCargo)
    {
        lViewer.LViewerMediaCommit(lCargo, LViewerCropPersistent);
        if (!LViewerCropPersistent)
        {
            LViewerCropReset?.Invoke();
        }

        LViewerIntent? lRequest = lViewer.LViewerIntent;
        lViewer.LViewerIntentSet(null);
        lViewer.LViewerMediaRaise(lCargo);
        return lRequest;
    }

    public void LViewerIntentApply(LViewerIntent? lRequest)
    {
        TimeSpan lPosition = lRequest?.LViewerIntentPosition ?? TimeSpan.Zero;
        if (lPosition > TimeSpan.Zero)
        {
            lViewer.LViewerPlayback.LViewerAccurateSeek(lPosition);
        }

        bool lPlaying = lRequest?.LViewerIntentPlaying ?? LPreference.LPreferenceStateCurrent.LPreferenceAutoplay;
        lViewer.LViewerResumeSet(false);
        if (!lPlaying)
        {
            LPlayer.LPlayerPause();
            lViewer.LViewerPlaybackUpdate(false, lPosition);
            return;
        }

        LPlayer.LPlayerPlay();
        lViewer.LViewerPlaybackUpdate(true, lPosition);
        lViewer.LViewerPlayback.LViewerClockRun(true);
    }

    private static void LViewerMediaRecord(LCargo lCargo, bool lPlayerReady)
    {
        string lSourcePath = lCargo.LCargoSourcePath ?? "(no path)";
        string lFileName = System.IO.Path.GetFileName(lSourcePath);
        if (lCargo.LCargoMediaInfo is not LMediaInfo lMediaInfo)
        {
            LTraceLog.LTraceErrorRecord(
                $"Media rejected '{lFileName}': {lCargo.LCargoFfmpegError ?? "unreadable"} [{lSourcePath}]");
            return;
        }

        string lStreams = lMediaInfo.LMediaVideoPresent
            ? $"video {lMediaInfo.LMediaVideoWidth}x{lMediaInfo.LMediaVideoHeight} "
                + $"{lMediaInfo.LMediaVideoCodec} {lMediaInfo.LMediaVideoRate:0.###}fps"
            : "no video";
        if (lMediaInfo.LMediaAudioPresent)
        {
            lStreams += $", audio {lMediaInfo.LMediaAudioCodec} "
                + $"{lMediaInfo.LMediaSampleRate}Hz {lMediaInfo.LMediaAudioChannels}ch";
        }

        LTraceLog.LTraceInfoRecord(
            $"Media opened '{lFileName}': {lMediaInfo.LMediaInfoDuration:hh\\:mm\\:ss\\.fff}, "
            + $"{lStreams} [{lSourcePath}]");

        if (!lPlayerReady)
        {
            LTraceLog.LTraceErrorRecord(
                $"Preview unavailable for '{lFileName}': "
                + $"{lCargo.LCargoPreviewError ?? "the player did not start"}");
        }
    }
}
