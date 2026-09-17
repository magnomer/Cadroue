using System;
using FlyleafLib.MediaPlayer;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

internal sealed class PPlayer
{
    private PPlayerEngine? pPlayerActive;

    public bool PPlayerReady => pPlayerActive is not null;

    public Player? PPlayerFlyleafPlayer => (pPlayerActive as PPlayerFlyleaf)?.PPlayerFlyleafPlayer;

    public void PPlayerFlyleafSet(Player player, LPlayer? lPlayer = null)
    {
        PPlayerEngineSet(new PPlayerFlyleaf(player, lPlayer));
    }

    public void PPlayerMpvSet(nint hostHandle)
    {
        PPlayerEngineSet(new PPlayerMpv(hostHandle));
    }

    public void PPlayerEngineSet(PPlayerEngine? engine)
    {
        if (ReferenceEquals(pPlayerActive, engine))
        {
            return;
        }

        pPlayerActive?.Dispose();
        pPlayerActive = engine;
    }

    public void PPlayerOpen(string sourcePath) => pPlayerActive?.PPlayerOpen(sourcePath);

    public void PPlayerSeek(TimeSpan playbackPosition) => pPlayerActive?.PPlayerSeek(playbackPosition);

    public void PPlayerStop() => pPlayerActive?.PPlayerStop();

    public void PPlayerPlay() => pPlayerActive?.PPlayerPlay();

    public void PPlayerPause() => pPlayerActive?.PPlayerPause();

    public void PPlayerVolumeSet(double volume) => pPlayerActive?.PPlayerVolumeSet(volume);

    public void PPlayerMpvUpdate() => (pPlayerActive as PPlayerMpv)?.PPlayerMpvUpdate();

    public void PPlayerFilterSet(string filterChain) => pPlayerActive?.PPlayerFilterSet(filterChain);

    public void PPlayerAudioSet(string filterChain) => pPlayerActive?.PPlayerAudioSet(filterChain);

    public void PPlayerDecodeInterrupt() => pPlayerActive?.PPlayerDecodeInterrupt();

    public TimeSpan PPlayerTimeRead() => pPlayerActive?.PPlayerTimeRead() ?? TimeSpan.Zero;

    public bool PPlayerEndedRead() => pPlayerActive?.PPlayerEndedRead() ?? false;

    public void PPlayerDispose() => PPlayerEngineSet(null);
}
