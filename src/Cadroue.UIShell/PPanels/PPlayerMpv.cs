using System;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

internal sealed class PPlayerMpv : PPlayerEngine
{
    private readonly LMpv pPlayerMpvLibrary;

    public PPlayerMpv(nint hostHandle)
    {
        pPlayerMpvLibrary = new LMpv();
        pPlayerMpvLibrary.LMpvHandleCreate(hostHandle);
    }

    public override void PPlayerOpen(string sourcePath)
    {
        pPlayerMpvLibrary.LMpvOpen(sourcePath);
    }

    public override void PPlayerSeek(TimeSpan playbackPosition)
    {
        pPlayerMpvLibrary.LMpvSeek(playbackPosition);
    }

    public override void PPlayerStop()
    {
        pPlayerMpvLibrary.LMpvStop();
    }

    public override void PPlayerPlay()
    {
        pPlayerMpvLibrary.LMpvPlaySet(true);
    }

    public override void PPlayerPause()
    {
        pPlayerMpvLibrary.LMpvPlaySet(false);
    }

    public override void PPlayerVolumeSet(double volume)
    {
        pPlayerMpvLibrary.LMpvVolumeSet(volume);
    }

    public override void PPlayerDecodeInterrupt()
    {
        pPlayerMpvLibrary.LMpvDecodeInterrupt();
    }

    public override TimeSpan PPlayerTimeRead()
    {
        return pPlayerMpvLibrary.LMpvTimeRead();
    }

    public override void Dispose()
    {
        try
        {
            pPlayerMpvLibrary.LMpvDispose();
        }
        catch
        {
        }
    }
}
