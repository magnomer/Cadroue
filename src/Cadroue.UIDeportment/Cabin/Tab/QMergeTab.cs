using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QMergeTab
{
    private readonly CMerge qMergeGate;
    private readonly LPresetSelection qMergePreset;
    private readonly LDocket qMergeDocket;

    public QMergeTab(CUsher lUsher, LPresetSelection lPreset, QViewer lViewer, QList lList, CMerge lMerge)
    {
        qMergeGate = lMerge;
        qMergePreset = lPreset;
        qMergeDocket = lList.QListDocket;
        QMergeGroup = new QGroup(lUsher, QMergeSelection);
        QMergeHandlersAttach(lViewer, lList);
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
        (bool lAuto, bool lStrict, LSeriesNameMode lMode) = QMergeLayoutResolve(lLayout);
        QMergeSelection.LGroupAutoChange(lAuto);
        QMergeSelection.LGroupStrictChange(lStrict);
        QMergeSelection.LGroupModeChange(lMode);
    }

    // Each run gesture is one gate call; the gate answers false when the preset is missing, which raises the notice.
    public void QMergeRun(bool lUrgent, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!qMergeGate.CMergeRun(
                qMergePreset.LPresetSelectionValid,
                qMergePreset.LPresetSelectionName,
                QMergeRecordsRead(),
                QMergeItemsRead(),
                lUrgent,
                lRelayTarget,
                lSourceTab))
        {
            QMergePresetMissing?.Invoke();
        }
    }

    public void QMergeAllRun(Guid lRelayTarget, Guid lSourceTab) =>
        QMergeRun(false, lRelayTarget, lSourceTab);

    public int QMergeCohortRun(Guid lCohort, Guid lRelayTarget, Guid lSourceTab) =>
        qMergeGate.CMergeCohortRun(
            qMergePreset.LPresetSelectionValid,
            qMergePreset.LPresetSelectionName,
            QMergeRecordsRead(),
            QMergeItemsRead(),
            lCohort,
            lRelayTarget,
            lSourceTab);

    public IReadOnlyList<string> QMergePathsRead() =>
        qMergeDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath).ToArray();

    public IReadOnlyList<string> QMergeEligibleRead() =>
        qMergeGate.CMergeEligibleRead(QMergeRecordsRead(), QMergeItemsRead());

    // The group list's raw rows, each group's path list as the list holds it.
    private IReadOnlyList<(string, IReadOnlyList<string>)> QMergeRecordsRead() =>
        QMergeGroup.QGroupRecords
            .Select(lRecord => (lRecord.QGroupRecordName, (IReadOnlyList<string>)lRecord.QGroupRecordPaths))
            .ToArray();

    private IReadOnlyList<(string, Guid, bool)> QMergeItemsRead() =>
        qMergeDocket.LDocketItemsRead()
            .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch, lItem.LDocketEntryLocked))
            .ToArray();

    private void QMergeHandlersAttach(QViewer lViewer, QList lList)
    {
        QMergeGroup.QGroupSourceAttach(QMergePathsRead);
        qMergeDocket.LDocketChanged += QMergeDocketHandle;
        qMergeDocket.LDocketRemoved += QMergeGroup.QGroupPathsRemove;
        lList.QListPathChanged += lViewer.QViewerWorkspace.CWorkspaceOpen;
        QMergeGroup.QGroupDrag.QGroupItemOpened += lViewer.QViewerWorkspace.CWorkspaceOpen;
        QMergeGroup.QGroupDrag.QGroupPathsRequest += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
    }

    private static (bool, bool, LSeriesNameMode) QMergeLayoutResolve(LSceneTabEntry? lLayout) =>
        (lLayout?.LSceneGroupAuto ?? false,
            lLayout?.LSceneGroupStrict ?? true,
            lLayout?.LSceneGroupMode ?? LSeriesNameMode.LSeriesNameBase);

    private void QMergeDocketHandle(IReadOnlyList<LDocketEntry> lEntries)
    {
        if (QMergeSelection.LGroupAuto)
        {
            QMergeGroup.QGroupAutoApply(QMergePathsRead());
        }
    }

}
