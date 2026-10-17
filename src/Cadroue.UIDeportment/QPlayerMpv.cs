using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QPlayerMpv
{
    private static readonly TimeSpan qPlayerMpvBudget = TimeSpan.FromSeconds(15);

    private readonly LMpv qPlayerMpvLibrary;

    public QPlayerMpv(nint lHostHandle)
    {
        qPlayerMpvLibrary = new LMpv();
        qPlayerMpvLibrary.LMpvContextCreate(lHostHandle);
    }

    public QPlayerSeam QPlayerSeamRead() => new(
        QPlayerOpen,
        QPlayerPlay,
        QPlayerPause,
        qPlayerMpvLibrary.LMpvStop,
        qPlayerMpvLibrary.LMpvSeek,
        qPlayerMpvLibrary.LMpvVolumeSet,
        qPlayerMpvLibrary.LMpvFilterSet,
        qPlayerMpvLibrary.LMpvAudioSet,
        QPlayerPreviewApply,
        QPlayerUpdate,
        qPlayerMpvLibrary.LMpvTimeRead,
        qPlayerMpvLibrary.LMpvEndedRead,
        QPlayerFactsRecord,
        QPlayerDispose);

    private void QPlayerOpen(string lSourcePath)
    {
        LMpvProbe lLoaded = qPlayerMpvLibrary.LMpvMediaCheck(lSourcePath, qPlayerMpvBudget, CancellationToken.None);
        QPlayerProbeCheck(lLoaded, lSourcePath);
    }

    private static void QPlayerProbeCheck(LMpvProbe lLoaded, string lSourcePath)
    {
        if (lLoaded != LMpvProbe.LMpvProbeUsable)
        {
            throw new InvalidOperationException(
                $"mpv did not reach the loaded state for '{lSourcePath}' "
                + $"within {qPlayerMpvBudget.TotalSeconds:0.#}s ({lLoaded}).");
        }
    }

    private void QPlayerPlay() => qPlayerMpvLibrary.LMpvPlaySet(true);

    private void QPlayerPause() => qPlayerMpvLibrary.LMpvPlaySet(false);

    private void QPlayerUpdate() => qPlayerMpvLibrary.LMpvSeek(qPlayerMpvLibrary.LMpvTimeRead());

    private static void QPlayerPreviewApply(LPreviewApplication lApplication)
    {
    }

    private static void QPlayerFactsRecord(string lReason, double? lMilliseconds)
    {
    }

    private void QPlayerDispose()
    {
        try
        {
            qPlayerMpvLibrary.LMpvDispose();
        }
        catch
        {
        }
    }
}
