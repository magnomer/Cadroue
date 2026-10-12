using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QMergeTab
{
    private readonly LPresetSelection qMergePreset;
    private readonly LDocket qMergeDocket;

    public QMergeTab(CUsher lUsher, LPresetSelection lPreset, QViewer lViewer, QList lList)
    {
        qMergePreset = lPreset;
        qMergeDocket = lList.QListDocket;
        QMergeGroup = new QGroup(lUsher, QMergeSelection);
        QMergeGroup.QGroupSourceAttach(QMergePathsRead);
        qMergeDocket.LDocketChanged += QMergeDocketHandle;
        qMergeDocket.LDocketRemoved += QMergeGroup.QGroupPathsRemove;
        lList.QListPathChanged += lViewer.QViewerPathHandle;
        QMergeGroup.QGroupDrag.QGroupItemOpened += lViewer.QViewerPathHandle;
        QMergeGroup.QGroupDrag.QGroupPathsRequest += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
    }

    public event Action? QMergePresetMissing;

    public LGroupSelection QMergeSelection { get; } = new();

    public QGroup QMergeGroup { get; }

    public void QMergeClose()
    {
        qMergeDocket.LDocketChanged -= QMergeDocketHandle;
        qMergeDocket.LDocketRemoved -= QMergeGroup.QGroupPathsRemove;
    }

    public LSceneTabEntry QMergeLayoutRead(LSceneTabEntry lLayout)
    {
        lLayout.LSceneGroupAuto = QMergeSelection.LGroupAuto;
        lLayout.LSceneGroupStrict = QMergeSelection.LGroupStrict;
        lLayout.LSceneGroupMode = QMergeSelection.LGroupNameMode;
        return lLayout;
    }

    public void QMergeLayoutApply(LSceneTabEntry? lLayout)
    {
        QMergeSelection.LGroupAutoChange(lLayout?.LSceneGroupAuto ?? false);
        QMergeSelection.LGroupStrictChange(lLayout?.LSceneGroupStrict ?? true);
        QMergeSelection.LGroupModeChange(lLayout?.LSceneGroupMode ?? LSeriesNameMode.LSeriesNameBase);
    }

    public void QMergeRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QMergePresetCheck())
        {
            return;
        }

        LMessenger.LMessengerMergeDescribe(
            lPriority,
            QMergeGroupsRead(),
            qMergePreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            QMergeRelaysRead());
    }

    public void QMergeAllRun(Guid lRelayTarget, Guid lSourceTab) =>
        QMergeRun(LWorkPriority.LWorkPriorityNormal, lRelayTarget, lSourceTab);

    public int QMergeCohortRun(Guid lCohort, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!qMergePreset.LPresetSelectionValid)
        {
            LTraceLog.LTraceWarningRecord(
                "Merge held relayed files in tab "
                + $"'{LMessenger.LMessengerTitleSource?.Invoke(lSourceTab) ?? string.Empty}': "
                + "no valid export preset is selected");
            return 0;
        }

        return LMessenger.LMessengerMergeDescribe(
            LWorkPriority.LWorkPriorityNormal,
            QMergeGroupsRead(lCohort),
            qMergePreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            QMergeRelaysRead());
    }

    public IReadOnlyList<string> QMergePathsRead() =>
        qMergeDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath).ToArray();

    public IReadOnlyList<string> QMergeEligibleRead() =>
        QMergeGroupsRead().SelectMany(lGroup => lGroup.LWorkGroupPaths).ToArray();

    public IReadOnlyDictionary<string, Guid> QMergeRelaysRead()
    {
        var lRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (LDocketEntry lItem in qMergeDocket.LDocketUnlockedRead())
        {
            lRelays[lItem.LDocketEntryPath] = lItem.LDocketEntryBatch;
        }

        return lRelays;
    }

    public IReadOnlyList<LWorkGroup> QMergeGroupsRead(Guid lCohort = default)
    {
        var lGroups = new List<LWorkGroup>();
        foreach (QGroupEntry lRecord in QMergeGroup.QGroupRecords)
        {
            if (lCohort != Guid.Empty
                && !lRecord.QGroupRecordPaths.All(lPath =>
                    qMergeDocket.LDocketItemFind(lPath)?.LDocketEntryBatch == lCohort))
            {
                continue;
            }

            string[] lLocked = lRecord.QGroupRecordPaths.Where(qMergeDocket.LDocketLockCheck).ToArray();
            if (lLocked.Length > 0)
            {
                LTraceLog.LTraceWarningRecord(
                    $"Merge skipped group '{lRecord.QGroupRecordName}': "
                    + $"{lLocked.Length} of {lRecord.QGroupRecordPaths.Count} file(s) still in the worklist");
                continue;
            }

            if (lRecord.QGroupRecordPaths.Count > 0)
            {
                lGroups.Add(new LWorkGroup(lRecord.QGroupRecordName, lRecord.QGroupRecordPaths));
            }
        }

        return lGroups;
    }

    private void QMergeDocketHandle(IReadOnlyList<LDocketEntry> lEntries)
    {
        if (QMergeSelection.LGroupAuto)
        {
            QMergeGroup.QGroupAutoApply(QMergePathsRead());
        }
    }

    private bool QMergePresetCheck()
    {
        if (qMergePreset.LPresetSelectionValid)
        {
            return true;
        }

        QMergePresetMissing?.Invoke();
        return false;
    }
}
