using System.IO;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

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

        IReadOnlyList<string> pRelayPaths = PRosterPathsRead(pWorkItem);
        if (pWorkItem.LWorkStateCurrent != LWorkState.LWorkStateDone
            || pRelayPaths.Count == 0
            || PStrip.PStripCurrent is not { } pTabset)
        {
            pArgs.Handled = true;
            return;
        }

        pMenu.Items.Clear();
        MenuItem pHeader = PMenu.PMenuItemCreate(
            pRelayPaths.Count > 1
                ? LLocalization.LLocalizationFormat("Roster.Relay.Many", pRelayPaths.Count)
                : LLocalization.LLocalizationTextRead("Roster.Relay.One"), null);
        pHeader.IsEnabled = false;
        pMenu.Items.Add(pHeader);

        bool pAnyTarget = false;
        foreach (PTabRecord pTabRecord in pTabset.PStripRecords)
        {
            if (pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            pAnyTarget = true;
            PTabRecord pTargetRecord = pTabRecord;
            MenuItem pItem = PMenu.PMenuItemCreate(pTabRecord.PTabTitle, pTabRecord.PTabIconSource);
            pItem.Click += (_, _) => PRosterRelaySend(pTargetRecord, pRelayPaths);
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
                : LLocalization.LLocalizationTextRead("Roster.Menu.Restart"),
            null);
        LWorkItem[] pRestartTargets = pRestartItems;
        pRestart.Click += (_, _) =>
        {
            foreach (LWorkItem pRestartItem in pRestartTargets)
            {
                PProgram.LScheduleCurrent.LScheduleItemReset(pRestartItem.LWorkId);
            }
        };
        pMenu.Items.Add(pRestart);
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

    private static string? PRosterFileRead(LWorkItem pWorkItem)
    {
        if (!string.IsNullOrWhiteSpace(pWorkItem.LWorkOutputPath) && File.Exists(pWorkItem.LWorkOutputPath))
        {
            return pWorkItem.LWorkOutputPath;
        }

        return !string.IsNullOrWhiteSpace(pWorkItem.LWorkSourcePath) && File.Exists(pWorkItem.LWorkSourcePath)
            ? pWorkItem.LWorkSourcePath
            : null;
    }

    private static void PRosterRelaySend(PTabRecord pTargetRecord, IReadOnlyList<string> pRelayPaths)
    {
        if (pTargetRecord.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is not { } pTargetOwner)
        {
            return;
        }

        PStrip.PStripCurrent?.PStripSelect(pTargetRecord);
        string[] pClearPaths = pTargetOwner.LDocketUnlockedRead()
            .Select(pEntry => pEntry.LDocketEntryPath)
            .ToArray();
        if (pClearPaths.Length > 0)
        {
            pTargetOwner.LDocketPathsRemove(pClearPaths);
        }

        pTargetOwner.LDocketPathsAdd(
            PList.PListMediaScan(pRelayPaths), Cadroue.Application.LGate.LGateBatchCreate());
    }
}
