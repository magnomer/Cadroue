using System.IO;
using Cadroue.Core;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PFunnelTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new();
    private readonly PFunnelRules pFunnelRules = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PFunnelTab(LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        pFunnelRules.PFunnelOptionsSet(PFunnelTargetsRead);
        if (lPreferenceTabLayout?.LPreferenceFunnelRules is { Count: > 0 } pRuleRecords)
        {
            pFunnelRules.PFunnelRulesSeed(pRuleRecords);
        }

        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += _ => PFunnelDispatch(pList.PListItemRead() is { } pSelected
            ? new[] { pSelected }
            : Array.Empty<PListItem>());
        pAction.PActionAllAdd += () => PFunnelDispatch(pList.PListItemsRead());
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.FunnelAll.Tooltip"));
        pAction.PActionRelayHide();

        pList.PListPathChange += PFunnelPathShow;
        PTabViewerAttach(pList, pViewer);
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

    private void PFunnelDispatch(IReadOnlyList<PListItem> pItems)
    {
        if (pItems.Count == 0 || LTabset.LTabsetCurrent is not { } lTabset)
        {
            return;
        }

        var pRelayedPaths = new List<string>();
        var pRelayedTargets = new HashSet<Guid>();
        foreach (PListItem pItem in pItems)
        {
            string pFileName = Path.GetFileName(pItem.PListItemPath);
            foreach (PFunnelRuleRow pRow in pFunnelRules.PFunnelRulesRead())
            {
                if (!pRow.PFunnelRowMatch(pFileName) || pRow.PFunnelTargetId == Guid.Empty)
                {
                    continue;
                }

                PTabRecord? pTarget = lTabset.PTabsetRecords
                    .FirstOrDefault(pRecord => pRecord.PTabId == pRow.PFunnelTargetId);
                if (pTarget?.PTabWorkspace.PWorkspaceSurface.PTabList is { } pTargetList)
                {
                    pTargetList.PListPathsAdd(new[] { pItem.PListItemPath }, pItem.PListItemRelay);
                    pRelayedPaths.Add(pItem.PListItemPath);
                    pRelayedTargets.Add(pRow.PFunnelTargetId);
                }

                break;
            }
        }

        if (PProgram.LPreferenceStateCurrent.LPreferenceRelayEmpty && pRelayedPaths.Count > 0)
        {
            pList.PListPathsRemove(pRelayedPaths);
        }

        foreach (Guid pRelayedTarget in pRelayedTargets)
        {
            LCourier.LCourierAutoRelay(pRelayedTarget);
        }

        LTraceLog.LTraceInfoRecord(
            $"Funnel relayed {pRelayedPaths.Count} of {pItems.Count} file(s) by filename rule");
    }

    private IReadOnlyList<LCourierOption> PFunnelTargetsRead()
    {
        var pOptions = new List<LCourierOption>();
        if (LTabset.LTabsetCurrent is not { } lTabset)
        {
            return pOptions;
        }

        foreach (PTabRecord pRecord in lTabset.PTabsetRecords)
        {
            if (ReferenceEquals(pRecord.PTabWorkspace.PWorkspaceSurface, this)
                || pRecord.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            pOptions.Add(new LCourierOption(pRecord.PTabId, pRecord.PTabTitle, pRecord.PTabIconSource));
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

    public override LPreferenceTabLayoutRecord PTabLayoutRead()
    {
        LPreferenceTabLayoutRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        lPreferenceTabLayout.LPreferenceFunnelRules = pFunnelRules.PFunnelRulesRead()
            .Select(pRow =>
            {
                LPreferenceFunnelRuleRecord pRecord = pRow.PFunnelRecordCreate();
                pRecord.LPreferenceFunnelTarget = PFunnelTargetRead(pRow.PFunnelTargetId);
                return pRecord;
            })
            .ToList();
        return lPreferenceTabLayout;
    }

    private static int PFunnelTargetRead(Guid pTargetId)
    {
        if (pTargetId == Guid.Empty || LTabset.LTabsetCurrent is not { } lTabset)
        {
            return -1;
        }

        for (int pIndex = 0; pIndex < lTabset.PTabsetRecords.Count; pIndex++)
        {
            if (lTabset.PTabsetRecords[pIndex].PTabId == pTargetId)
            {
                return pIndex;
            }
        }

        return -1;
    }
}
