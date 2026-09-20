using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class LViewerPlayback
{
    private readonly LViewer lViewer;
    private bool lViewerDragActive;
    private readonly List<string> lViewerSeekTrace = [];
    private int lViewerTraceCount;
    private TimeSpan lViewerTraceFinal;

    public LViewerPlayback(LViewer lOwner)
    {
        lViewer = lOwner;
    }

    public event Action<TimeSpan>? LViewerClockTick;
    public event Action? LViewerClockStart;
    public event Action? LViewerClockStop;
    public event Action? LViewerLoupePlay;
    public event Action? LViewerLoupePause;
    public event Action<TimeSpan>? LViewerLoupeSeek;
    public event Action<double>? LViewerLoupeVolume;

    private LPlayer LPlayer => lViewer.LPlayer;

    public void LViewerVolumeSet(double lVolume)
    {
        if (lViewer.LViewerLoupeActive)
        {
            lViewer.LViewerVolumeApply(lVolume);
            LViewerLoupeVolume?.Invoke(lViewer.LViewerVolume);
            return;
        }

        if (!lViewer.LViewerCommandActive)
        {
            return;
        }

        lViewer.LViewerVolumeApply(lVolume);
        if (!LPlayer.LPlayerReady)
        {
            return;
        }

        LPlayer.LPlayerVolumeSet(lViewer.LViewerVolume);
    }

    public void LViewerVolumeAdjust(double lDelta) => LViewerVolumeSet(lViewer.LViewerVolume + lDelta);

    public void LViewerClockRun(bool lRunning)
    {
        if (lRunning)
        {
            LViewerClockStart?.Invoke();
            return;
        }

        LViewerClockStop?.Invoke();
    }

    public void LViewerLoupeSync(TimeSpan lPosition, bool lPlaying)
    {
        lViewer.LViewerPlaybackUpdate(lPlaying, lPosition);
        LViewerClockTick?.Invoke(lPosition);
    }

    public void LViewerPlay()
    {
        if (lViewer.LViewerLoupeActive)
        {
            LViewerLoupePlay?.Invoke();
            return;
        }

        if (!lViewer.LViewerCommandActive)
        {
            return;
        }

        if (lViewer.LViewerIntent is { } lPending)
        {
            lViewer.LViewerIntentSet(lPending with { LViewerIntentPlaying = true });
            return;
        }

        if (!LPlayer.LPlayerReady || lViewer.LViewerPlaying)
        {
            return;
        }

        if (lViewer.LViewerEndReached)
        {
            lViewer.LViewerEndSet(false);
            LPlayer.LPlayerSeek(TimeSpan.Zero);
        }

        lViewer.LViewerResumeSet(false);
        LPlayer.LPlayerPlay();
        lViewer.LViewerPlaybackUpdate(true, LPlayer.LPlayerTimeRead());
        LViewerClockRun(true);
    }

    public void LViewerPause()
    {
        if (lViewer.LViewerLoupeActive)
        {
            LViewerLoupePause?.Invoke();
            return;
        }

        if (!lViewer.LViewerCommandActive)
        {
            return;
        }

        if (lViewer.LViewerIntent is { } lPending)
        {
            lViewer.LViewerIntentSet(lPending with { LViewerIntentPlaying = false });
            return;
        }

        if (!LPlayer.LPlayerReady)
        {
            return;
        }

        lViewer.LViewerResumeSet(false);
        LPlayer.LPlayerPause();
        lViewer.LViewerPlaybackUpdate(false, LPlayer.LPlayerTimeRead());
        LViewerClockRun(false);
    }

    public void LViewerSeek(TimeSpan lPosition)
    {
        if (lViewer.LViewerLoupeActive)
        {
            LViewerLoupeSeek?.Invoke(lPosition);
            LViewerLoupeSync(lPosition, lViewer.LViewerPlaying);
            return;
        }

        if (!lViewer.LViewerCommandActive)
        {
            return;
        }

        if (lViewer.LViewerIntent is { } lPending)
        {
            lViewer.LViewerIntentSet(lPending with { LViewerIntentPosition = lPosition });
            lViewer.LViewerPlaybackUpdate(null, lPosition);
            return;
        }

        if (!LPlayer.LPlayerReady)
        {
            return;
        }

        lViewer.LViewerEndSet(false);
        try
        {
            LViewerAccurateSeek(lPosition);
        }
        catch (Exception lSeekException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Preview seek to {lPosition:hh\\:mm\\:ss\\.fff} was rejected: {lSeekException.Message}");
            return;
        }

        lViewer.LViewerPlaybackUpdate(null, lPosition);
    }

    public void LViewerAccurateSeek(TimeSpan lPosition)
    {
        if (lViewer.LViewerMpvActive)
        {
            LViewerSeekRecord(lPosition, "mpv engine seeks directly");
            LPlayer.LPlayerSeek(lPosition);
            return;
        }

        bool lWasRunning = LPlayer.LPlayerAccurateSet();
        LViewerSeekRecord(
            lPosition,
            lWasRunning
                ? "a seek was still running; queued for Flyleaf to conflate"
                : "no seek was in flight");
        LPlayer.LPlayerSeek(lPosition);
    }

    public void LViewerSuspend()
    {
        LViewerClockRun(false);
        if (!LPlayer.LPlayerReady)
        {
            lViewer.LViewerResumeSet(false);
            return;
        }

        lViewer.LViewerResumeSet(lViewer.LViewerPlaying);
        if (!lViewer.LViewerResumeInactive)
        {
            return;
        }

        LPlayer.LPlayerPause();
    }

    public void LViewerResume()
    {
        if (!lViewer.LViewerResumeInactive || !LPlayer.LPlayerReady)
        {
            lViewer.LViewerResumeSet(false);
            if (lViewer.LViewerPlaying)
            {
                LViewerClockRun(true);
            }

            return;
        }

        lViewer.LViewerResumeSet(false);
        LPlayer.LPlayerPlay();
        lViewer.LViewerPlaybackUpdate(true, LPlayer.LPlayerTimeRead());
        LViewerClockRun(true);
    }

    public void LViewerTick()
    {
        if (!lViewer.LViewerCommandActive || !LPlayer.LPlayerReady)
        {
            return;
        }

        if (lViewer.LViewerPlaying && LPlayer.LPlayerEndedRead())
        {
            LViewerEndStop();
            return;
        }

        TimeSpan lPosition = LPlayer.LPlayerTimeRead();
        lViewer.LViewerPlaybackUpdate(null, lPosition);
        LViewerClockTick?.Invoke(lPosition);
    }

    private void LViewerEndStop()
    {
        lViewer.LViewerResumeSet(false);
        lViewer.LViewerEndSet(true);
        LViewerClockRun(false);
        LPlayer.LPlayerPause();
        lViewer.LViewerPlaybackUpdate(false, null);
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
}
