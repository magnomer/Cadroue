using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The convert dispatch: the preset's encoding is read by its name, the source records and the priority are built
// here; which sources run, and whether any run at all, is the gate's. The dispatch is not awaited.
public sealed class LConvertFacade
{
    internal LConvertFacade()
    {
    }

    public void LConvertRun(
        string lPresetName,
        IReadOnlyList<(string LConvertPath, Guid LConvertBatch)> lSources,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab) =>
        _ = LMessengerConvert.LMessengerConvertDescribe(
            lUrgent ? LWorkPriority.LWorkPriorityHigh : LWorkPriority.LWorkPriorityNormal,
            lSources.Select(lSource => new LWorkSource(lSource.LConvertPath, lSource.LConvertBatch)).ToArray(),
            LPresetSelection.LPresetEncodingRead(lPresetName),
            lRelayTarget,
            lSourceTab);
}
