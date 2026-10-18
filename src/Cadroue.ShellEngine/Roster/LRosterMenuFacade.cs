using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The item menu for the roster menu gate. The menu answer holds its own run: restart and cancel act on the records the
// read chose, at the moment of the click.
public sealed class LRosterMenuFacade : LRosterMenuPort
{
    internal LRosterMenuFacade()
    {
    }

    public (bool LRosterMenuRestartable, bool LRosterMenuCancellable, int LRosterMenuCount,
        IReadOnlyList<string> LRosterMenuPaths, Action LRosterMenuAction) LRosterMenuRead(
        LStation lStation, Guid lClicked)
    {
        LScheduleContract lSchedule = LStationEngine.LStationSchedule!;
        if (lSchedule.LScheduleRecords.FirstOrDefault(lWorkItem => lWorkItem.LWorkId == lClicked) is not { } lItem)
        {
            return (false, false, 0, [], () => { });
        }

        IReadOnlyList<LWorkItem> lChosen = lStation.LStationSelectionRead();
        if (LRosterRestartCheck(lItem))
        {
            IReadOnlyList<LWorkItem> lTargets = LRosterTargetsRead(lItem, lChosen, LRosterRestartCheck);
            return (true, false, lTargets.Count, [],
                () => LRosterRestartRun(LStationEngine.LStationTransition!, lTargets));
        }

        if (LRosterCancelCheck(lItem))
        {
            IReadOnlyList<LWorkItem> lTargets = LRosterTargetsRead(lItem, lChosen, LRosterCancelCheck);
            return (false, true, lTargets.Count, [],
                () => LRosterCancelRun(LStationEngine.LStationTransition!, lStation, lTargets));
        }

        IReadOnlyList<string> lPaths = lItem.LWorkRunState.LWorkStateCurrent == LWorkState.LWorkStateDone
            ? LRosterPathsRead(lItem, lChosen)
            : [];
        return (false, false, lPaths.Count, lPaths, () => { });
    }

    private static bool LRosterRestartCheck(LWorkItem lWorkItem) =>
        lWorkItem.LWorkRunState.LWorkStateCurrent is LWorkState.LWorkStateCancelled or LWorkState.LWorkStateFailed;

    private static bool LRosterCancelCheck(LWorkItem lWorkItem) =>
        lWorkItem.LWorkRunState.LWorkStateCurrent is LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning;

    private static IReadOnlyList<LWorkItem> LRosterTargetsRead(
        LWorkItem lClicked, IReadOnlyList<LWorkItem> lChosen, Func<LWorkItem, bool> lStateCheck)
    {
        LWorkItem[] lTargets = lChosen.Where(lStateCheck).ToArray();
        return lTargets.Length == 0 || !lTargets.Any(lWorkItem => ReferenceEquals(lWorkItem, lClicked))
            ? [lClicked]
            : lTargets;
    }

    private static void LRosterRestartRun(LScheduleTransitionContract lSchedule, IReadOnlyList<LWorkItem> lTargets)
    {
        foreach (LWorkItem lWorkItem in lTargets)
        {
            lSchedule.LScheduleItemReset(lWorkItem.LWorkId);
        }
    }

    private static void LRosterCancelRun(
        LScheduleTransitionContract lSchedule, LStation lStation, IReadOnlyList<LWorkItem> lTargets)
    {
        foreach (LWorkItem lWorkItem in lTargets)
        {
            if (lWorkItem.LWorkRunState.LWorkStateCurrent == LWorkState.LWorkStateRunning)
            {
                lStation.LStationRunner.LRunnerJobCancel(lWorkItem.LWorkId);
            }
            else
            {
                lSchedule.LScheduleItemCancel(lWorkItem);
            }
        }
    }

    // A relay offers the outputs of the selection when the clicked item is part of a wider one, else its own output.
    private static IReadOnlyList<string> LRosterPathsRead(LWorkItem lClicked, IReadOnlyList<LWorkItem> lChosen)
    {
        IEnumerable<LWorkItem> lRelayItems =
            lChosen.Count > 1 && lChosen.Any(lWorkItem => ReferenceEquals(lWorkItem, lClicked))
                ? lChosen
                : [lClicked];
        var lRelayPaths = new List<string>();
        foreach (LWorkItem lRelayItem in lRelayItems)
        {
            if (lRelayItem.LWorkRunState.LWorkStateCurrent == LWorkState.LWorkStateDone
                && LRosterFileRead(lRelayItem) is { } lRelayPath
                && !lRelayPaths.Contains(lRelayPath, StringComparer.OrdinalIgnoreCase))
            {
                lRelayPaths.Add(lRelayPath);
            }
        }

        return lRelayPaths;
    }

    private static string? LRosterFileRead(LWorkItem lWorkItem) =>
        !string.IsNullOrWhiteSpace(lWorkItem.LWorkOutputPath)
        && LUsherDiskEngine.LUsherFileSeam(lWorkItem.LWorkOutputPath)
            ? lWorkItem.LWorkOutputPath
            : null;
}
