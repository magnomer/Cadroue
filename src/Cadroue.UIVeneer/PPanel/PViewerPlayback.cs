using System;
using System.Windows;

using Cadroue.Core;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PViewer
{
    internal void PViewerLoupeSync(TimeSpan pViewerPosition, bool pViewerPlaying)
    {
        LViewer.LViewerPlaybackUpdate(pViewerPlaying, pViewerPosition);
        PViewerClockTick?.Invoke(pViewerPosition);
    }

    public bool PViewerPlayingRead() => LViewer.LViewerPlaying;

    public TimeSpan PViewerPositionRead() => LViewer.LViewerPosition;

    public void PViewerPlay()
    {
        if (pViewerLoupe is not null)
        {
            pViewerLoupe.PSLoupePlay();
            return;
        }

        if (!LViewer.LViewerCommandActive)
        {
            return;
        }

        if (LViewer.LViewerIntent is { } pViewerPending)
        {
            LViewer.LViewerIntentSet(pViewerPending with { LViewerIntentPlaying = true });
            return;
        }

        if (!pViewerPlayer.PPlayerReady || LViewer.LViewerPlaying)
        {
            return;
        }

        if (LViewer.LViewerEndReached)
        {
            LViewer.LViewerEndSet(false);
            pViewerPlayer.PPlayerSeek(TimeSpan.Zero);
        }

        LViewer.LViewerResumeSet(false);
        pViewerPlayer.PPlayerPlay();
        LViewer.LViewerPlaybackUpdate(true, pViewerPlayer.PPlayerTimeRead());
        pViewerClockTimer.Start();
    }

    public void PViewerPause()
    {
        if (pViewerLoupe is not null)
        {
            pViewerLoupe.PSLoupePause();
            return;
        }

        if (!LViewer.LViewerCommandActive)
        {
            return;
        }

        if (LViewer.LViewerIntent is { } pViewerPending)
        {
            LViewer.LViewerIntentSet(pViewerPending with { LViewerIntentPlaying = false });
            return;
        }

        if (!pViewerPlayer.PPlayerReady)
        {
            return;
        }

        LViewer.LViewerResumeSet(false);
        pViewerPlayer.PPlayerPause();
        LViewer.LViewerPlaybackUpdate(false, pViewerPlayer.PPlayerTimeRead());
        pViewerClockTimer.Stop();
    }

    public void PViewerSeek(TimeSpan playbackPosition)
    {
        if (pViewerLoupe is not null)
        {
            pViewerLoupe.PSLoupeSeek(playbackPosition);
            PViewerLoupeSync(playbackPosition, LViewer.LViewerPlaying);
            return;
        }

        if (!LViewer.LViewerCommandActive)
        {
            return;
        }

        if (LViewer.LViewerIntent is { } pViewerPending)
        {
            LViewer.LViewerIntentSet(pViewerPending with { LViewerIntentPosition = playbackPosition });
            LViewer.LViewerPlaybackUpdate(null, playbackPosition);
            return;
        }

        if (!pViewerPlayer.PPlayerReady)
        {
            return;
        }

        LViewer.LViewerEndSet(false);
        try
        {
            PPlayerAccurateSeek(playbackPosition);
        }
        catch (Exception pViewerSeekException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Preview seek to {playbackPosition:hh\\:mm\\:ss\\.fff} was rejected: {pViewerSeekException.Message}");
            return;
        }

        LViewer.LViewerPlaybackUpdate(null, playbackPosition);
    }

    public void PViewerDragSet(bool pViewerDragging) => LViewer.LViewerDragSet(pViewerDragging);

    public void PViewerVolumeAdjust(double pViewerDelta) => PViewerVolumeSet(LViewer.LViewerVolume + pViewerDelta);

    public void PViewerVolumeSet(double volume)
    {
        if (pViewerLoupe is not null)
        {
            LViewer.LViewerVolumeSet(volume);
            pViewerLoupe.PSLoupeVolumeSet(LViewer.LViewerVolume);
            return;
        }

        if (!LViewer.LViewerCommandActive) return;
        LViewer.LViewerVolumeSet(volume);
        if (!pViewerPlayer.PPlayerReady)
        {
            return;
        }

        pViewerPlayer.PPlayerVolumeSet(LViewer.LViewerVolume);
    }
}
