using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

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

public sealed class LViewer
{
    private int lViewerLoadSerial;
    private int lViewerHostStamp;
    private bool lViewerHostBuilt;
    private bool lViewerMpvActive;
    private bool lViewerEngineSubscribed;
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
    private bool lViewerDragActive;
    private readonly List<string> lViewerSeekTrace = [];
    private int lViewerTraceCount;
    private TimeSpan lViewerTraceFinal;

    public event Action<LCargo>? LViewerMediaChange;
    public event Action<bool>? LViewerPlayingChange;
    public event Action? LViewerEngineChange;
    public event Action? LViewerPreviewChange;
    public event Action<bool>? LViewerBypassChange;
    public event Action<bool, LNeutralTarget>? LViewerToolChange;
    public event Action<double>? LViewerVolumeChange;

    public int LViewerLoadSerial => lViewerLoadSerial;

    public bool LViewerHostBuilt => lViewerHostBuilt;

    public bool LViewerMpvActive => lViewerMpvActive;

    public bool LViewerEngineSubscribed => lViewerEngineSubscribed;

    public LPreviewEngine LViewerEngine => lViewerEngine;

    public bool LViewerCommandActive => lViewerCommandActive;

    public bool LViewerUnloaded => lViewerUnloaded;

    public bool LViewerEndReached => lViewerEndReached;

    public bool LViewerResumeInactive => lViewerResumeInactive;

    public bool LViewerAudioAllowed => lViewerAudioAllowed;

    public bool LViewerAudioEligible => lViewerAudioEligible;

    public bool LViewerEditEligible => lViewerEditEligible;

    public bool LViewerColorPreview => lViewerColorPreview;

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

    public int LViewerSerialChange() => ++lViewerLoadSerial;

    public int LViewerStampChange() => ++lViewerHostStamp;

    public bool LViewerSerialCheck(int lSerial) =>
        !lViewerUnloaded && lSerial == lViewerLoadSerial && lViewerCommandActive;

    public void LViewerHostSet(bool lHostBuilt) => lViewerHostBuilt = lHostBuilt;

    public void LViewerMpvSet(bool lMpvActive) => lViewerMpvActive = lMpvActive;

    public void LViewerSubscribedSet(bool lSubscribed) => lViewerEngineSubscribed = lSubscribed;

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

    public bool LViewerSourceMatch(string lRequestPath) =>
        string.Equals(lViewerRequestPath, lRequestPath, StringComparison.OrdinalIgnoreCase)
        && (lViewerMediaInfo is not null || lViewerIntent is not null);

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

    public void LViewerBypassSet(bool lBypass)
    {
        if (lViewerBypass == lBypass)
        {
            return;
        }

        lViewerBypass = lBypass;
        LViewerBypassChange?.Invoke(lBypass);
    }

    public void LViewerVolumeSet(double lVolume)
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

    public void LViewerDragSet(bool lDragging)
    {
        if (lDragging)
        {
            if (!lViewerDragActive)
            {
                lViewerSeekTrace.Clear();
                lViewerTraceCount = 0;
            }

            lViewerDragActive = true;
            return;
        }

        lViewerDragActive = false;
        if (lViewerTraceCount == 0)
        {
            return;
        }

        string lSummary = lViewerTraceCount == 1
            ? $"Seek accurate to {lViewerTraceFinal:hh\\:mm\\:ss\\.fff}"
            : $"Seek accurate while dragging to {lViewerTraceFinal:hh\\:mm\\:ss\\.fff} ({lViewerTraceCount} requests)";
        LTrace.LTraceRecord(LTraceKind.LTraceUi, lSummary, string.Join(Environment.NewLine, lViewerSeekTrace));
        lViewerSeekTrace.Clear();
        lViewerTraceCount = 0;
    }

    public void LViewerSeekRecord(TimeSpan lPosition, string lDetail)
    {
        string lSummary = $"Seek accurate to {lPosition:hh\\:mm\\:ss\\.fff}";
        if (!lViewerDragActive)
        {
            LTrace.LTraceRecord(LTraceKind.LTraceUi, lSummary, lDetail);
            return;
        }

        if (!LTrace.LTraceCheck(LTraceKind.LTraceUi))
        {
            return;
        }

        string lTime = DateTimeOffset.Now.ToString(
            "HH:mm:ss.fff",
            System.Globalization.CultureInfo.InvariantCulture);
        lViewerSeekTrace.Add($"{lTime}  {lSummary}");
        lViewerSeekTrace.Add($"{new string(' ', 14)}{lDetail}");
        lViewerTraceCount++;
        lViewerTraceFinal = lPosition;
    }

    public static void LViewerMediaRecord(LCargo lCargo, bool lPlayerReady)
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

    public static void LViewerPlayerAttach(Action<object, LPreviewApplication> lPlayerApply) =>
        LPreview.LPreviewApplySeam = lPlayerApply;
}
