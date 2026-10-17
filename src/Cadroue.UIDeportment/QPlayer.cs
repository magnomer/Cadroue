using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record QPlayerSeam(
    Action<string> QPlayerOpenSeam,
    Action QPlayerPlaySeam,
    Action QPlayerPauseSeam,
    Action QPlayerStopSeam,
    Action<TimeSpan> QPlayerSeekSeam,
    Action<double> QPlayerVolumeSeam,
    Action<string>? QPlayerFilterSeam,
    Action<string>? QPlayerAudioSeam,
    Action<LPreviewApplication> QPlayerPreviewSeam,
    Action? QPlayerUpdateSeam,
    Func<TimeSpan> QPlayerTimeSeam,
    Func<bool> QPlayerEndSeam,
    Action<string, double?> QPlayerFactsSeam,
    Action QPlayerDisposeSeam);

public sealed class QPlayer
{
    private QPlayerSeam? qPlayerSeam;
    private volatile bool qPlayerAccurateActive;
    private volatile bool qPlayerRendererPending;
    private string qPlayerFilterApplied = string.Empty;
    private string? qPlayerAudioApplied;
    private TimeSpan? qPlayerVideoEnd;
    private readonly QChronometer qPlayerChronometer = new();

    public bool QPlayerReady => qPlayerSeam is not null;

    public bool QPlayerAccurateActive => qPlayerAccurateActive;

    public bool QPlayerRendererPending => qPlayerRendererPending;

    public string QPlayerFilterApplied => qPlayerFilterApplied;

    public string? QPlayerAudioApplied => qPlayerAudioApplied;

    public TimeSpan? QPlayerVideoEnd => qPlayerVideoEnd;

    public void QPlayerEngineSet(QPlayerSeam? lSeam)
    {
        if (ReferenceEquals(qPlayerSeam, lSeam))
        {
            return;
        }

        qPlayerSeam?.QPlayerDisposeSeam();
        QPlayerSeamSet(lSeam);
    }

    private void QPlayerSeamSet(QPlayerSeam? lSeam)
    {
        qPlayerSeam = lSeam;
        QPlayerAppliedReset();
    }

    public void QPlayerMpvCreate(nint lHostHandle) =>
        QPlayerEngineSet(new QPlayerMpv(lHostHandle).QPlayerSeamRead());

    public void QPlayerDispose() => QPlayerEngineSet(null);

    public void QPlayerEndSet(TimeSpan? lVideoEnd) => qPlayerVideoEnd = lVideoEnd;

    public Task QPlayerOpenStart(string lSourcePath)
    {
        QPlayerSeam? lSeam = qPlayerSeam;
        return Task.Run(() => lSeam?.QPlayerOpenSeam(lSourcePath));
    }

    public void QPlayerPlay() => qPlayerSeam?.QPlayerPlaySeam();

    public void QPlayerPause() => qPlayerSeam?.QPlayerPauseSeam();

    public void QPlayerStop() => qPlayerSeam?.QPlayerStopSeam();

    public void QPlayerSeek(TimeSpan lPosition) =>
        qPlayerSeam?.QPlayerSeekSeam(LPreview.LPreviewPositionResolve(lPosition, qPlayerVideoEnd));

    public void QPlayerVolumeSet(double lVolume) => qPlayerSeam?.QPlayerVolumeSeam(lVolume);

    public void QPlayerUpdate() => qPlayerSeam?.QPlayerUpdateSeam?.Invoke();

    public TimeSpan QPlayerTimeRead() => qPlayerSeam?.QPlayerTimeSeam() ?? TimeSpan.Zero;

    public bool QPlayerEndedRead() => qPlayerSeam?.QPlayerEndSeam() ?? false;

    public void QPlayerFactsRecord(string lReason, double? lMilliseconds = null) =>
        qPlayerSeam?.QPlayerFactsSeam(lReason, lMilliseconds);

    public void QPlayerCreateStart() => qPlayerChronometer.QChronometerStart();

    public double QPlayerCreateRead() => qPlayerChronometer.QChronometerRead();

    public void QPlayerPreviewApply(LPreviewState lState, string lReason)
    {
        LPreviewApplication lApplication = LPreview.LPreviewApplicationResolve(lState, lReason);
        qPlayerSeam?.QPlayerPreviewSeam(QPlayerContrastResolve(lApplication));
    }

    private static LPreviewApplication QPlayerContrastResolve(LPreviewApplication lApplication) =>
        lApplication with
        {
            LPreviewContrast = LFlyleaf.LFlyleafActive ? lApplication.LPreviewContrast : 0
        };

    public bool QPlayerAccurateSet()
    {
        bool lWasRunning = qPlayerAccurateActive;
        qPlayerAccurateActive = true;
        return lWasRunning;
    }

    public void QPlayerAccurateReset() => qPlayerAccurateActive = false;

    public void QPlayerRendererSet(bool lPending) => qPlayerRendererPending = lPending;

    public bool QPlayerSeekCommit(int lSeekMilliseconds)
    {
        QPlayerAccurateReset();
        return QPlayerSeekCheck(lSeekMilliseconds) && QPlayerRendererCommit();
    }

    private static bool QPlayerSeekCheck(int lSeekMilliseconds) => lSeekMilliseconds >= 0;

    private bool QPlayerRendererCommit()
    {
        if (!qPlayerRendererPending)
        {
            return false;
        }

        qPlayerRendererPending = false;
        return true;
    }

    public void QPlayerSeekHandle(int lSeekMilliseconds)
    {
        if (QPlayerSeekCommit(lSeekMilliseconds))
        {
            QPlayerFactsRecord("Renderer resolved after the first completed seek");
        }
    }

    public bool QPlayerFilterApply(string lFilter)
    {
        if (QPlayerFilterMatch(lFilter))
        {
            return true;
        }

        try
        {
            qPlayerSeam?.QPlayerFilterSeam?.Invoke(lFilter);
            QPlayerFilterSet(lFilter);
            return true;
        }
        catch (Exception lFilterException)
        {
            LTraceLog.LTraceErrorRecord(
                "mpv rejected the preview filter (likely an LGPL libmpv without the GPL eq filter); "
                + "the queued export is unaffected. "
                + $"Filter '{lFilter}': {lFilterException.Message}");
            QPlayerFilterClear();
            return false;
        }
    }

    private void QPlayerFilterClear()
    {
        try
        {
            qPlayerSeam?.QPlayerFilterSeam?.Invoke(string.Empty);
            QPlayerFilterSet(string.Empty);
        }
        catch (Exception lClearException)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv rejected stale preview filter cleanup: {lClearException.Message}");
        }
    }

    public void QPlayerAudioApply(string lAudio)
    {
        if (QPlayerAudioMatch(lAudio))
        {
            return;
        }

        try
        {
            qPlayerSeam?.QPlayerAudioSeam?.Invoke(lAudio);
            QPlayerAudioSet(lAudio);
        }
        catch (Exception lAudioException)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv rejected audio filter '{lAudio}': {lAudioException.Message}");
        }
    }

    private bool QPlayerFilterMatch(string lFilter) => lFilter == qPlayerFilterApplied;

    private void QPlayerFilterSet(string lFilter) => qPlayerFilterApplied = lFilter;

    private bool QPlayerAudioMatch(string lAudio) => lAudio == qPlayerAudioApplied;

    private void QPlayerAudioSet(string lAudio) => qPlayerAudioApplied = lAudio;

    public void QPlayerAppliedReset()
    {
        qPlayerFilterApplied = string.Empty;
        qPlayerAudioApplied = null;
    }
}
