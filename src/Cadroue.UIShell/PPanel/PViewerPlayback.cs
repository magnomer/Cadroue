using System;
using System.Windows;

using Cadroue.Core;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PViewer
{
    internal void PViewerLoupeSync(TimeSpan pViewerPosition, bool pViewerPlaying)
    {
        PViewerPlaybackUpdate(pViewerPlaying, pViewerPosition);
        PViewerClockTick?.Invoke(pViewerPosition);
    }

    public bool PViewerPlayingRead() => LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying;

    public TimeSpan PViewerPositionRead() => LPreviewStateCurrent.LPlaybackState.LPlaybackPosition;

    public void PViewerPlay()
    {
        if (pViewerLoupe is not null)
        {
            pViewerLoupe.PSLoupePlay();
            return;
        }

        if (!pViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
        {
            return;
        }

        if (pViewerEndReached)
        {
            pViewerEndReached = false;
            pViewerPlayer.PPlayerSeek(TimeSpan.Zero);
        }

        pViewerResumeInactive = false;
        pViewerPlayer.PPlayerPlay();
        PViewerPlaybackUpdate(true, pViewerPlayer.PPlayerTimeRead());
        pViewerClockTimer.Start();
    }

    public void PViewerPause()
    {
        if (pViewerLoupe is not null)
        {
            pViewerLoupe.PSLoupePause();
            return;
        }

        if (!pViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        pViewerResumeInactive = false;
        pViewerPlayer.PPlayerPause();
        PViewerPlaybackUpdate(false, pViewerPlayer.PPlayerTimeRead());
        pViewerClockTimer.Stop();
    }

    public void PViewerSeek(TimeSpan playbackPosition)
    {
        if (pViewerLoupe is not null)
        {
            pViewerLoupe.PSLoupeSeek(playbackPosition);
            PViewerLoupeSync(playbackPosition, LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying);
            return;
        }

        if (!pViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        pViewerEndReached = false;
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

        PViewerPlaybackUpdate(null, playbackPosition);
    }

    public void PViewerDragSet(bool pViewerDragging)
    {
        if (pViewerDragging)
        {
            if (!pViewerDragActive)
            {
                pViewerSeekTrace.Clear();
                pViewerTraceCount = 0;
            }

            pViewerDragActive = true;
            return;
        }

        pViewerDragActive = false;
        if (pViewerTraceCount == 0)
        {
            return;
        }

        string pViewerSummary = pViewerTraceCount == 1
            ? $"Seek accurate to {pViewerTraceFinal:hh\\:mm\\:ss\\.fff}"
            : $"Seek accurate while dragging to {pViewerTraceFinal:hh\\:mm\\:ss\\.fff} ({pViewerTraceCount} requests)";
        LTrace.LTraceRecord(
            LTraceKind.LTraceUi,
            pViewerSummary,
            string.Join(Environment.NewLine, pViewerSeekTrace));
        pViewerSeekTrace.Clear();
        pViewerTraceCount = 0;
    }

    private void PViewerSeekRecord(TimeSpan pViewerPosition, string pViewerDetail)
    {
        string pViewerSummary = $"Seek accurate to {pViewerPosition:hh\\:mm\\:ss\\.fff}";
        if (!pViewerDragActive)
        {
            LTrace.LTraceRecord(LTraceKind.LTraceUi, pViewerSummary, pViewerDetail);
            return;
        }

        if (!LTrace.LTraceCheck(LTraceKind.LTraceUi))
        {
            return;
        }

        string pViewerTime = DateTimeOffset.Now.ToString(
            "HH:mm:ss.fff",
            System.Globalization.CultureInfo.InvariantCulture);
        pViewerSeekTrace.Add($"{pViewerTime}  {pViewerSummary}");
        pViewerSeekTrace.Add($"{new string(' ', 14)}{pViewerDetail}");
        pViewerTraceCount++;
        pViewerTraceFinal = pViewerPosition;
    }

    public void PViewerVolumeSet(double volume)
    {
        if (pViewerLoupe is not null)
        {
            pViewerVolume = LPreferenceState.LPreferenceVolumeClamp(volume);
            if (LPreference.LPreferenceStateCurrent.LPreferenceVolumeUnified)
                LPreference.LPreferenceVolumeSet(pViewerVolume);
            pViewerLoupe.PSLoupeVolumeSet(pViewerVolume);
            return;
        }

        if (!pViewerCommandActive) return;
        pViewerVolume = LPreferenceState.LPreferenceVolumeClamp(volume);
        if (LPreference.LPreferenceStateCurrent.LPreferenceVolumeUnified)
            LPreference.LPreferenceVolumeSet(pViewerVolume);
        if (!pViewerPlayer.PPlayerReady)
        {
            return;
        }

        pViewerPlayer.PPlayerVolumeSet(pViewerVolume);
    }
}
