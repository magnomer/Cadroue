using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The edit dispatch: the preset's encoding is read by its name and the source records are built here; which sources
// run, and whether any run at all, is the gate's. The persistent audio save merges and writes each sidecar here and
// traces the failures through the rig's warning; whether it runs is the gate's.
public sealed class LEditFacade
{
    private readonly LRigTrace _lEditFacadeTrace;

    internal LEditFacade(LRigTrace lTrace)
    {
        ArgumentNullException.ThrowIfNull(lTrace);
        _lEditFacadeTrace = lTrace;
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

    public void LEditAudioSave(
        LInspectorPort lPersistent,
        IReadOnlyList<string> lPaths,
        bool lSkipPersistent,
        bool lSkipApply)
    {
        LWorkAudio lPlan = lPersistent.LInspectorAudioRead();
        var lFailed = new List<string>();
        foreach (string lPath in lPaths)
        {
            bool lStored = LAudio.LAudioPlanSave(
                lPath,
                LAudio.LAudioPlanResolve(
                    LAudio.LAudioPlanRead(lPath, LLibrarian.LLibrarianAudioLoad),
                    lPlan,
                    lSkipPersistent,
                    lSkipApply),
                LLibrarian.LLibrarianAudioSave);
            if (!lStored)
            {
                lFailed.Add(lPath);
            }
        }

        if (lFailed.Count > 0)
        {
            _lEditFacadeTrace.LRigTraceWarning(
                $"Audio persistent save failed for {lFailed.Count} file(s): those sidecars were not written",
                string.Join(Environment.NewLine, lFailed));
        }
    }
}
