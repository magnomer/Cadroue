using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public enum LViewerTool
{
    LViewerToolNone,
    LViewerToolCrop,
    LViewerToolNeutral
}

public sealed record LViewerIntent(
    string LViewerIntentPath,
    TimeSpan LViewerIntentPosition,
    bool? LViewerIntentPlaying);

public sealed record LViewerSwitch(string LViewerSwitchText, string LViewerSwitchTip, bool LViewerSwitchEnabled);

public sealed class LViewer
{
    private int lViewerLoadSerial;
    private bool lViewerMpvActive;
    private bool lViewerHostVisible;
    private bool lViewerLoupeActive;
    private LPreviewEngine lViewerEngine = LPreviewEngine.LPreviewEngineFlyleaf;
    private bool lViewerCommandActive;
    private bool lViewerUnloaded;
    private bool lViewerEndReached;
    private bool lViewerResumeInactive;
    private bool lViewerAudioAllowed;
    private bool lViewerAudioEligible;
    private bool lViewerEditEligible;
    private bool lViewerColorPreview;
    private bool lViewerBypass;
    private string lViewerAudioFilter = string.Empty;
    private LViewerIntent? lViewerIntent;
    private string? lViewerRequestPath;
    private string? lViewerSourcePath;
    private LMediaInfo? lViewerMediaInfo;
    private double lViewerVolume = LPreference.LPreferenceStateCurrent.LPreferenceVolume;
    private LPreviewState lViewerPreview = LPreviewState.LPreviewDefaultCreate();
    private LViewerTool lViewerTool;
    private LNeutralTarget lViewerNeutralTarget;
    private int lViewerNeutralSerial;
    private bool lViewerNeutralPlaying;

    public LViewer()
    {
        LViewerPlayback = new LViewerPlayback(this);
        LViewerMedia = new LViewerMedia(this);
        LViewerSource = new LViewerSource(this);
        LViewerRenderer = new LViewerRenderer(this);
        LViewerMpv = new LViewerMpv(this);
    }

    public LPlayer LPlayer { get; } = new();

    public LViewerPlayback LViewerPlayback { get; }

    public LViewerMedia LViewerMedia { get; }

    public LViewerSource LViewerSource { get; }

    public LViewerRenderer LViewerRenderer { get; }

    public LViewerMpv LViewerMpv { get; }

    public LCrop LCrop { get; } = new();

    public event Action<LCargo>? LViewerMediaChange;
    public event Action<bool>? LViewerPlayingChange;
    public event Action? LViewerEngineChange;
    public event Action? LViewerPreviewChange;
    public event Action<bool>? LViewerBypassChange;
    public event Action<bool, LNeutralTarget>? LViewerToolChange;
    public event Action<double>? LViewerVolumeChange;
    public event Action<string>? LViewerOpenRequest;

    public int LViewerLoadSerial => lViewerLoadSerial;

    public bool LViewerMpvActive => lViewerMpvActive;

    public bool LViewerHostVisible => lViewerHostVisible;

    public bool LViewerMpvShown => lViewerHostVisible && lViewerMpvActive;

    public bool LViewerFlyleafShown => lViewerHostVisible && !lViewerMpvActive;

    public bool LViewerAudioShown => lViewerHostVisible && lViewerAudioEligible;

    public bool LViewerAudioCapable => lViewerEngine == LPreviewEngine.LPreviewEngineMpv;

    public bool LViewerLoupeActive => lViewerLoupeActive;

    public LPreviewEngine LViewerEngine => lViewerEngine;

    public bool LViewerCommandActive => lViewerCommandActive;

    public bool LViewerUnloaded => lViewerUnloaded;

    public bool LViewerEndReached => lViewerEndReached;

    public bool LViewerResumeInactive => lViewerResumeInactive;

    public bool LViewerAudioAllowed => lViewerAudioAllowed;

    public bool LViewerAudioEligible => lViewerAudioEligible;

    public bool LViewerEditEligible => lViewerEditEligible;

    public bool LViewerColorPreview => lViewerColorPreview;

    public bool LViewerProcessorForced => lViewerColorPreview && LFlyleaf.LFlyleafActive;

    public bool LViewerBypass => lViewerBypass;

    public LViewerIntent? LViewerIntent => lViewerIntent;

    public string? LViewerSourcePath => lViewerSourcePath;

    public LMediaInfo? LViewerMediaInfo => lViewerMediaInfo;

    public double LViewerVolume => lViewerVolume;

    public LPreviewState LViewerPreview => lViewerPreview;

    public LViewerTool LViewerTool => lViewerTool;

    public LNeutralTarget LViewerNeutralTarget => lViewerNeutralTarget;

    public int LViewerNeutralSerial => lViewerNeutralSerial;

    public bool LViewerNeutralPlaying => lViewerNeutralPlaying;

    public bool LViewerPlaying => lViewerPreview.LPlaybackState.LPlaybackStatePlaying;

    public TimeSpan LViewerPosition => lViewerPreview.LPlaybackState.LPlaybackPosition;

    public TimeSpan LViewerDuration => lViewerMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero;

    public bool LViewerVideoPresent => lViewerMediaInfo is { LMediaVideoPresent: true };

    public LViewerSwitch LViewerSwitchRead() => new(
        LLocalization.LLocalizationTextRead(lViewerBypass ? "Viewer.Audio.Original" : "Viewer.Audio.Filtered"),
        LLocalization.LLocalizationTextRead(
            LViewerAudioCapable ? "Viewer.Audio.SwitchTooltip" : "Viewer.Audio.MpvRequired"),
        LViewerAudioCapable);

    public int LViewerSerialChange() => ++lViewerLoadSerial;

    public bool LViewerSerialCheck(int lSerial) =>
        !lViewerUnloaded && lSerial == lViewerLoadSerial && lViewerCommandActive;

    public void LViewerMpvSet(bool lMpvActive) => lViewerMpvActive = lMpvActive;

    public void LViewerHostSet(bool lHostVisible) => lViewerHostVisible = lHostVisible;

    public void LViewerLoupeSet(bool lLoupeActive) => lViewerLoupeActive = lLoupeActive;

    public void LViewerCommandSet(bool lCommandActive) => lViewerCommandActive = lCommandActive;

    public void LViewerUnloadSet() => lViewerUnloaded = true;

    public void LViewerEndSet(bool lEndReached) => lViewerEndReached = lEndReached;

    public void LViewerResumeSet(bool lResumeInactive) => lViewerResumeInactive = lResumeInactive;

    public void LViewerAllowSet(bool lAudioAllowed) => lViewerAudioAllowed = lAudioAllowed;

    public void LViewerEligibleSet(bool lAudioEligible, bool lEditEligible, bool lColorPreview)
    {
        lViewerAudioEligible = lAudioEligible;
        lViewerEditEligible = lEditEligible;
        lViewerColorPreview = lColorPreview;
    }

    public void LViewerIntentSet(LViewerIntent? lIntent) => lViewerIntent = lIntent;

    public void LViewerRequestSet(string lRequestPath) => lViewerRequestPath = lRequestPath;

    public void LViewerPathHandle(string? lPath)
    {
        if (string.IsNullOrWhiteSpace(lPath) || LViewerSourceMatch(lPath))
        {
            return;
        }

        LViewerOpenRequest?.Invoke(lPath);
    }

    public bool LViewerSourceMatch(string lRequestPath) =>
        string.Equals(lViewerRequestPath, lRequestPath, StringComparison.OrdinalIgnoreCase)
        && (lViewerMediaInfo is not null || lViewerIntent is not null);

    public bool LViewerSurfaceMatch(nint lForeground, nint lSurface, nint lOverlay) =>
        lForeground != nint.Zero && (lForeground == lSurface || lForeground == lOverlay);

    public void LViewerEngineSet(LPreviewEngine lEngine)
    {
        if (lViewerEngine == lEngine)
        {
            return;
        }

        lViewerEngine = lEngine;
        LViewerEngineChange?.Invoke();
    }

    public void LViewerFilterSet(string? lFilter) => lViewerAudioFilter = lFilter ?? string.Empty;

    public string LViewerAudioResolve() => lViewerBypass ? string.Empty : lViewerAudioFilter;

    public void LViewerGraphSet(string lGraph)
    {
        LViewerFilterSet(lGraph);
        LViewerAudioApply();
        LViewerPreviewRaise();
    }

    public void LViewerBypassSet(bool lBypass)
    {
        if (lViewerBypass == lBypass)
        {
            return;
        }

        lViewerBypass = lBypass;
        LViewerBypassChange?.Invoke(lBypass);
        LViewerAudioApply();
        LViewerPreviewRaise();
    }

    public void LViewerBypassToggle() => LViewerBypassSet(!lViewerBypass);

    public void LViewerAudioApply()
    {
        if (!lViewerMpvActive || !LPlayer.LPlayerReady)
        {
            return;
        }

        LPlayer.LPlayerAudioApply(LViewerAudioResolve());
    }

    public void LViewerVolumeApply(double lVolume)
    {
        lViewerVolume = LPreferenceState.LPreferenceVolumeClamp(lVolume);
        if (LPreference.LPreferenceStateCurrent.LPreferenceVolumeUnified)
        {
            LPreference.LPreferenceVolumeSet(lViewerVolume);
        }

        LViewerVolumeChange?.Invoke(lViewerVolume);
    }

    public void LViewerPreviewSet(LPreviewState lPreview) => lViewerPreview = lPreview;

    public void LViewerPreviewRaise() => LViewerPreviewChange?.Invoke();

    public LPreviewState LViewerRenderRead() =>
        LCrop.LCropActive
            ? lViewerPreview
            : lViewerPreview
                .LRotateFlipChange(LRotateFlip.LRotateDefaultCreate())
                .LCropboxChange(null);

    public void LViewerFilterUpdate()
    {
        if (!lViewerMpvActive || !LPlayer.LPlayerReady)
        {
            return;
        }

        string lFilter = LPreview.LPreviewFilterResolve(LViewerRenderRead());
        bool lChanged = lFilter != LPlayer.LPlayerFilterApplied;
        if (!LPlayer.LPlayerFilterApply(lFilter) || !lChanged)
        {
            return;
        }

        if (LViewerPlaying)
        {
            return;
        }

        try
        {
            LPlayer.LPlayerUpdate();
        }
        catch (Exception lRefreshException)
        {
            LTraceLog.LTraceErrorRecord($"mpv rejected paused preview refresh: {lRefreshException.Message}");
        }
    }

    public void LViewerPreviewApply()
    {
        if (lViewerMpvActive)
        {
            LViewerFilterUpdate();
            LViewerPreviewRaise();
            return;
        }

        LPlayer.LPlayerPreviewApply(LViewerRenderRead(), "preview color/geometry");
        LPlayer.LPlayerFactsRecord("Preview color applied");
        LViewerPreviewRaise();
    }

    public void LViewerRotateSet(LRotateFlip lRotateFlip)
    {
        lViewerPreview = lViewerPreview.LRotateFlipChange(lRotateFlip);
        LTraceLog.LTraceInfoRecord(
            $"Viewer rotate/flip set: rotate {lRotateFlip.LRotateKind}, "
            + $"H {lRotateFlip.LRotateFlipHorizontal}, V {lRotateFlip.LRotateFlipVertical}, "
            + $"player {(LPlayer.LPlayerReady ? "ready" : "none")}, overlay remapped");
        LViewerPreviewApply();
    }

    public void LViewerColorSet(LColor lColor)
    {
        lViewerPreview = lViewerPreview.LColorChange(lColor);
        LViewerPreviewApply();
    }

    public void LViewerMediaRaise(LCargo lCargo)
    {
        Delegate[] lHandlers = LViewerMediaChange?.GetInvocationList() ?? Array.Empty<Delegate>();
        foreach (Action<LCargo> lHandler in lHandlers.Cast<Action<LCargo>>())
        {
            try
            {
                lHandler(lCargo);
            }
            catch (Exception lException)
            {
                LTraceLog.LTraceErrorRecord("Viewer media notice handler failed", lException);
            }
        }
    }

    public void LViewerPlaybackUpdate(bool? lPlaying, TimeSpan? lPosition)
    {
        LPlaybackState lState = lViewerPreview.LPlaybackState;
        bool lPlayingNow = lPlaying ?? lState.LPlaybackStatePlaying;
        lViewerPreview = lViewerPreview.LPlaybackStateChange(
            new LPlaybackState(lPlayingNow, lPosition ?? lState.LPlaybackPosition));
        if (lPlayingNow != lState.LPlaybackStatePlaying)
        {
            LViewerPlayingChange?.Invoke(lPlayingNow);
        }
    }

    public void LViewerMediaCommit(LCargo lCargo, bool lCropPersistent)
    {
        lViewerMediaInfo = lCargo.LCargoMediaInfo;
        lViewerSourcePath = lCargo.LCargoSourcePath;
        lViewerPreview = lViewerPreview.LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
        lViewerEndReached = false;
        if (!lCropPersistent)
        {
            lViewerPreview = lViewerPreview.LCropboxChange(null);
        }
    }

    public void LViewerMediaClose()
    {
        lViewerLoadSerial++;
        lViewerIntent = null;
        lViewerRequestPath = null;
        lViewerSourcePath = null;
        lViewerMediaInfo = null;
        lViewerPreview = lViewerPreview
            .LCropboxChange(null)
            .LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
    }

    public TimeSpan LViewerTimeRead() => LPlayer.LPlayerReady ? LPlayer.LPlayerTimeRead() : LViewerPosition;

    public async void LViewerFrameRead(Action<LMediaFrame?> lFrameApply)
    {
        if (lViewerMediaInfo is not { LMediaVideoPresent: true } lMediaInfo)
        {
            lFrameApply(null);
            return;
        }

        int lWidth = lMediaInfo.LMediaVideoWidth;
        int lHeight = lMediaInfo.LMediaVideoHeight;
        string? lPath = lViewerSourcePath;
        if (lWidth <= 0 || lHeight <= 0 || string.IsNullOrWhiteSpace(lPath))
        {
            lFrameApply(null);
            return;
        }

        TimeSpan lTime = LViewerTimeRead();
        int lClaim = lViewerLoadSerial;
        LMediaFrame? lFrame = await LMedia.LMediaFrameStart(lPath, lTime, lWidth, lHeight);
        if (lViewerUnloaded || lClaim != lViewerLoadSerial)
        {
            return;
        }

        lFrameApply(lFrame);
    }

    public void LViewerToolSet(LViewerTool lTool) => lViewerTool = lTool;

    public bool LViewerNeutralSet(LNeutralTarget lTarget)
    {
        if (lViewerTool == LViewerTool.LViewerToolNeutral)
        {
            lViewerNeutralTarget = lTarget;
            return false;
        }

        lViewerNeutralSerial++;
        lViewerNeutralTarget = lTarget;
        lViewerNeutralPlaying = LViewerPlaying;
        lViewerTool = LViewerTool.LViewerToolNeutral;
        return true;
    }

    public void LViewerNeutralRaise(bool lArmed) => LViewerToolChange?.Invoke(lArmed, lViewerNeutralTarget);

    public bool LViewerNeutralCancel()
    {
        if (lViewerTool != LViewerTool.LViewerToolNeutral)
        {
            return false;
        }

        lViewerNeutralSerial++;
        return true;
    }

    public bool LViewerNeutralReset()
    {
        lViewerTool = LViewerTool.LViewerToolNone;
        bool lResume = lViewerNeutralPlaying;
        lViewerNeutralPlaying = false;
        return lResume;
    }
}
