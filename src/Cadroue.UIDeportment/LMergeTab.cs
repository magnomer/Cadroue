using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LMergeTab
{
    private readonly LPresetSelection lMergePreset;
    private readonly LDocket lMergeDocket;

    public LMergeTab(LPresetSelection lPreset, LDocket lDocket, LSceneTabRecord? lLayout)
    {
        lMergePreset = lPreset;
        lMergeDocket = lDocket;
        LMergeSelection = new LGroupSelection(
            lLayout?.LSceneGroupAuto ?? false,
            lLayout?.LSceneGroupStrict ?? true,
            lLayout?.LSceneGroupMode ?? LSeriesNameMode.LSeriesNameBase);
        LMergeGroup = new LGroup(LMergeSelection);
        LMergeGroup.LGroupSourceAttach(LMergePathsRead);
        lDocket.LDocketChange += LMergeDocketHandle;
        lDocket.LDocketRemoved += LMergeGroup.LGroupPathsRemove;
    }

    public event Action? LMergePresetMissing;

    public LGroupSelection LMergeSelection { get; }

    public LGroup LMergeGroup { get; }

    public void LMergeClose()
    {
        lMergeDocket.LDocketChange -= LMergeDocketHandle;
        lMergeDocket.LDocketRemoved -= LMergeGroup.LGroupPathsRemove;
    }

    public LSceneTabRecord LMergeLayoutRead(LSceneTabRecord lLayout)
    {
        lLayout.LSceneGroupAuto = LMergeSelection.LGroupAuto;
        lLayout.LSceneGroupStrict = LMergeSelection.LGroupStrict;
        lLayout.LSceneGroupMode = LMergeSelection.LGroupNameMode;
        return lLayout;
    }

    public void LMergeRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LMergePresetCheck())
        {
            return;
        }

        LMessenger.LMessengerMergeDescribe(
            lPriority,
            LMergeGroupsRead(),
            lMergePreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            LMergeRelaysRead());
    }

    public void LMergeAllRun(Guid lRelayTarget, Guid lSourceTab) =>
        LMergeRun(LWorkPriority.LWorkPriorityNormal, lRelayTarget, lSourceTab);

    public int LMergeCohortRun(Guid lCohort, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!lMergePreset.LPresetSelectionValid)
        {
            LTraceLog.LTraceWarningRecord(
                "Merge held relayed files in tab "
                + $"'{LMessenger.LMessengerTitleSource?.Invoke(lSourceTab) ?? string.Empty}': "
                + "no valid export preset is selected");
            return 0;
        }

        return LMessenger.LMessengerMergeDescribe(
            LWorkPriority.LWorkPriorityNormal,
            LMergeGroupsRead(lCohort),
            lMergePreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            LMergeRelaysRead());
    }

    public IReadOnlyList<string> LMergePathsRead() =>
        lMergeDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath).ToArray();

    public IReadOnlyList<string> LMergeEligibleRead() =>
        LMergeGroupsRead().SelectMany(lGroup => lGroup.LWorkGroupPaths).ToArray();

    public IReadOnlyDictionary<string, Guid> LMergeRelaysRead()
    {
        var lRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (LDocketEntry lItem in lMergeDocket.LDocketUnlockedRead())
        {
            lRelays[lItem.LDocketEntryPath] = lItem.LDocketEntryBatch;
        }

        return lRelays;
    }

    public IReadOnlyList<LWorkGroup> LMergeGroupsRead(Guid lCohort = default)
    {
        var lGroups = new List<LWorkGroup>();
        foreach (LGroupRecord lRecord in LMergeGroup.LGroupRecords)
        {
            if (lCohort != Guid.Empty
                && !lRecord.LGroupRecordPaths.All(lPath =>
                    lMergeDocket.LDocketItemFind(lPath)?.LDocketEntryBatch == lCohort))
            {
                continue;
            }

            string[] lLocked = lRecord.LGroupRecordPaths.Where(lMergeDocket.LDocketLockCheck).ToArray();
            if (lLocked.Length > 0)
            {
                LTraceLog.LTraceWarningRecord(
                    $"Merge skipped group '{lRecord.LGroupRecordName}': "
                    + $"{lLocked.Length} of {lRecord.LGroupRecordPaths.Count} file(s) still in the worklist");
                continue;
            }

            if (lRecord.LGroupRecordPaths.Count > 0)
            {
                lGroups.Add(new LWorkGroup(lRecord.LGroupRecordName, lRecord.LGroupRecordPaths));
            }
        }

        return lGroups;
    }

    private void LMergeDocketHandle(IReadOnlyList<LDocketEntry> lEntries)
    {
        if (LMergeSelection.LGroupAuto)
        {
            LMergeGroup.LGroupAutoApply(LMergePathsRead());
        }
    }

    private bool LMergePresetCheck()
    {
        if (lMergePreset.LPresetSelectionValid)
        {
            return true;
        }

        LMergePresetMissing?.Invoke();
        return false;
    }
}
