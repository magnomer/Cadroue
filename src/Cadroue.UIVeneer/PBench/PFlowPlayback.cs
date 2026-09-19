using Cadroue.Core;

using Cadroue.Application;

namespace Cadroue.UIVeneer.PBench;

public sealed partial class PFlow
{
    private const double PFlowVolumeStep = 5;

    public event Action? PFlowPlay;
    public event Action? PFlowPause;
    public event Action<double>? PFlowVolumeAdjust;

    public Func<bool>? PFlowPlayingSource { get; set; }

    public void PFlowPlayRaise()
    {
        if (LFlow.LFlowCommandActive) PFlowPlay?.Invoke();
    }

    public void PFlowPauseRaise()
    {
        if (LFlow.LFlowCommandActive) PFlowPause?.Invoke();
    }
}
