using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The edit dispatch: the preset's encoding is read by its name and the source records are built here; which sources
// run, and whether any run at all, is the gate's.
public sealed class LEditFacade
{
    internal LEditFacade()
    {
    }

    public void LEditAllRun(
        string lPresetName,
        IReadOnlyList<(string LEditPath, Guid LEditBatch)> lSources,
        Guid lRelayTarget,
        Guid lSourceTab) =>
        LMessenger.LMessengerEditDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lSources.Select(lSource => new LWorkSource(lSource.LEditPath, lSource.LEditBatch)).ToArray(),
            LPresetSelection.LPresetEncodingRead(lPresetName),
            lRelayTarget,
            lSourceTab);
}
