using System;

using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

internal sealed class PPlayerMpv : PPlayerEngine
{
    private static readonly TimeSpan pPlayerMpvOpenBudget = TimeSpan.FromSeconds(3);

    private readonly LMpv pPlayerMpvLibrary;

    public PPlayerMpv(nint hostHandle)
    {
        pPlayerMpvLibrary = new LMpv();
        pPlayerMpvLibrary.LMpvHandleCreate(hostHandle);
    }

    public override void PPlayerOpen(string sourcePath)
    {
        LMpvProbe pPlayerMpvLoaded = pPlayerMpvLibrary.LMpvOpenWait(sourcePath, pPlayerMpvOpenBudget);
        if (pPlayerMpvLoaded != LMpvProbe.LMpvProbeUsable)
        {
            throw new InvalidOperationException(
                $"mpv did not reach the loaded state for '{sourcePath}' within {pPlayerMpvOpenBudget.TotalSeconds:0.#}s ({pPlayerMpvLoaded}).");
        }
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
