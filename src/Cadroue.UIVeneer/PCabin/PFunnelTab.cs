using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PPorch;
using Cadroue.UIVeneer.PWing;
using Cadroue.UIDeportment;

using Cadroue.ShellEngine;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PFunnelTab : PTabSurface
{
    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new(new LDocket());
    private readonly PFunnelRules pFunnelRules = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PFunnelTab(LSceneTabRecord? lPreferenceTabLayout = null)
    {
        pFunnelRules.PFunnelOptionsSet(PFunnelTargetsRead);
        if (lPreferenceTabLayout is { } pLayout)
        {
            pFunnelRules.LFunnel.LFunnelRulesRestore(pLayout.LSceneFunnelRules);
        }

        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += _ => PFunnelDispatch(pList.PListItemRead() is { } pSelected
            ? new[] { pSelected }
            : Array.Empty<LDocketEntry>());
        pAction.PActionAllAdd += () => PFunnelDispatch(pList.PListItemsRead());
        pAction.PActionItemsAdd += pFunnelPaths => PFunnelDispatch(
            pList.PListItemsRead()
                .Where(pItem => pFunnelPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .ToArray());
        pAction.PActionListAttach(pList);
        pAction.PActionEligibleSource = pList.PListPathsRead;
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.FunnelAll.Tooltip"));
        pAction.PActionRelayHide();

        pList.PListPathChange += PFunnelPathShow;
        pViewer.PDropPathsChange += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pFunnelRules, pViewer },
            new PCompass(pFlow, pViewer),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
    }

    public void PFunnelTargetsResolve(IReadOnlyList<LStripTab> lStripTabs) =>
        pFunnelRules.LFunnel.LFunnelTargetsResolve(lStripTabs.Select(lTab => lTab.LStripTabId).ToArray());

    private void PFunnelDispatch(IReadOnlyList<LDocketEntry> pItems)
    {
        if (pItems.Count == 0)
        {
            return;
        }

        LFunnel lFunnel = pFunnelRules.LFunnel;
        IReadOnlyList<LFunnelRule> lRows = lFunnel.LFunnelRules;
        var pRules = lRows.Select(lFunnel.LFunnelRecordCreate).ToList();
        var pLiveTargets = PFunnelTargetsRead().Select(pOption => pOption.PActionRelayId).ToHashSet();
        var pTargets = lRows
            .Select(lRule => pLiveTargets.Contains(lRule.LFunnelRuleTarget) ? lRule.LFunnelRuleTarget : Guid.Empty)
            .ToList();
        var pDispatchItems = pItems
            .Select(pItem => (pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
            .ToList();

        LMessenger.LMessengerFunnelDescribe(PTabAction!.PActionSourceTab, pRules, pTargets, pDispatchItems);
    }

    private IReadOnlyList<PActionRelayOption> PFunnelTargetsRead()
    {
        var pOptions = new List<PActionRelayOption>();
        if (PWindow.PWindowStripRead() is not { } pStrip)
        {
            return pOptions;
        }

        foreach (LStripTab lTab in pStrip.LStrip.LStripTabs)
        {
            if (ReferenceEquals(pStrip.PStripWorkspaceRead(lTab)?.PWorkspaceSurface, this)
                || lTab.LStripTabDocket is null)
            {
                continue;
            }

            pOptions.Add(new PActionRelayOption(
                lTab.LStripTabId, lTab.LStripTabTitle, PTabIcon.PTabIconRead(lTab.LStripTabKey)));
        }

        return pOptions;
    }

    private void PFunnelPathShow(string? pSourcePath) => pViewer.LViewer.LViewerPathHandle(pSourcePath);

    public override PFlow PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;

    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutCreate();
        lPreferenceTabLayout.LSceneFunnelRules = pFunnelRules.LFunnel.LFunnelRules
            .Select(lRule =>
            {
                LSceneFunnelRule pRecord = pFunnelRules.LFunnel.LFunnelRecordCreate(lRule);
                pRecord.LSceneFunnelTarget = PFunnelTargetRead(lRule.LFunnelRuleTarget);
                return pRecord;
            })
            .ToList();
        return lPreferenceTabLayout;
    }

    private static int PFunnelTargetRead(Guid pTargetId)
    {
        if (pTargetId == Guid.Empty || PWindow.PWindowStripRead() is not { } pStrip)
        {
            return -1;
        }

        IReadOnlyList<LStripTab> lTabs = pStrip.LStrip.LStripTabs;
        for (int pIndex = 0; pIndex < lTabs.Count; pIndex++)
        {
            if (lTabs[pIndex].LStripTabId == pTargetId)
            {
                return pIndex;
            }
        }

        return -1;
    }
}
