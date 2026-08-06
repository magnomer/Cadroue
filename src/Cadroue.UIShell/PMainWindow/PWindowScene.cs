using Cadroue.Core;
using System;
using System.Linq;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;
using Cadroue.MigrationInterface;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    public LSceneRecord PWindowSceneRead(string lSceneName) => new()
    {
        LSceneName = lSceneName,
        LSceneLayoutKeys = lTabset.PTabsetRecords
            .Select(pTabRecord => pTabRecord.PTabLayoutKey)
            .ToList(),
        LSceneTabExports = lTabset.PTabsetRecords
            .Select(pTabRecord => pTabRecord.PTabWorkspace.PWorkspaceExportState.LPresetRecordCreate())
            .ToList(),
        LSceneTabLayouts = lTabset.PTabsetRecords
            .Select(pTabRecord => pTabRecord.PTabWorkspace.PWorkspaceLayoutRead())
            .ToList(),
        LSceneTabRelays = PMainArea.LCourier.LCourierSlotsRead(lTabset.PTabsetRecords).ToList(),
        LSceneTabNames = lTabset.PTabsetRecords
            .Select(pTabRecord => pTabRecord.PTabNameCustom)
            .ToList(),
        LSceneTabIndex = lTabset.PTabsetCurrent is null
            ? 0
            : Math.Max(0, lTabset.PTabsetRecords.IndexOf(lTabset.PTabsetCurrent))
    };

    public void PWindowSceneApply(LSceneRecord lScene)
    {
        while (lTabset.PTabsetRecords.Count > 0)
        {
            lTabset.LTabsetClose(lTabset.PTabsetRecords[0]);
        }

        PWindowSceneRestore(lTabset, lScene);
    }
}
