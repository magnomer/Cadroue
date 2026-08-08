using Cadroue.Core;
using System;
using System.Linq;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    public LSceneRecord PWindowSceneRead(string lSceneName) => new()
    {
        LSceneName = lSceneName,
        LSceneLayoutKeys = pStrip.PStripRecords
            .Select(pTabRecord => pTabRecord.PTabLayoutKey)
            .ToList(),
        LSceneTabExports = pStrip.PStripRecords
            .Select(pTabRecord => pTabRecord.PTabWorkspace.PWorkspaceExportState.LPresetRecordCreate())
            .ToList(),
        LSceneTabLayouts = pStrip.PStripRecords
            .Select(pTabRecord => pTabRecord.PTabWorkspace.PWorkspaceLayoutRead())
            .ToList(),
        LSceneTabRelays = PWindowRelayRead(pStrip.PStripRecords).ToList(),
        LSceneTabNames = pStrip.PStripRecords
            .Select(pTabRecord => pTabRecord.PTabNameCustom)
            .ToList(),
        LSceneTabIndex = pStrip.PStripSelected is null
            ? 0
            : Math.Max(0, pStrip.PStripRecords.IndexOf(pStrip.PStripSelected))
    };

    public void PWindowSceneApply(LSceneRecord lScene)
    {
        while (pStrip.PStripRecords.Count > 0)
        {
            pStrip.PStripClose(pStrip.PStripRecords[0]);
        }

        PWindowSceneRestore(pStrip, lScene);
    }

    private const int PWindowFinishSlot = -2;

    public static IReadOnlyList<int> PWindowRelayRead(IReadOnlyList<PTabRecord> pWindowTabRecords)
    {
        var pWindowSlots = new List<int>(pWindowTabRecords.Count);
        foreach (PTabRecord pTabRecord in pWindowTabRecords)
        {
            Guid pWindowTarget = LCartographer.LCartographerTargetRead(pTabRecord.PTabId);
            int pWindowSlot = pWindowTarget == LCartographer.LCartographerFinishTarget ? PWindowFinishSlot : -1;
            for (int pWindowIndex = 0; pWindowSlot == -1 && pWindowIndex < pWindowTabRecords.Count; pWindowIndex++)
            {
                if (pWindowTabRecords[pWindowIndex].PTabId == pWindowTarget)
                {
                    pWindowSlot = pWindowIndex;
                    break;
                }
            }

            pWindowSlots.Add(pWindowSlot);
        }

        return pWindowSlots;
    }

    public static void PWindowRelayApply(
        IReadOnlyList<PTabRecord> pWindowTabRecords,
        IReadOnlyList<int> pWindowSlots)
    {
        for (int pWindowIndex = 0; pWindowIndex < pWindowTabRecords.Count; pWindowIndex++)
        {
            if (pWindowIndex >= pWindowSlots.Count)
            {
                break;
            }

            int pWindowSlot = pWindowSlots[pWindowIndex];
            if (pWindowSlot == PWindowFinishSlot)
            {
                PTabRecord pWindowFinishSource = pWindowTabRecords[pWindowIndex];
                LCartographer.LCartographerTargetSet(pWindowFinishSource.PTabId, LCartographer.LCartographerFinishTarget);
                pWindowFinishSource.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(LCartographer.LCartographerFinishTarget);
                continue;
            }

            if (pWindowSlot < 0 || pWindowSlot >= pWindowTabRecords.Count || pWindowSlot == pWindowIndex)
            {
                continue;
            }

            PTabRecord pWindowSource = pWindowTabRecords[pWindowIndex];
            PTabRecord pWindowTarget = pWindowTabRecords[pWindowSlot];
            if (pWindowTarget.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            LCartographer.LCartographerTargetSet(pWindowSource.PTabId, pWindowTarget.PTabId);
            pWindowSource.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(pWindowTarget.PTabId);
        }
    }
}
