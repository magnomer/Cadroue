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
        LSceneLayoutKeys = pStrip.PStripRecords
            .Select(pTabRecord => pTabRecord.PTabLayoutKey)
            .ToList(),
        LSceneTabExports = pStrip.PStripRecords
            .Select(pTabRecord => pTabRecord.PTabWorkspace.PWorkspaceExportState.LPresetRecordCreate())
            .ToList(),
        LSceneTabLayouts = pStrip.PStripRecords
            .Select(pTabRecord => pTabRecord.PTabWorkspace.PWorkspaceLayoutRead())
            .ToList(),
        LSceneTabRelays = PMainArea.LCourier.LCourierSlotsRead(pStrip.PStripRecords).ToList(),
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
}
