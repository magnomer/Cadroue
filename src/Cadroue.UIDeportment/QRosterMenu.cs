using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record QRosterItem(
    string QRosterItemText,
    string QRosterItemIcon,
    bool QRosterItemEnabled,
    Action QRosterItemAction);

public sealed class QRosterMenu
{
    private static readonly IReadOnlyList<QRosterItem> QRosterMenuNone = [];

    private readonly LScheduleContract qSchedule;
    private readonly LStation qStation;
    private readonly Func<IReadOnlyList<LWorkItem>> qRosterSelectionSource;

    private readonly CUsher qRosterUsher;

    public QRosterMenu(
        CUsher lUsher,
        LScheduleContract lScheduleOwner,
        LStation lStationOwner,
        Func<IReadOnlyList<LWorkItem>> lSelectionSource)
    {
        qRosterUsher = lUsher;
        qSchedule = lScheduleOwner;
        qStation = lStationOwner;
        qRosterSelectionSource = lSelectionSource;
    }

    public IReadOnlyList<QRosterItem> QRosterMenuRead(Guid lId, QStrip? lStrip)
    {
        if (qSchedule.LScheduleRecords.FirstOrDefault(lWorkItem => lWorkItem.LWorkId == lId) is not { } lClicked)
        {
            return QRosterMenuNone;
        }

        if (QRosterRestartCheck(lClicked))
        {
            IReadOnlyList<LWorkItem> lTargets = QRosterTargetsRead(lClicked, QRosterRestartCheck);
            return [QRosterItemCreate(
                lTargets.Count, "Roster.Menu.Restart", "Roster.Menu.RestartMany", () => QRosterRestartRun(lTargets))];
        }

        if (QRosterCancelCheck(lClicked))
        {
            IReadOnlyList<LWorkItem> lTargets = QRosterTargetsRead(lClicked, QRosterCancelCheck);
            return [QRosterItemCreate(
                lTargets.Count, "Roster.Menu.Cancel", "Roster.Menu.CancelMany", () => QRosterCancelRun(lTargets))];
        }

        IReadOnlyList<string> lRelayPaths = QRosterPathsRead(lClicked);
        if (lClicked.LWorkStateCurrent != LWorkState.LWorkStateDone || lRelayPaths.Count == 0 || lStrip is null)
        {
            return QRosterMenuNone;
        }

        QRosterItem[] lTargetItems = lStrip.QStripTabs
            .Where(lStripTab => lStripTab.QStripTabDocket is not null)
            .Select(lStripTab => new QRosterItem(
                lStripTab.QStripTabTitle,
                lStripTab.QStripTabKey,
                true,
                () => _ = QRosterRelayRun(lStrip, lStripTab, lRelayPaths)))
            .ToArray();
        if (lTargetItems.Length == 0)
        {
            return QRosterMenuNone;
        }

        QRosterItem lHeader = new(
            lRelayPaths.Count > 1
                ? QLocalizationCatalog.QLocalizationFormat("Roster.Relay.Many", lRelayPaths.Count)
                : QLocalizationCatalog.QLocalizationTextRead("Roster.Relay.One"),
            string.Empty,
            false,
            () => { });
        return [lHeader, .. lTargetItems];
    }

    private static bool QRosterRestartCheck(LWorkItem lWorkItem) =>
        lWorkItem.LWorkStateCurrent is LWorkState.LWorkStateCancelled or LWorkState.LWorkStateFailed;

    private static bool QRosterCancelCheck(LWorkItem lWorkItem) =>
        lWorkItem.LWorkStateCurrent is LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning;

    private IReadOnlyList<LWorkItem> QRosterTargetsRead(LWorkItem lClicked, Func<LWorkItem, bool> lStateCheck)
    {
        LWorkItem[] lTargets = qRosterSelectionSource().Where(lStateCheck).ToArray();
        return lTargets.Length == 0 || !lTargets.Any(lWorkItem => ReferenceEquals(lWorkItem, lClicked))
            ? [lClicked]
            : lTargets;
    }

    private static QRosterItem QRosterItemCreate(int lCount, string lOneKey, string lManyKey, Action lRun) => new(
        lCount > 1
            ? QLocalizationCatalog.QLocalizationFormat(lManyKey, lCount)
            : QLocalizationCatalog.QLocalizationTextRead(lOneKey),
        string.Empty,
        true,
        lRun);

    private void QRosterRestartRun(IReadOnlyList<LWorkItem> lTargets)
    {
        foreach (LWorkItem lWorkItem in lTargets)
        {
            qSchedule.LScheduleItemReset(lWorkItem.LWorkId);
        }
    }

    private void QRosterCancelRun(IReadOnlyList<LWorkItem> lTargets)
    {
        foreach (LWorkItem lWorkItem in lTargets)
        {
            if (lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning)
            {
                qStation.LStationRunner.LRunnerJobCancel(lWorkItem.LWorkId);
            }
            else
            {
                qSchedule.LScheduleItemCancel(lWorkItem);
            }
        }
    }

    private IReadOnlyList<string> QRosterPathsRead(LWorkItem lClicked)
    {
        IReadOnlyList<LWorkItem> lSelected = qRosterSelectionSource();
        IEnumerable<LWorkItem> lRelayItems =
            lSelected.Count > 1 && lSelected.Any(lWorkItem => ReferenceEquals(lWorkItem, lClicked))
                ? lSelected
                : [lClicked];
        var lRelayPaths = new List<string>();
        foreach (LWorkItem lRelayItem in lRelayItems)
        {
            if (lRelayItem.LWorkStateCurrent == LWorkState.LWorkStateDone
                && QRosterFileRead(lRelayItem) is { } lRelayPath
                && !lRelayPaths.Contains(lRelayPath, StringComparer.OrdinalIgnoreCase))
            {
                lRelayPaths.Add(lRelayPath);
            }
        }

        return lRelayPaths;
    }

    private string? QRosterFileRead(LWorkItem lWorkItem) =>
        !string.IsNullOrWhiteSpace(lWorkItem.LWorkOutputPath) && qRosterUsher.CUsherFileExist(lWorkItem.LWorkOutputPath)
            ? lWorkItem.LWorkOutputPath
            : null;

    private static async Task QRosterRelayRun(
        QStrip lStrip, QStripTab lTargetTab, IReadOnlyList<string> lRelayPaths)
    {
        if (lTargetTab.QStripTabDocket is not { } lTargetOwner)
        {
            return;
        }

        lStrip.QStripSelect(lTargetTab);
        lTargetOwner.LDocketPathsAdd(
            (await LMedia.LMediaPathScan(lRelayPaths).ConfigureAwait(true)).LMediaScanPaths,
            LGate.LGateBatchCreate());
    }
}
