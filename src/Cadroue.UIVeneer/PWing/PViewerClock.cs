using System;

using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PViewer
{
    private void PPlayerSuspend()
    {
        pViewerClockTimer.Stop();
        if (!pViewerPlayer.PPlayerReady)
        {
            LViewer.LViewerResumeSet(false);
            return;
        }

        LViewer.LViewerResumeSet(LViewer.LViewerPlaying);
        if (!LViewer.LViewerResumeInactive)
        {
            return;
        }

        pViewerPlayer.PPlayerPause();
    }

    private void PPlayerResume()
    {
        if (!LViewer.LViewerResumeInactive || !pViewerPlayer.PPlayerReady)
        {
            LViewer.LViewerResumeSet(false);
            if (LViewer.LViewerPlaying)
            {
                pViewerClockTimer.Start();
            }
            return;
        }

        LViewer.LViewerResumeSet(false);
        pViewerPlayer.PPlayerPlay();
        LViewer.LViewerPlaybackUpdate(true, pViewerPlayer.PPlayerTimeRead());
        pViewerClockTimer.Start();
    }

    private void PViewerClockHandle(object? sender, EventArgs eventArgs)
    {
        if (!LViewer.LViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        if (LViewer.LViewerPlaying && pViewerPlayer.PPlayerEndedRead())
        {
            PViewerEndStop();
            return;
        }

        TimeSpan playbackPosition = pViewerPlayer.PPlayerTimeRead();
        LViewer.LViewerPlaybackUpdate(null, playbackPosition);
        PViewerClockTick?.Invoke(playbackPosition);
    }

    private void PViewerEndStop()
    {
        LViewer.LViewerResumeSet(false);
        LViewer.LViewerEndSet(true);
        pViewerClockTimer.Stop();
        pViewerPlayer.PPlayerPause();
        LViewer.LViewerPlaybackUpdate(false, null);
    }
}
