using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The fix dispatch: the preset's encoding is read by its name, the source records and the priority are built here;
// which sources run, and whether any run at all, is the gate's.
public sealed class LFixFacade : LFixPort
{
    internal LFixFacade()
    {
    }

    public void LFixRun(
        string lPresetName,
        IReadOnlyList<(string LFixPath, Guid LFixBatch)> lSources,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab) =>
        LMessengerFix.LMessengerFixDescribe(
            lUrgent ? LWorkPriority.LWorkPriorityHigh : LWorkPriority.LWorkPriorityNormal,
            lSources.Select(lSource => new LWorkSource(lSource.LFixPath, lSource.LFixBatch)).ToArray(),
            LPresetSelection.LPresetEncodingRead(lPresetName),
            lRelayTarget,
            lSourceTab);
}
