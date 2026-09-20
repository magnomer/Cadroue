using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class LPlayerMpv
{
    private static readonly TimeSpan lPlayerMpvBudget = TimeSpan.FromSeconds(15);

    private readonly LMpv lPlayerMpvLibrary;

    public LPlayerMpv(nint lHostHandle)
    {
        lPlayerMpvLibrary = new LMpv();
        lPlayerMpvLibrary.LMpvContextCreate(lHostHandle);
    }

    public LPlayerSeam LPlayerSeamRead() => new(
        LPlayerOpen,
        LPlayerPlay,
        LPlayerPause,
        lPlayerMpvLibrary.LMpvStop,
        lPlayerMpvLibrary.LMpvSeek,
        lPlayerMpvLibrary.LMpvVolumeSet,
        lPlayerMpvLibrary.LMpvFilterSet,
        lPlayerMpvLibrary.LMpvAudioSet,
        LPlayerPreviewApply,
        LPlayerUpdate,
        lPlayerMpvLibrary.LMpvTimeRead,
        lPlayerMpvLibrary.LMpvEndedRead,
        LPlayerFactsRecord,
        LPlayerDispose);

    private void LPlayerOpen(string lSourcePath)
    {
        LMpvProbe lLoaded = lPlayerMpvLibrary.LMpvMediaCheck(lSourcePath, lPlayerMpvBudget, CancellationToken.None);
        if (lLoaded != LMpvProbe.LMpvProbeUsable)
        {
            throw new InvalidOperationException(
                $"mpv did not reach the loaded state for '{lSourcePath}' "
                + $"within {lPlayerMpvBudget.TotalSeconds:0.#}s ({lLoaded}).");
        }
    }

    private void LPlayerPlay() => lPlayerMpvLibrary.LMpvPlaySet(true);

    private void LPlayerPause() => lPlayerMpvLibrary.LMpvPlaySet(false);

    private void LPlayerUpdate() => lPlayerMpvLibrary.LMpvSeek(lPlayerMpvLibrary.LMpvTimeRead());

    private static void LPlayerPreviewApply(LPreviewApplication lApplication)
    {
    }

    private static void LPlayerFactsRecord(string lReason, double? lMilliseconds)
    {
    }

    private void LPlayerDispose()
    {
        try
        {
            lPlayerMpvLibrary.LMpvDispose();
        }
        catch
        {
        }
    }
}
