using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIVeneer.PPanel;
using Cadroue.UIVeneer.PToolbar;
using PFlowControl = Cadroue.UIVeneer.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIVeneer.PDeck;

public sealed class PMergeTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly LDocket lDocket = new();
    private readonly PList pList;
    private readonly LGroupSelection lGroupOwner;
    private readonly PGroup pGroup;
    private readonly PAction pAction = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PMergeTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        pList = new PList(lDocket);
        lGroupOwner = new LGroupSelection(
            lPreferenceTabLayout?.LSceneGroupAuto ?? false,
            lPreferenceTabLayout?.LSceneGroupStrict ?? true,
            lPreferenceTabLayout?.LSceneGroupMode ?? LSeriesNameMode.LSeriesNameBase);
        pGroup = new PGroup(lGroupOwner);
        PTabAction = pAction;
        pAction.PActionRun += pPriority =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            LMessenger.LMessengerMergeDescribe(
                pPriority, PMergeGroupsRead(), lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget, pAction.PActionSourceTab, PMergeRelaysRead());
        };
        pAction.PActionAllAdd += () =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            LMessenger.LMessengerMergeDescribe(
                LWorkPriority.LWorkPriorityNormal,
                PMergeGroupsRead(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget, pAction.PActionSourceTab, PMergeRelaysRead());
        };
        pAction.PActionCohortAdd += pCohort =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                LTraceLog.LTraceWarningRecord(
                    $"Merge held relayed files in tab '{PStrip.PStripTitleRead(pAction.PActionSourceTab)}': "
                    + "no valid export preset is selected");
                return 0;
            }

            return LMessenger.LMessengerMergeDescribe(
                LWorkPriority.LWorkPriorityNormal,
                PMergeGroupsRead(pCohort),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget, pAction.PActionSourceTab, PMergeRelaysRead());
        };
        pList.PListPathChange += PMergePathShow;
        pGroup.PGroupItemOpen += PMergePathShow;
        pGroup.PGroupSourceFiles = () => pList.PListUnlockedRead()
            .Select(pItem => pItem.LDocketEntryPath)
            .ToArray();
        pGroup.PGroupFileRequest = pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
        pAction.PActionEligibleSource = () => PMergeGroupsRead()
            .SelectMany(pMergeGroup => pMergeGroup.LWorkGroupPaths)
            .ToArray();
        lDocket.LDocketChange += PMergeDocketHandle;
        pList.PListClearChange += pGroup.PGroupPathsRemove;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner);
        PTabLockAttach(pList, pExport);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pGroup, pViewer, pExport },
            new PCompass(pFlow, pViewer),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
    }

    private IReadOnlyDictionary<string, Guid> PMergeRelaysRead()
    {
        var pMergeRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (LDocketEntry pItem in pList.PListUnlockedRead())
        {
            pMergeRelays[pItem.LDocketEntryPath] = pItem.LDocketEntryBatch;
        }

        return pMergeRelays;
    }

    private IReadOnlyList<LWorkGroup> PMergeGroupsRead(Guid pMergeCohort = default)
    {
        var pMergeGroups = new List<LWorkGroup>();
        foreach (LGroupRecord pGroupSelection in pGroup.PGroupGroupsRead())
        {
            if (pMergeCohort != Guid.Empty
                && !pGroupSelection.LGroupRecordPaths.All(pMergePath =>
                    lDocket.LDocketItemFind(pMergePath)?.LDocketEntryBatch == pMergeCohort))
            {
                continue;
            }

            string[] pMergeLocked = pGroupSelection.LGroupRecordPaths
                .Where(pList.PListLockCheck)
                .ToArray();
            if (pMergeLocked.Length > 0)
            {
                LTraceLog.LTraceWarningRecord(
                    $"Merge skipped group '{pGroupSelection.LGroupRecordName}': "
                    + $"{pMergeLocked.Length} of {pGroupSelection.LGroupRecordPaths.Count} file(s) "
                    + "still in the worklist");
                continue;
            }

            if (pGroupSelection.LGroupRecordPaths.Count > 0)
            {
                pMergeGroups.Add(new LWorkGroup(
                    pGroupSelection.LGroupRecordName,
                    pGroupSelection.LGroupRecordPaths));
            }
        }

        return pMergeGroups;
    }

    private void PMergeDocketHandle(IReadOnlyList<LDocketEntry> pDocketEntries)
    {
        if (lGroupOwner.LGroupAuto)
        {
            pGroup.PGroupAutoUpdate();
        }
    }

    private void PMergePathShow(string? pSourcePath) => PTabSourceOpen(pViewer, pSourcePath);

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override PGroup? PTabGroup => pGroup;

    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        lPreferenceTabLayout.LSceneGroupAuto = lGroupOwner.LGroupAuto;
        lPreferenceTabLayout.LSceneGroupStrict = lGroupOwner.LGroupStrict;
        lPreferenceTabLayout.LSceneGroupMode = lGroupOwner.LGroupNameMode;
        return lPreferenceTabLayout;
    }
}
