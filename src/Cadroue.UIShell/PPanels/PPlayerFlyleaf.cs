using System;
using FlyleafLib.MediaPlayer;

using Cadroue.Application;

namespace Cadroue.UIShell.PPanels;

internal sealed class PPlayerFlyleaf : PPlayerEngine
{
    public PPlayerFlyleaf(Player player)
    {
        PPlayerFlyleafPlayer = player;
    }

    public Player PPlayerFlyleafPlayer { get; }

    public override void PPlayerOpen(string sourcePath)
    {
        var openResult = PPlayerFlyleafPlayer.Open(sourcePath);
        if (!openResult.Success)
        {
            throw new InvalidOperationException(
                openResult.Error ?? LLocalization.LLocalizationTextRead("Viewer.Error.FlyleafOpen"));
        }
    }

    public override void PPlayerSeek(TimeSpan playbackPosition)
    {
        PPlayerFlyleafPlayer.SeekAccurate((int)playbackPosition.TotalMilliseconds);
    }

    public override void PPlayerStop()
    {
        PPlayerFlyleafPlayer.Stop();
    }

    public override void PPlayerPlay()
    {
        PPlayerFlyleafPlayer.Play();
    }

    public override void PPlayerPause()
    {
        PPlayerFlyleafPlayer.Pause();
    }

    public override void PPlayerVolumeSet(double volume)
    {
        PPlayerFlyleafPlayer.Audio.Volume = (int)Math.Round(volume);
    }

    public override void PPlayerDecodeInterrupt()
    {
        PPlayerFlyleafPlayer.Stop();
    }

    public override TimeSpan PPlayerTimeRead()
    {
        return TimeSpan.FromTicks(PPlayerFlyleafPlayer.CurTime);
    }

    public override void Dispose()
    {
        try
        {
            PPlayerFlyleafPlayer.Stop();
            PPlayerFlyleafPlayer.Dispose();
        }
        catch
        {
        }
    }
}
