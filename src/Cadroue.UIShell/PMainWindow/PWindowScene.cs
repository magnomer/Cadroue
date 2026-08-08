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

        PStrip.PStripRestoring = true;
        try
        {
            PWindowSceneRestore(pStrip, lScene);
        }
        finally
        {
            PStrip.PStripRestoring = false;
        }
    }

    public static IReadOnlyList<int> PWindowRelayRead(IReadOnlyList<PTabRecord> pWindowTabRecords)
    {
        List<Guid> pWindowTabIds = pWindowTabRecords
            .Select(pTabRecord => pTabRecord.PTabId)
            .ToList();
        return LCartographer.LCartographerSlotResolve(pWindowTabIds);
    }

    public static void PWindowRelayApply(
        IReadOnlyList<PTabRecord> pWindowTabRecords,
        IReadOnlyList<int> pWindowSlots)
    {
        List<Guid> pWindowTabIds = pWindowTabRecords
            .Select(pTabRecord => pTabRecord.PTabId)
            .ToList();

        foreach ((Guid pWindowSource, Guid pWindowTarget) in
            LCartographer.LCartographerAssignmentResolve(pWindowTabIds, pWindowSlots))
        {
            if (pWindowTarget == LCartographer.LCartographerFinishTarget)
            {
                PTabRecord? pWindowFinishSource = pWindowTabRecords.FirstOrDefault(pTabRecord => pTabRecord.PTabId == pWindowSource);
                if (pWindowFinishSource is null)
                {
                    continue;
                }

                LCartographer.LCartographerTargetSet(pWindowSource, LCartographer.LCartographerFinishTarget);
                pWindowFinishSource.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(LCartographer.LCartographerFinishTarget);
                continue;
            }

            PTabRecord? pWindowSourceRecord = pWindowTabRecords.FirstOrDefault(pTabRecord => pTabRecord.PTabId == pWindowSource);
            PTabRecord? pWindowTargetRecord = pWindowTabRecords.FirstOrDefault(pTabRecord => pTabRecord.PTabId == pWindowTarget);
            if (pWindowSourceRecord is null || pWindowTargetRecord is null)
            {
                continue;
            }

            if (pWindowTargetRecord.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            LCartographer.LCartographerTargetSet(pWindowSource, pWindowTarget);
            pWindowSourceRecord.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(pWindowTarget);
        }
    }
}
