using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QViewerPlayback
{
    private readonly QViewer qViewer;
    private bool qViewerDragActive;
    private readonly List<string> qViewerSeekTrace = [];
    private int qViewerTraceCount;
    private TimeSpan qViewerTraceFinal;

    public QViewerPlayback(QViewer lOwner)
    {
        qViewer = lOwner;
    }

    public event Action<TimeSpan>? QViewerClockTicked;
    public event Action? QViewerClockStarted;
    public event Action? QViewerClockStopped;
    public event Action? QViewerLoupePlayed;
    public event Action? QViewerLoupePaused;
    public event Action<TimeSpan>? QViewerLoupeSought;
    public event Action<double>? QViewerLoupeVolume;

    private QPlayer QPlayer => qViewer.QPlayer;

    public void QViewerVolumeSet(double lVolume)
    {
        if (qViewer.QViewerLoupeActive)
        {
            qViewer.QViewerAudio.QViewerVolumeApply(lVolume);
            QViewerLoupeVolume?.Invoke(qViewer.QViewerAudio.QViewerVolume);
            return;
        }

        if (!qViewer.QViewerCommandActive)
        {
            return;
        }

        qViewer.QViewerAudio.QViewerVolumeApply(lVolume);
        if (!QPlayer.QPlayerReady)
        {
            return;
        }

        QPlayer.QPlayerVolumeSet(qViewer.QViewerAudio.QViewerVolume);
    }

    public void QViewerVolumeAdjust(double lDelta) => QViewerVolumeSet(qViewer.QViewerAudio.QViewerVolume + lDelta);

    public void QViewerClockRun(bool lRunning)
    {
        if (lRunning)
        {
            QViewerClockStarted?.Invoke();
            return;
        }

        QViewerClockStopped?.Invoke();
    }

    public void QViewerLoupeSync(TimeSpan lPosition, bool lPlaying)
    {
        qViewer.QViewerPlaybackUpdate(lPlaying, lPosition);
        QViewerClockTicked?.Invoke(lPosition);
    }

    public void QViewerPlay()
    {
        if (qViewer.QViewerLoupeActive)
        {
            QViewerLoupePlayed?.Invoke();
            return;
        }

        if (!qViewer.QViewerCommandActive)
        {
            return;
        }

        qViewer.QViewerWorkspace.CWorkspacePlay(true);
    }

    // The workspace holds no pending load: the player plays now.
    public void QViewerPlayApply()
    {
        if (!QPlayer.QPlayerReady || qViewer.QViewerPlaying)
        {
            return;
        }

        if (qViewer.QViewerEndReached)
        {
            qViewer.QViewerEndSet(false);
            QPlayer.QPlayerSeek(TimeSpan.Zero);
        }

        qViewer.QViewerResumeSet(false);
        QPlayer.QPlayerPlay();
        qViewer.QViewerPlaybackUpdate(true, QPlayer.QPlayerTimeRead());
        QViewerClockRun(true);
    }

    public void QViewerPause()
    {
        if (qViewer.QViewerLoupeActive)
        {
            QViewerLoupePaused?.Invoke();
            return;
        }

        if (!qViewer.QViewerCommandActive)
        {
            return;
        }

        qViewer.QViewerWorkspace.CWorkspacePlay(false);
    }

    // The workspace holds no pending load: the player pauses now.
    public void QViewerPauseApply()
    {
        if (!QPlayer.QPlayerReady)
        {
            return;
        }

        qViewer.QViewerResumeSet(false);
        QPlayer.QPlayerPause();
        qViewer.QViewerPlaybackUpdate(false, QPlayer.QPlayerTimeRead());
        QViewerClockRun(false);
    }

    public void QViewerSeek(TimeSpan lPosition)
    {
        if (qViewer.QViewerLoupeActive)
        {
            QViewerLoupeSought?.Invoke(lPosition);
            QViewerLoupeSync(lPosition, qViewer.QViewerPlaying);
            return;
        }

        if (!qViewer.QViewerCommandActive)
        {
            return;
        }

        qViewer.QViewerWorkspace.CWorkspaceSeek(lPosition);
    }

    // The workspace holds no pending load: the player seeks now.
    public void QViewerSeekApply(TimeSpan lPosition)
    {
        if (!QPlayer.QPlayerReady)
        {
            return;
        }

        qViewer.QViewerEndSet(false);
        try
        {
            QViewerAccurateSeek(lPosition);
        }
        catch (Exception lSeekException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Preview seek to {lPosition:hh\\:mm\\:ss\\.fff} was rejected: {lSeekException.Message}");
            return;
        }

        qViewer.QViewerPlaybackUpdate(null, lPosition);
    }

    public void QViewerAccurateSeek(TimeSpan lPosition)
    {
        if (qViewer.QViewerMpvActive)
        {
            QViewerSeekRecord(lPosition, "mpv engine seeks directly");
            QPlayer.QPlayerSeek(lPosition);
            return;
        }

        bool lWasRunning = QPlayer.QPlayerAccurateSet();
        QViewerSeekRecord(
            lPosition,
            lWasRunning
                ? "a seek was still running; queued for Flyleaf to conflate"
                : "no seek was in flight");
        QPlayer.QPlayerSeek(lPosition);
    }

    public void QViewerSuspend()
    {
        QViewerClockRun(false);
        if (!QPlayer.QPlayerReady)
        {
            qViewer.QViewerResumeSet(false);
            return;
        }

        qViewer.QViewerResumeSet(qViewer.QViewerPlaying);
        if (!qViewer.QViewerResumeInactive)
        {
            return;
        }

        QPlayer.QPlayerPause();
    }

    public void QViewerResume()
    {
        if (!qViewer.QViewerResumeInactive || !QPlayer.QPlayerReady)
        {
            qViewer.QViewerResumeSet(false);
            if (qViewer.QViewerPlaying)
            {
                QViewerClockRun(true);
            }

            return;
        }

        qViewer.QViewerResumeSet(false);
        QPlayer.QPlayerPlay();
        qViewer.QViewerPlaybackUpdate(true, QPlayer.QPlayerTimeRead());
        QViewerClockRun(true);
    }

    public void QViewerTick()
    {
        if (!qViewer.QViewerCommandActive || !QPlayer.QPlayerReady)
        {
            return;
        }

        if (qViewer.QViewerPlaying && QPlayer.QPlayerEndedRead())
        {
            QViewerEndStop();
            return;
        }

        TimeSpan lPosition = QPlayer.QPlayerTimeRead();
        qViewer.QViewerPlaybackUpdate(null, lPosition);
        QViewerClockTicked?.Invoke(lPosition);
    }

    private void QViewerEndStop()
    {
        qViewer.QViewerResumeSet(false);
        qViewer.QViewerEndSet(true);
        QViewerClockRun(false);
        QPlayer.QPlayerPause();
        qViewer.QViewerPlaybackUpdate(false, null);
    }

    public void QViewerDragSet(bool lDragging)
    {
        if (lDragging)
        {
            if (!qViewerDragActive)
            {
                qViewerSeekTrace.Clear();
                qViewerTraceCount = 0;
            }
            qViewerDragActive = true;
            return;
        }

        qViewerDragActive = false;
        if (qViewerTraceCount == 0)
        {
            return;
        }

        string lSummary = qViewerTraceCount == 1
            ? $"Seek accurate to {qViewerTraceFinal:hh\\:mm\\:ss\\.fff}"
            : $"Seek accurate while dragging to {qViewerTraceFinal:hh\\:mm\\:ss\\.fff} ({qViewerTraceCount} requests)";
        LTrace.LTraceRecord(LTraceKind.LTraceUi, lSummary, string.Join(Environment.NewLine, qViewerSeekTrace));
        qViewerSeekTrace.Clear();
        qViewerTraceCount = 0;
    }

    public void QViewerSeekRecord(TimeSpan lPosition, string lDetail)
    {
        string lSummary = $"Seek accurate to {lPosition:hh\\:mm\\:ss\\.fff}";
        if (!qViewerDragActive)
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
        qViewerSeekTrace.Add($"{lTime}  {lSummary}");
        qViewerSeekTrace.Add($"{new string(' ', 14)}{lDetail}");
        qViewerTraceCount++;
        qViewerTraceFinal = lPosition;
    }
}
