using System;
using FlyleafLib.MediaPlayer;

namespace Cadroue.UIShell.PPanels;

internal sealed class PPlayer
{
    private PPlayerEngine? pPlayerActive;

    public bool PPlayerReady => pPlayerActive is not null;

    public Player? PPlayerFlyleafPlayer => (pPlayerActive as PPlayerFlyleaf)?.PPlayerFlyleafPlayer;

    public void PPlayerFlyleafSet(Player player)
    {
        PPlayerEngineSet(new PPlayerFlyleaf(player));
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

    public void PPlayerEqualizerSet(int brightness, int contrast, int saturation, int hue) =>
        pPlayerActive?.PPlayerEqualizerSet(brightness, contrast, saturation, hue);

    public void PPlayerMpvGammaSet(double gammaFactor) =>
        (pPlayerActive as PPlayerMpv)?.PPlayerMpvGammaSet(gammaFactor);

    public void PPlayerMpvRefresh() => (pPlayerActive as PPlayerMpv)?.PPlayerMpvRefresh();

    public void PPlayerFilterSet(string filterChain) => pPlayerActive?.PPlayerFilterSet(filterChain);

    public void PPlayerDecodeInterrupt() => pPlayerActive?.PPlayerDecodeInterrupt();

    public TimeSpan PPlayerTimeRead() => pPlayerActive?.PPlayerTimeRead() ?? TimeSpan.Zero;

    public void PPlayerDispose() => PPlayerEngineSet(null);
}
