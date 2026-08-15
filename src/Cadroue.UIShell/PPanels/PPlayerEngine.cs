using System;

namespace Cadroue.UIShell.PPanels;

internal abstract class PPlayerEngine : IDisposable
{
    public abstract void PPlayerOpen(string sourcePath);

    public abstract void PPlayerSeek(TimeSpan playbackPosition);

    public abstract void PPlayerStop();

    public abstract void PPlayerPlay();

    public abstract void PPlayerPause();

    public abstract void PPlayerVolumeSet(double volume);

    public abstract void PPlayerFilterSet(string filterChain);

    public abstract void PPlayerAudioSet(string filterChain);

    public abstract void PPlayerDecodeInterrupt();

    public abstract TimeSpan PPlayerTimeRead();

    public abstract void Dispose();
}
