using System;

using Cadroue.Application;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PViewer
{
    private void PPlayerSuspend()
    {
        pViewerClockTimer.Stop();
        if (!pViewerPlayer.PPlayerReady)
        {
            pViewerResumeInactive = false;
            return;
        }

        pViewerResumeInactive = LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying;
        if (!pViewerResumeInactive)
        {
            return;
        }

        pViewerPlayer.PPlayerPause();
    }

    private void PPlayerResume()
    {
        if (!pViewerResumeInactive || !pViewerPlayer.PPlayerReady)
        {
            pViewerResumeInactive = false;
            if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
            {
                pViewerClockTimer.Start();
            }
            return;
        }

        pViewerResumeInactive = false;
        pViewerPlayer.PPlayerPlay();
        PViewerPlaybackUpdate(true, pViewerPlayer.PPlayerTimeRead());
        pViewerClockTimer.Start();
    }

    private void PViewerClockHandle(object? sender, EventArgs eventArgs)
    {
        if (!pViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying && pViewerPlayer.PPlayerEndedRead())
        {
            PViewerEndStop();
            return;
        }

        TimeSpan playbackPosition = pViewerPlayer.PPlayerTimeRead();
        PViewerPlaybackUpdate(null, playbackPosition);
        PViewerClockTick?.Invoke(playbackPosition);
    }

    private void PViewerEndStop()
    {
        pViewerResumeInactive = false;
        pViewerEndReached = true;
        pViewerClockTimer.Stop();
        pViewerPlayer.PPlayerPause();
        PViewerPlaybackUpdate(false, null);
    }

    private void PViewerPlaybackUpdate(bool? playing, TimeSpan? playbackPosition)
    {
        LPlaybackState playbackState = LPreviewStateCurrent.LPlaybackState;
        bool pViewerPlayingNow = playing ?? playbackState.LPlaybackStatePlaying;
        LPreviewStateCurrent = LPreviewStateCurrent.LPlaybackStateChange(new LPlaybackState(
            pViewerPlayingNow,
            playbackPosition ?? playbackState.LPlaybackPosition));
        if (pViewerPlayingNow != playbackState.LPlaybackStatePlaying)
        {
            PViewerPlayingChange?.Invoke(pViewerPlayingNow);
        }
    }
}
