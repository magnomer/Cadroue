using System;
using System.Threading;

using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

internal sealed class PPlayerMpv : PPlayerEngine
{
    private static readonly TimeSpan pPlayerMpvBudget = TimeSpan.FromSeconds(15);

    private readonly LMpv pPlayerMpvLibrary;

    private CancellationTokenSource? pPlayerMpvCancellation;

    public PPlayerMpv(nint hostHandle)
    {
        pPlayerMpvLibrary = new LMpv();
        pPlayerMpvLibrary.LMpvContextCreate(hostHandle);
    }

    public void PPlayerMpvCancel()
    {
        pPlayerMpvCancellation?.Cancel();
    }

    public override void PPlayerOpen(string sourcePath)
    {
        CancellationTokenSource pPlayerMpvCancel = new();
        CancellationTokenSource? pPlayerMpvPrevious = Interlocked.Exchange(ref pPlayerMpvCancellation, pPlayerMpvCancel);
        pPlayerMpvPrevious?.Cancel();
        pPlayerMpvPrevious?.Dispose();

        try
        {
            LMpvProbe pPlayerMpvLoaded = pPlayerMpvLibrary.LMpvMediaCheck(sourcePath, pPlayerMpvBudget, pPlayerMpvCancel.Token);
            if (pPlayerMpvLoaded != LMpvProbe.LMpvProbeUsable)
            {
                throw new InvalidOperationException(
                    $"mpv did not reach the loaded state for '{sourcePath}' within {pPlayerMpvBudget.TotalSeconds:0.#}s ({pPlayerMpvLoaded}).");
            }
        }
        finally
        {
            if (Interlocked.CompareExchange(ref pPlayerMpvCancellation, null, pPlayerMpvCancel) == pPlayerMpvCancel)
            {
                pPlayerMpvCancel.Dispose();
            }
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

    public void PPlayerMpvUpdate() =>
        pPlayerMpvLibrary.LMpvSeek(pPlayerMpvLibrary.LMpvTimeRead());

    public override void PPlayerFilterSet(string filterChain)
    {
        pPlayerMpvLibrary.LMpvFilterSet(filterChain);
    }

    public override void PPlayerAudioSet(string filterChain)
    {
        pPlayerMpvLibrary.LMpvAudioSet(filterChain);
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
            pPlayerMpvCancellation?.Cancel();
            pPlayerMpvLibrary.LMpvDispose();
        }
        catch
        {
        }
    }
}
