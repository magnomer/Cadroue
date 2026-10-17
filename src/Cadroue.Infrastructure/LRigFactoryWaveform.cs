using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LRigFactoryWaveform
{
    // One flow's waveform scan over its own orchestrator; its notice reaches one handler, detached at disposal.
    public static LRigWaveform LRigWaveformCreate()
    {
        LWaveformOrchestrator lOrchestrator = new();
        Action<LWaveformNotice>? lRelay = null;
        return new LRigWaveform(
            lOrchestrator.LWaveformStart,
            lOrchestrator.LWaveformClear,
            () => lOrchestrator.LWaveformCurrent,
            LWaveform.LWaveformRangeRead,
            lHandler =>
            {
                lRelay = _ => lHandler();
                lOrchestrator.LWaveformReady += lRelay;
            },
            () =>
            {
                lOrchestrator.LWaveformReady -= lRelay;
                lOrchestrator.Dispose();
            });
    }
}
