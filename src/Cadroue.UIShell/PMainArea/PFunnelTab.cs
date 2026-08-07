using System.IO;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

using Cadroue.Infrastructure;
using Cadroue.MigrationInterface;

namespace Cadroue.UIShell.PMainArea;

public sealed class PFunnelTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new(new LDocket());
    private readonly PFunnelRules pFunnelRules = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PFunnelTab(LSceneTabRecord? lPreferenceTabLayout = null)
    {
        pFunnelRules.PFunnelOptionsSet(PFunnelTargetsRead);
        if (lPreferenceTabLayout?.LSceneFunnelRules is { Count: > 0 } pRuleRecords)
        {
            pFunnelRules.PFunnelRulesSeed(pRuleRecords);
        }

        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += _ => PFunnelDispatch(pList.PListItemRead() is { } pSelected
            ? new[] { pSelected }
            : Array.Empty<LDocketEntry>());
        pAction.PActionAllAdd += () => PFunnelDispatch(pList.PListItemsRead());
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.FunnelAll.Tooltip"));
        pAction.PActionRelayHide();

        pList.PListPathChange += PFunnelPathShow;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pFunnelRules, pViewer },
            new PCompass(pFlow),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
    }

    public void PFunnelTargetsResolve(IReadOnlyList<PTabRecord> pTabRecords) =>
        pFunnelRules.PFunnelTargetsResolve(pTabRecords);

    private void PFunnelDispatch(IReadOnlyList<LDocketEntry> pItems)
    {
        if (pItems.Count == 0 || PStrip.PStripCurrent is not { } pStrip)
        {
            return;
        }

        var pRelayedPaths = new List<string>();
        var pRelayedRoutes = new List<(Guid pFunnelTarget, string pFunnelPath, Guid pFunnelCohort)>();
        foreach (LDocketEntry pItem in pItems)
        {
            string pFileName = Path.GetFileName(pItem.LDocketEntryPath);
            foreach (PFunnelRuleRow pRow in pFunnelRules.PFunnelRulesRead())
            {
                if (!pRow.PFunnelRowMatch(pFileName) || pRow.PFunnelTargetId == Guid.Empty)
                {
                    continue;
                }

                PTabRecord? pTarget = pStrip.PStripRecords
                    .FirstOrDefault(pRecord => pRecord.PTabId == pRow.PFunnelTargetId);
                if (pTarget?.PTabWorkspace.PWorkspaceSurface.PTabList?.PListDocketRead() is { } pTargetOwner)
                {
                    pTargetOwner.LDocketPathsAdd(
                        PList.PListMediaScan(new[] { pItem.LDocketEntryPath }), pItem.LDocketEntryBatch, true);
                    pRelayedPaths.Add(pItem.LDocketEntryPath);
                    pRelayedRoutes.Add((pRow.PFunnelTargetId, pItem.LDocketEntryPath, pItem.LDocketEntryBatch));
                }

                break;
            }
        }

        if (LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty && pRelayedPaths.Count > 0)
        {
            pList.PListDocketRead().LDocketPathsRemove(pRelayedPaths);
        }

        foreach ((Guid pFunnelTarget, string pFunnelPath, Guid pFunnelCohort) in pRelayedRoutes)
        {
            PAction.PActionArrive(pFunnelTarget, pFunnelPath, pFunnelCohort);
        }

        LSeal.LSealSweep();

        LTraceLog.LTraceInfoRecord(
            $"Funnel relayed {pRelayedPaths.Count} of {pItems.Count} file(s) by filename rule");
    }

    private IReadOnlyList<PActionRelayOption> PFunnelTargetsRead()
    {
        var pOptions = new List<PActionRelayOption>();
        if (PStrip.PStripCurrent is not { } pStrip)
        {
            return pOptions;
        }

        foreach (PTabRecord pRecord in pStrip.PStripRecords)
        {
            if (ReferenceEquals(pRecord.PTabWorkspace.PWorkspaceSurface, this)
                || pRecord.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            pOptions.Add(new PActionRelayOption(pRecord.PTabId, pRecord.PTabTitle, pRecord.PTabIconSource));
        }

        return pOptions;
    }

    private void PFunnelPathShow(string? pSourcePath)
    {
        if (!string.IsNullOrWhiteSpace(pSourcePath))
        {
            pViewer.PViewerSourceOpen(pSourcePath);
        }
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;

    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        lPreferenceTabLayout.LSceneFunnelRules = pFunnelRules.PFunnelRulesRead()
            .Select(pRow =>
            {
                LSceneFunnelRule pRecord = pRow.PFunnelRecordCreate();
                pRecord.LSceneFunnelTarget = PFunnelTargetRead(pRow.PFunnelTargetId);
                return pRecord;
            })
            .ToList();
        return lPreferenceTabLayout;
    }

    private static int PFunnelTargetRead(Guid pTargetId)
    {
        if (pTargetId == Guid.Empty || PStrip.PStripCurrent is not { } pStrip)
        {
            return -1;
        }

        for (int pIndex = 0; pIndex < pStrip.PStripRecords.Count; pIndex++)
        {
            if (pStrip.PStripRecords[pIndex].PTabId == pTargetId)
            {
                return pIndex;
            }
        }

        return -1;
    }
}
