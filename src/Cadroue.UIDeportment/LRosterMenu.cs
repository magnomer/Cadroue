using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record LRosterItem(
    string LRosterItemText,
    string LRosterItemIcon,
    bool LRosterItemEnabled,
    Action LRosterItemAction);

public sealed class LRosterMenu
{
    private static readonly IReadOnlyList<LRosterItem> LRosterMenuNone = [];

    private readonly LScheduleContract lSchedule;
    private readonly LStation lStation;
    private readonly Func<IReadOnlyList<LWorkItem>> lRosterSelectionSource;

    public LRosterMenu(
        LScheduleContract lScheduleOwner,
        LStation lStationOwner,
        Func<IReadOnlyList<LWorkItem>> lSelectionSource)
    {
        lSchedule = lScheduleOwner;
        lStation = lStationOwner;
        lRosterSelectionSource = lSelectionSource;
    }

    public IReadOnlyList<LRosterItem> LRosterMenuRead(Guid lId, LStrip? lStrip)
    {
        if (lSchedule.LScheduleRecords.FirstOrDefault(lWorkItem => lWorkItem.LWorkId == lId) is not { } lClicked)
        {
            return LRosterMenuNone;
        }

        if (LRosterRestartCheck(lClicked))
        {
            IReadOnlyList<LWorkItem> lTargets = LRosterTargetsRead(lClicked, LRosterRestartCheck);
            return [LRosterItemCreate(
                lTargets.Count, "Roster.Menu.Restart", "Roster.Menu.RestartMany", () => LRosterRestartRun(lTargets))];
        }

        if (LRosterCancelCheck(lClicked))
        {
            IReadOnlyList<LWorkItem> lTargets = LRosterTargetsRead(lClicked, LRosterCancelCheck);
            return [LRosterItemCreate(
                lTargets.Count, "Roster.Menu.Cancel", "Roster.Menu.CancelMany", () => LRosterCancelRun(lTargets))];
        }

        IReadOnlyList<string> lRelayPaths = LRosterPathsRead(lClicked);
        if (lClicked.LWorkStateCurrent != LWorkState.LWorkStateDone || lRelayPaths.Count == 0 || lStrip is null)
        {
            return LRosterMenuNone;
        }

        LRosterItem[] lTargetItems = lStrip.LStripTabs
            .Where(lStripTab => lStripTab.LStripTabDocket is not null)
            .Select(lStripTab => new LRosterItem(
                lStripTab.LStripTabTitle,
                lStripTab.LStripTabKey,
                true,
                () => LRosterRelayRun(lStrip, lStripTab, lRelayPaths)))
            .ToArray();
        if (lTargetItems.Length == 0)
        {
            return LRosterMenuNone;
        }

        LRosterItem lHeader = new(
            lRelayPaths.Count > 1
                ? LLocalization.LLocalizationFormat("Roster.Relay.Many", lRelayPaths.Count)
                : LLocalization.LLocalizationTextRead("Roster.Relay.One"),
            string.Empty,
            false,
            () => { });
        return [lHeader, .. lTargetItems];
    }

    private static bool LRosterRestartCheck(LWorkItem lWorkItem) =>
        lWorkItem.LWorkStateCurrent is LWorkState.LWorkStateCancelled or LWorkState.LWorkStateFailed;

    private static bool LRosterCancelCheck(LWorkItem lWorkItem) =>
        lWorkItem.LWorkStateCurrent is LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning;

    private IReadOnlyList<LWorkItem> LRosterTargetsRead(LWorkItem lClicked, Func<LWorkItem, bool> lStateCheck)
    {
        LWorkItem[] lTargets = lRosterSelectionSource().Where(lStateCheck).ToArray();
        return lTargets.Length == 0 || !lTargets.Any(lWorkItem => ReferenceEquals(lWorkItem, lClicked))
            ? [lClicked]
            : lTargets;
    }

    private static LRosterItem LRosterItemCreate(int lCount, string lOneKey, string lManyKey, Action lRun) => new(
        lCount > 1
            ? LLocalization.LLocalizationFormat(lManyKey, lCount)
            : LLocalization.LLocalizationTextRead(lOneKey),
        string.Empty,
        true,
        lRun);

    private void LRosterRestartRun(IReadOnlyList<LWorkItem> lTargets)
    {
        foreach (LWorkItem lWorkItem in lTargets)
        {
            lSchedule.LScheduleItemReset(lWorkItem.LWorkId);
        }
    }

    private void LRosterCancelRun(IReadOnlyList<LWorkItem> lTargets)
    {
        foreach (LWorkItem lWorkItem in lTargets)
        {
            if (lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning)
            {
                lStation.LStationRunner.LRunnerJobCancel(lWorkItem.LWorkId);
            }
            else
            {
                lSchedule.LScheduleItemCancel(lWorkItem);
            }
        }
    }

    private IReadOnlyList<string> LRosterPathsRead(LWorkItem lClicked)
    {
        IReadOnlyList<LWorkItem> lSelected = lRosterSelectionSource();
        IEnumerable<LWorkItem> lRelayItems =
            lSelected.Count > 1 && lSelected.Any(lWorkItem => ReferenceEquals(lWorkItem, lClicked))
                ? lSelected
                : [lClicked];
        var lRelayPaths = new List<string>();
        foreach (LWorkItem lRelayItem in lRelayItems)
        {
            if (lRelayItem.LWorkStateCurrent == LWorkState.LWorkStateDone
                && LRosterFileRead(lRelayItem) is { } lRelayPath
                && !lRelayPaths.Contains(lRelayPath, StringComparer.OrdinalIgnoreCase))
            {
                lRelayPaths.Add(lRelayPath);
            }
        }

        return lRelayPaths;
    }

    private static string? LRosterFileRead(LWorkItem lWorkItem) =>
        !string.IsNullOrWhiteSpace(lWorkItem.LWorkOutputPath) && LUsher.LUsherFileExist(lWorkItem.LWorkOutputPath)
            ? lWorkItem.LWorkOutputPath
            : null;

    private static void LRosterRelayRun(LStrip lStrip, LStripTab lTargetTab, IReadOnlyList<string> lRelayPaths)
    {
        if (lTargetTab.LStripTabDocket is not { } lTargetOwner)
        {
            return;
        }

        lStrip.LStripSelect(lTargetTab);
        lTargetOwner.LDocketPathsAdd(
            LMedia.LMediaPathScan(lRelayPaths).GetAwaiter().GetResult().LMediaScanPaths,
            LGate.LGateBatchCreate());
    }
}
