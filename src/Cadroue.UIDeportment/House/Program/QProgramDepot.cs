using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The workspace folder the program has applied: a new preferred folder moves the depot there once, prepares its index
// and points the sidecars at it; a failure keeps the applied root.
public sealed class QProgramDepot
{
    private string? qDepotRootApplied;

    public string? QProgramDepotRoot => qDepotRootApplied;

    internal bool QProgramDepotHandle()
    {
        bool lDepotApplied = QProgramDepotApply();
        QProgramSidecarApply();
        return lDepotApplied;
    }

    private static void QProgramSidecarApply() =>
        LSidecarPath.LSidecarFolderApply(LPreference.LPreferenceStateCurrent.LPreferenceRecordWorkspace);

    public bool QProgramDepotApply()
    {
        try
        {
            string lDepotRoot = LDepot.LDepotRootResolve(
                LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
            if (string.Equals(lDepotRoot, qDepotRootApplied, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!QProgramDepotPrepare(lDepotRoot))
            {
                return false;
            }

            QProgramDepotSet(lDepotRoot);
            QProgramSidecarApply();
            LTraceLog.LTraceLoadingRecord($"Workspace at {lDepotRoot}");
            return true;
        }
        catch (Exception lException)
        {
            QProgramDepotSet(null);
            LTraceLog.LTraceErrorRecord("Workspace folder could not be prepared", lException);
            return false;
        }
    }

    private void QProgramDepotSet(string? lDepotRoot) => qDepotRootApplied = lDepotRoot;

    private bool QProgramDepotPrepare(string lDepotRoot)
    {
        if (qDepotRootApplied is string lDepotPrevious
            && !LDepotRelocation.LDepotFolderMove(lDepotPrevious, lDepotRoot))
        {
            return false;
        }

        if (qDepotRootApplied is null)
        {
            LDepot.LDepotRootSet(lDepotRoot);
        }

        LDepotIndex.LDepotIndexCreate();
        return true;
    }
}
