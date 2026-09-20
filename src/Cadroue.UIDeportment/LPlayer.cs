using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record LPlayerSeam(
    Action<string> LPlayerOpenSeam,
    Action LPlayerPlaySeam,
    Action LPlayerPauseSeam,
    Action LPlayerStopSeam,
    Action<TimeSpan> LPlayerSeekSeam,
    Action<double> LPlayerVolumeSeam,
    Action<string> LPlayerFilterSeam,
    Action<string> LPlayerAudioSeam,
    Action<LPreviewApplication> LPlayerPreviewSeam,
    Action LPlayerUpdateSeam,
    Func<TimeSpan> LPlayerTimeSeam,
    Func<bool> LPlayerEndSeam,
    Action<string, double?> LPlayerFactsSeam,
    Action LPlayerDisposeSeam);

public sealed class LPlayer
{
    private LPlayerSeam? lPlayerSeam;
    private volatile bool lPlayerAccurateActive;
    private volatile bool lPlayerRendererPending;
    private string lPlayerFilterApplied = string.Empty;
    private string? lPlayerAudioApplied;
    private TimeSpan? lPlayerVideoEnd;

    public bool LPlayerReady => lPlayerSeam is not null;

    public bool LPlayerAccurateActive => lPlayerAccurateActive;

    public bool LPlayerRendererPending => lPlayerRendererPending;

    public string LPlayerFilterApplied => lPlayerFilterApplied;

    public string? LPlayerAudioApplied => lPlayerAudioApplied;

    public TimeSpan? LPlayerVideoEnd => lPlayerVideoEnd;

    public void LPlayerEngineSet(LPlayerSeam? lSeam)
    {
        if (ReferenceEquals(lPlayerSeam, lSeam))
        {
            return;
        }

        lPlayerSeam?.LPlayerDisposeSeam();
        lPlayerSeam = lSeam;
        LPlayerAppliedReset();
    }

    public void LPlayerDispose() => LPlayerEngineSet(null);

    public void LPlayerEndSet(TimeSpan? lVideoEnd) => lPlayerVideoEnd = lVideoEnd;

    public Task LPlayerOpenStart(string lSourcePath)
    {
        LPlayerSeam? lSeam = lPlayerSeam;
        return Task.Run(() => lSeam?.LPlayerOpenSeam(lSourcePath));
    }

    public void LPlayerPlay() => lPlayerSeam?.LPlayerPlaySeam();

    public void LPlayerPause() => lPlayerSeam?.LPlayerPauseSeam();

    public void LPlayerStop() => lPlayerSeam?.LPlayerStopSeam();

    public void LPlayerSeek(TimeSpan lPosition) =>
        lPlayerSeam?.LPlayerSeekSeam(LPreview.LPreviewPositionResolve(lPosition, lPlayerVideoEnd));

    public void LPlayerVolumeSet(double lVolume) => lPlayerSeam?.LPlayerVolumeSeam(lVolume);

    public void LPlayerUpdate() => lPlayerSeam?.LPlayerUpdateSeam();

    public TimeSpan LPlayerTimeRead() => lPlayerSeam?.LPlayerTimeSeam() ?? TimeSpan.Zero;

    public bool LPlayerEndedRead() => lPlayerSeam?.LPlayerEndSeam() ?? false;

    public void LPlayerFactsRecord(string lReason, double? lMilliseconds = null) =>
        lPlayerSeam?.LPlayerFactsSeam(lReason, lMilliseconds);

    public void LPlayerPreviewApply(LPreviewState lState, string lReason)
    {
        LPreviewApplication lApplication = LPreview.LPreviewApplicationResolve(lState, lReason);
        lPlayerSeam?.LPlayerPreviewSeam(lApplication with
        {
            LPreviewContrast = LFlyleaf.LFlyleafActive ? lApplication.LPreviewContrast : 0
        });
    }

    public bool LPlayerAccurateSet()
    {
        bool lWasRunning = lPlayerAccurateActive;
        lPlayerAccurateActive = true;
        return lWasRunning;
    }

    public void LPlayerAccurateReset() => lPlayerAccurateActive = false;

    public void LPlayerRendererSet(bool lPending) => lPlayerRendererPending = lPending;

    public bool LPlayerSeekCommit(int lSeekMilliseconds)
    {
        lPlayerAccurateActive = false;
        if (!lPlayerRendererPending || lSeekMilliseconds < 0)
        {
            return false;
        }

        lPlayerRendererPending = false;
        return true;
    }

    public void LPlayerSeekHandle(int lSeekMilliseconds)
    {
        if (LPlayerSeekCommit(lSeekMilliseconds))
        {
            LPlayerFactsRecord("Renderer resolved after the first completed seek");
        }
    }

    public bool LPlayerFilterApply(string lFilter)
    {
        if (lFilter == lPlayerFilterApplied)
        {
            return true;
        }

        try
        {
            lPlayerSeam?.LPlayerFilterSeam(lFilter);
            lPlayerFilterApplied = lFilter;
            return true;
        }
        catch (Exception lFilterException)
        {
            LTraceLog.LTraceErrorRecord(
                "mpv rejected the preview filter (likely an LGPL libmpv without the GPL eq filter); "
                + "the queued export is unaffected. "
                + $"Filter '{lFilter}': {lFilterException.Message}");
            LPlayerFilterClear();
            return false;
        }
    }

    private void LPlayerFilterClear()
    {
        try
        {
            lPlayerSeam?.LPlayerFilterSeam(string.Empty);
            lPlayerFilterApplied = string.Empty;
        }
        catch (Exception lClearException)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv rejected stale preview filter cleanup: {lClearException.Message}");
        }
    }

    public void LPlayerAudioApply(string lAudio)
    {
        if (lAudio == lPlayerAudioApplied)
        {
            return;
        }

        try
        {
            lPlayerSeam?.LPlayerAudioSeam(lAudio);
            lPlayerAudioApplied = lAudio;
        }
        catch (Exception lAudioException)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv rejected audio filter '{lAudio}': {lAudioException.Message}");
        }
    }

    public void LPlayerAppliedReset()
    {
        lPlayerFilterApplied = string.Empty;
        lPlayerAudioApplied = null;
    }
}
