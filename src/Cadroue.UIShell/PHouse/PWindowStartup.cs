using Cadroue.Infrastructure;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Cadroue.Media;
using Cadroue.UIShell.PToolbar;
using Cadroue.UIShell.PPanel;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.Core;
using Cadroue.Application;

namespace Cadroue.UIShell.PHouse;

public partial class PWindow
{
    private static void PWindowTabsRestore(PStrip pTabset, LPreferenceState lPreferenceState, LSceneRecord lScene)
    {
        LTrace.LTraceLoadingSet(true);
        try
        {
            if (lPreferenceState.LPreferenceStartupMode == "DefaultTab")
            {
                foreach (string pStartupKey in lPreferenceState.LPreferenceStartupTabs)
                {
                    pTabset.PStripAdd(pStartupKey);
                }

                if (pTabset.PStripRecords.Count > 0)
                {
                    pTabset.PStripSelect(pTabset.PStripRecords[0]);
                }

                PWindowLoadingRecord(pTabset);
                return;
            }

            PWindowSceneRestore(pTabset, lScene);
            PWindowLoadingRecord(pTabset, lScene.LSceneName);
        }
        finally
        {
            LTrace.LTraceLoadingSet(false);
        }
    }

    private static void PWindowLoadingRecord(PStrip pTabset, string? pSceneName = null)
    {
        if (!string.IsNullOrWhiteSpace(pSceneName))
        {
            LTraceLog.LTraceLoadingRecord($"Scene \"{pSceneName}\" loaded");
        }

        int pTabCount = pTabset.PStripRecords.Count;
        string? pTabDetail = pTabCount == 0
            ? null
            : string.Join(
                Environment.NewLine,
                pTabset.PStripRecords.Select(
                    pTabRecord => $"Tab '{pTabRecord.PTabTitle}' ({pTabRecord.PTabLayoutKey}) opened"));
        LTraceLog.LTraceLoadingRecord(
            $"{pTabCount} {(pTabCount == 1 ? "tab" : "tabs")} opened",
            pTabDetail);
    }

    private static void PWindowSceneRestore(PStrip pTabset, LSceneRecord lScene)
    {
        IReadOnlyList<string> pTabKeys = lScene.LSceneDefaultTabs
            ? new[] { "Split", "Edit", "Audio", "Convert", "Merge", "Worklist" }
            : lScene.LSceneLayoutKeys;
        IReadOnlyList<LPresetRecord> pTabExports = lScene.LSceneTabExports;
        IReadOnlyList<LSceneTabRecord> pTabLayouts = lScene.LSceneTabLayouts;
        pTabset.PStripUpdateSuspend();
        try
        {
            for (int pTabIndex = 0; pTabIndex < pTabKeys.Count; pTabIndex++)
            {
                LPreset? pTabExportState = pTabIndex < pTabExports.Count
                    ? LPreset.LPresetStateCreate(pTabExports[pTabIndex])
                    : null;
                LSceneTabRecord? pTabLayout = pTabIndex < pTabLayouts.Count ? pTabLayouts[pTabIndex] : null;
                PTabRecord pTabRestored = pTabset.PStripAdd(pTabKeys[pTabIndex], pTabExportState, pTabLayout);
                if (pTabIndex < lScene.LSceneTabNames.Count)
                {
                    pTabset.PStripNameSet(pTabRestored, lScene.LSceneTabNames[pTabIndex]);
                }
            }
        }
        finally
        {
            pTabset.PStripUpdateResume();
        }
        PWindowRelayApply(pTabset.PStripRecords, lScene.LSceneTabRelays);
        pTabset.PStripTitleUpdate();
        foreach (PTabRecord pTabRecord in pTabset.PStripRecords)
        {
            if (pTabRecord.PTabWorkspace.PWorkspaceSurface is PDeck.PFunnelTab pFunnelSurface)
            {
                pFunnelSurface.PFunnelTargetsResolve(pTabset.PStripRecords);
            }
        }
        if (pTabset.PStripRecords.Count == 0)
        {
            pTabset.PStripSelect(null);
            return;
        }

        int pSelectIndex = Math.Clamp(lScene.LSceneTabIndex, 0, pTabset.PStripRecords.Count - 1);
        pTabset.PStripSelect(pTabset.PStripRecords[pSelectIndex]);
    }

}
