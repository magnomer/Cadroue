using System;
using System.Linq;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    public LSceneRecord PWindowSceneRead(string lSceneName)
    {
        var lScenePrefs = new LPreferenceState
        {
            LPreferenceLayoutKeys = lTabset.PTabsetRecords
                .Select(pTabRecord => pTabRecord.PTabLayoutKey)
                .ToList(),
            LPreferenceTabExports = lTabset.PTabsetRecords
                .Select(pTabRecord => LPresetRecord.LPresetRecordCreate(pTabRecord.PTabWorkspace.PWorkspaceExportState))
                .ToList(),
            LPreferenceTabLayouts = lTabset.PTabsetRecords
                .Select(pTabRecord => pTabRecord.PTabWorkspace.PWorkspaceLayoutRead())
                .ToList(),
            LPreferenceTabRelays = PMainArea.LCourier.LCourierSlotsRead(lTabset.PTabsetRecords).ToList(),
            LPreferenceTabNames = lTabset.PTabsetRecords
                .Select(pTabRecord => pTabRecord.PTabNameCustom)
                .ToList(),
            LPreferenceTabIndex = lTabset.PTabsetCurrent is null
                ? 0
                : Math.Max(0, lTabset.PTabsetRecords.IndexOf(lTabset.PTabsetCurrent))
        };

        return LSceneRecord.LSceneRecordCreate(lSceneName, lScenePrefs);
    }

    public void PWindowSceneApply(LSceneRecord lScene)
    {
        while (lTabset.PTabsetRecords.Count > 0)
        {
            lTabset.LTabsetClose(lTabset.PTabsetRecords[0]);
        }

        var lScenePrefs = new LPreferenceState { LPreferenceStartupMode = "LastSession" };
        lScene.LSceneStateApply(lScenePrefs);
        PWindowTabsRestore(lTabset, lScenePrefs);
    }
}
