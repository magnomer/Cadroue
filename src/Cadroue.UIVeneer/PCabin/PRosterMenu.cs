using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PPorch;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PRoster
{
    private void PRosterMenuOpen(Border pRow, ContextMenuEventArgs pArgs)
    {
        if (pRow.Tag is not LWorkItem pWorkItem || pRow.ContextMenu is not { } pMenu)
        {
            pArgs.Handled = true;
            return;
        }

        if (pWorkItem.LWorkStateCurrent is LWorkState.LWorkStateCancelled or LWorkState.LWorkStateFailed)
        {
            PRosterRestartBuild(pMenu, pWorkItem);
            return;
        }

        if (pWorkItem.LWorkStateCurrent is LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning)
        {
            PRosterCancelBuild(pMenu, pWorkItem);
            return;
        }

        IReadOnlyList<string> pRelayPaths = PRosterPathsRead(pWorkItem);
        if (pWorkItem.LWorkStateCurrent != LWorkState.LWorkStateDone
            || pRelayPaths.Count == 0
            || PWindow.PWindowStripRead() is not { } pTabset)
        {
            pArgs.Handled = true;
            return;
        }

        pMenu.Items.Clear();
        MenuItem pHeader = PMenu.PMenuItemCreate(
            pRelayPaths.Count > 1
                ? LLocalization.LLocalizationFormat("Roster.Relay.Many", pRelayPaths.Count)
                : LLocalization.LLocalizationTextRead("Roster.Relay.One"));
        pHeader.IsEnabled = false;
        pMenu.Items.Add(pHeader);

        bool pAnyTarget = false;
        foreach (LStripTab lStripTab in pTabset.LStrip.LStripTabs)
        {
            if (lStripTab.LStripTabDocket is null)
            {
                continue;
            }

            pAnyTarget = true;
            LStripTab lTargetTab = lStripTab;
            MenuItem pItem = PMenu.PMenuItemCreate(
                lStripTab.LStripTabTitle, PTabIcon.PTabIconRead(lStripTab.LStripTabKey));
            pItem.Click += (_, _) => PRosterRelaySend(lTargetTab, pRelayPaths);
            pMenu.Items.Add(pItem);
        }

        if (!pAnyTarget)
        {
            pArgs.Handled = true;
        }
    }

    private void PRosterRestartBuild(ContextMenu pMenu, LWorkItem pClickedItem)
    {
        LWorkItem[] pRestartItems = PRosterSelectionRead()
            .Where(pItem => pItem.LWorkStateCurrent is LWorkState.LWorkStateCancelled or LWorkState.LWorkStateFailed)
            .ToArray();
        if (pRestartItems.Length == 0 || !pRestartItems.Any(pItem => ReferenceEquals(pItem, pClickedItem)))
        {
            pRestartItems = new[] { pClickedItem };
        }

        pMenu.Items.Clear();
        MenuItem pRestart = PMenu.PMenuItemCreate(
            pRestartItems.Length > 1
                ? LLocalization.LLocalizationFormat("Roster.Menu.RestartMany", pRestartItems.Length)
                : LLocalization.LLocalizationTextRead("Roster.Menu.Restart"));
        LWorkItem[] pRestartTargets = pRestartItems;
        pRestart.Click += (_, _) =>
        {
            foreach (LWorkItem pRestartItem in pRestartTargets)
            {
                LProgram.LScheduleCurrent.LScheduleItemReset(pRestartItem.LWorkId);
            }
        };
        pMenu.Items.Add(pRestart);
    }

    private void PRosterCancelBuild(ContextMenu pMenu, LWorkItem pClickedItem)
    {
        LWorkItem[] pCancelItems = PRosterSelectionRead()
            .Where(pItem => pItem.LWorkStateCurrent is LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning)
            .ToArray();
        if (pCancelItems.Length == 0 || !pCancelItems.Any(pItem => ReferenceEquals(pItem, pClickedItem)))
        {
            pCancelItems = new[] { pClickedItem };
        }

        pMenu.Items.Clear();
        MenuItem pCancel = PMenu.PMenuItemCreate(
            pCancelItems.Length > 1
                ? LLocalization.LLocalizationFormat("Roster.Menu.CancelMany", pCancelItems.Length)
                : LLocalization.LLocalizationTextRead("Roster.Menu.Cancel"));
        LWorkItem[] pCancelTargets = pCancelItems;
        pCancel.Click += (_, _) =>
        {
            foreach (LWorkItem pCancelItem in pCancelTargets)
            {
                PRosterItemCancel(pCancelItem);
            }
        };
        pMenu.Items.Add(pCancel);
    }

    private void PRosterItemCancel(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning)
        {
            pRosterStation.LStationRunner.LRunnerJobCancel(pWorkItem.LWorkId);
            return;
        }

        pRosterSchedule.LScheduleItemCancel(pWorkItem);
    }

    private IReadOnlyList<string> PRosterPathsRead(LWorkItem pClickedItem)
    {
        IReadOnlyList<LWorkItem> pSelectedItems = PRosterSelectionRead();
        IEnumerable<LWorkItem> pRelayItems =
            pSelectedItems.Count > 1 && pSelectedItems.Any(pItem => ReferenceEquals(pItem, pClickedItem))
                ? pSelectedItems
                : new[] { pClickedItem };

        var pRelayPaths = new List<string>();
        foreach (LWorkItem pRelayItem in pRelayItems)
        {
            if (pRelayItem.LWorkStateCurrent == LWorkState.LWorkStateDone
                && PRosterFileRead(pRelayItem) is { } pRelayPath
                && !pRelayPaths.Contains(pRelayPath, StringComparer.OrdinalIgnoreCase))
            {
                pRelayPaths.Add(pRelayPath);
            }
        }

        return pRelayPaths;
    }

    private static string? PRosterFileRead(LWorkItem pWorkItem) =>
        !string.IsNullOrWhiteSpace(pWorkItem.LWorkOutputPath) && LUsher.LUsherFileExist(pWorkItem.LWorkOutputPath)
            ? pWorkItem.LWorkOutputPath
            : null;

    private static void PRosterRelaySend(LStripTab lTargetTab, IReadOnlyList<string> pRelayPaths)
    {
        if (lTargetTab.LStripTabDocket is not { } pTargetOwner)
        {
            return;
        }

        PWindow.PWindowStripRead()?.LStrip.LStripSelect(lTargetTab);
        pTargetOwner.LDocketPathsAdd(
            Cadroue.Media.LMedia.LMediaPathScan(pRelayPaths).GetAwaiter().GetResult().LMediaScanPaths,
            Cadroue.Application.LGate.LGateBatchCreate());
    }
}
