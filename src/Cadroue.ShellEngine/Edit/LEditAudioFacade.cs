using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The persistent audio save: merges and writes each sidecar here and traces the failures through the rig's warning;
// whether it runs is the gate's.
public sealed class LEditAudioFacade
{
    private readonly LRigTrace _lEditAudioTrace;

    internal LEditAudioFacade(LRigTrace lTrace)
    {
        ArgumentNullException.ThrowIfNull(lTrace);
        _lEditAudioTrace = lTrace;
    }

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
            bool lStored = LAudioSidecar.LAudioSidecarSave(
                lPath,
                LAudioPlan.LAudioPlanResolve(
                    LAudioSidecar.LAudioSidecarRead(lPath, LLibrarian.LLibrarianAudioLoad),
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
            _lEditAudioTrace.LRigTraceWarning(
                $"Audio persistent save failed for {lFailed.Count} file(s): those sidecars were not written",
                string.Join(Environment.NewLine, lFailed));
        }
    }
}
