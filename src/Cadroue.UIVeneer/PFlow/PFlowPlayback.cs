using Cadroue.Core;

using Cadroue.Application;

namespace Cadroue.UIVeneer.PFlow;

public sealed partial class PFlow
{
    private const double PFlowVolumeStep = 5;

    private double pFlowVolumeCurrent = 100;

    public event Action? PFlowPlay;
    public event Action? PFlowPause;
    public event Action<bool>? PFlowPlayingChange;
    public event Action<double>? PFlowVolumeChange;
    public event Action<double>? PFlowVolumeValue;

    public Func<bool>? PFlowPlayingSource { get; set; }

    public void PFlowVolumeSet(double volume)
    {
        if (!pFlowCommandActive) return;
        pFlowVolumeCurrent = LPreferenceState.LPreferenceVolumeClamp(volume);
        PFlowVolumeValue?.Invoke(pFlowVolumeCurrent);
    }

    public void PFlowVolumeRaise(double pFlowVolume)
    {
        if (!pFlowCommandActive) return;
        double pFlowVolumeClamp = LPreferenceState.LPreferenceVolumeClamp(pFlowVolume);
        PFlowVolumeSet(pFlowVolumeClamp);
        PFlowVolumeChange?.Invoke(pFlowVolumeClamp);
    }

    public void PFlowPlayRaise()
    {
        if (pFlowCommandActive) PFlowPlay?.Invoke();
    }

    public void PFlowPauseRaise()
    {
        if (pFlowCommandActive) PFlowPause?.Invoke();
    }

    public void PFlowPlayingRaise(bool pFlowPlaying) => PFlowPlayingChange?.Invoke(pFlowPlaying);
}
