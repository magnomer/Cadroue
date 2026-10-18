using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The tab's undo history: snapshots its sections and preset after each change, starts over on new media, and puts a
// snapshot back on undo or redo while the flow allows section edits.
public sealed class QWorkspaceHistory
{
    private readonly LHistory qWorkspaceHistory = new();
    private readonly QWorkspace qWorkspace;

    public QWorkspaceHistory(QWorkspace lOwner)
    {
        qWorkspace = lOwner;
    }

    // The history starts from now and the preset's changes feed it; the workspace wires the flow's section and media
    // changes to its handlers with the tab's other handlers.
    internal void QWorkspaceHistoryAttach()
    {
        QWorkspaceHistoryReset();
        qWorkspace.QWorkspacePreset.LPresetChanged += QWorkspaceHistoryAdd;
    }

    internal void QWorkspaceHistoryDetach() => qWorkspace.QWorkspacePreset.LPresetChanged -= QWorkspaceHistoryAdd;

    public LHistoryEntry QWorkspaceSnapshotRead() => new(
        qWorkspace.QWorkspaceSegment?.LSegmentListRead() ?? Array.Empty<LPiece>(),
        qWorkspace.QWorkspaceSegment?.LSegmentSelectionRead(),
        qWorkspace.QWorkspacePreset.LPresetRecordCreate());

    public void QWorkspaceSectionHandle(IReadOnlyList<LPiece> lSections, int? lSectionSelect) => QWorkspaceHistoryAdd();

    public void QWorkspaceHistoryAdd() => qWorkspaceHistory.LHistoryAdd(QWorkspaceSnapshotRead());

    public void QWorkspaceHistoryReset() => qWorkspaceHistory.LHistoryReset(QWorkspaceSnapshotRead());

    public bool QWorkspaceUndo() =>
        QWorkspaceEditableCheck() && QWorkspaceSnapshotApply(qWorkspaceHistory.LHistoryUndo());

    public bool QWorkspaceRedo() =>
        QWorkspaceEditableCheck() && QWorkspaceSnapshotApply(qWorkspaceHistory.LHistoryRedo());

    private bool QWorkspaceEditableCheck() =>
        qWorkspace.QWorkspaceFlow is null || qWorkspace.QWorkspaceFlow.QFlowEdit.QFlowSectionEditable;

    private bool QWorkspaceSnapshotApply(LHistoryEntry? lEntry)
    {
        if (lEntry is null)
        {
            return false;
        }

        qWorkspaceHistory.LHistorySuspend();
        try
        {
            QWorkspaceSectionsSet(lEntry.LHistorySections, lEntry.LHistorySectionIndex);
            qWorkspace.QWorkspacePresetOwner.LPresetSelectionValue = lEntry.LHistoryExport;
        }
        finally
        {
            qWorkspaceHistory.LHistoryResume();
        }

        return true;
    }

    private void QWorkspaceSectionsSet(IReadOnlyList<LPiece> lSections, int? lSectionSelect)
    {
        if (qWorkspace.QWorkspaceFlow is not { } lFlow || qWorkspace.QWorkspaceSegment is not { } lSegment
            || !lFlow.QFlowEdit.QFlowSectionEditable || lFlow.QFlowRange is null)
        {
            return;
        }

        lSegment.LSegmentBoundSet(lSections, lSectionSelect, lFlow.QFlowDuration);
    }
}
