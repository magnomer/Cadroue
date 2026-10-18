using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QEditTabPlan
{
    private readonly CUsher qEditUsher;
    private readonly QInspector qEditInspector;
    private readonly QViewer qEditViewer;
    private readonly CList qEditList;
    private readonly QEditTabColor qEditColor;

    public QEditTabPlan(CUsher lUsher, QInspector lInspector, QViewer lViewer, CList lList, QEditTabColor lColor)
    {
        qEditUsher = lUsher;
        qEditInspector = lInspector;
        qEditViewer = lViewer;
        qEditList = lList;
        qEditColor = lColor;
    }

    public string QEditNameRead(string lPath) => qEditUsher.CUsherNameRead(lPath);

    public LEditPlan QEditStateRead()
    {
        (bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight) =
            qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateRatio;
        return new LEditPlan(
            qEditInspector.QInspectorCrop.QInspectorCropRead(),
            qEditColor.QEditVideoRead(),
            qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateActive)
        {
            LEditSkip = qEditInspector.QInspectorSkip.QSkipActive,
            LEditRatioFixed = lRatioFixed,
            LEditRatioLenient = lRatioLenient,
            LEditRatioWidth = lRatioWidth,
            LEditRatioHeight = lRatioHeight
        };
    }

    public LEditPlan? QEditCarriedRead()
    {
        bool lCropPersistent = qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent;
        bool lVideoPersistent = qEditInspector.QInspectorVideo.QInspectorPersistentCheck();
        bool lSkipPersistent = qEditInspector.QInspectorSkip.QSkipPersistent;
        if (!lCropPersistent && !lVideoPersistent && !lSkipPersistent)
        {
            return null;
        }

        (bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight) =
            qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateRatio;
        LWorkCrop lCrop = lCropPersistent
            ? qEditInspector.QInspectorCrop.QInspectorCropRead()
            : LWorkCrop.LWorkCropCreate();
        LWorkVideo lVideo = lVideoPersistent
            ? qEditInspector.QInspectorVideo.QInspectorPersistentRead()
            : LWorkVideo.LWorkVideoCreate();
        return new LEditPlan(lCrop, lVideo, lCropPersistent && qEditInspector.QInspectorCrop.QInspectorActive)
        {
            LEditSkip = lSkipPersistent && qEditInspector.QInspectorSkip.QSkipActive,
            LEditRatioFixed = lCropPersistent && lRatioFixed,
            LEditRatioLenient = lCropPersistent && lRatioLenient,
            LEditRatioWidth = lCropPersistent ? lRatioWidth : 0,
            LEditRatioHeight = lCropPersistent ? lRatioHeight : 0
        };
    }

    public LEditPlan? QEditSavedRead() =>
        qEditViewer.QViewerWorkspace.CWorkspaceSource is { } lSourcePath
            ? LEditSidecar.LEditSidecarRead(lSourcePath, LLibrarian.LLibrarianEditLoad)
            : null;

    public void QEditStateSave() => QEditStateSave(QEditStateRead(), qEditUsher.CUsherNameRead);

    // The plan is saved for the held source; the name is read only for a plan that is written, so a caller that
    // already holds the source's name hands it in rather than asking the usher again.
    public void QEditStateSave(LEditPlan lPlan, Func<string, string> lNameRead)
    {
        if (qEditInspector.QInspectorSaveSuspended
            || qEditViewer.QViewerWorkspace.CWorkspaceSource is not { } lSourcePath
            || qEditList.CListLockCheck(lSourcePath))
        {
            return;
        }

        if (lPlan.LEditPlanEmpty
            && LEditSidecar.LEditSidecarRead(lSourcePath, LLibrarian.LLibrarianEditLoad) is null)
        {
            return;
        }

        string lName = lNameRead(lSourcePath);
        string lSummary = QEditTabTrace.QEditPlanFormat(lPlan);
        if (!LEditSidecar.LEditSidecarSave(lSourcePath, lPlan, LLibrarian.LLibrarianEditSave))
        {
            LTraceLog.LTraceWarningRecord($"Edit plan could not be saved for '{lName}'", lSummary);
            return;
        }

        LTraceLog.LTraceInfoRecord($"Edit plan saved for '{lName}': {lSummary}");
        QEditPersistentSave();
    }

    public void QEditPersistentSave() => QEditPersistentSave(qEditList.CListUnlockedRead());

    public void QEditItemsHandle(IReadOnlyList<LDocketEntry> lAdded) =>
        QEditPersistentSave(lAdded.Select(lItem => lItem.LDocketEntryPath));

    private void QEditPersistentSave(IEnumerable<string> lPaths)
    {
        if (qEditInspector.QInspectorSaveSuspended || QEditCarriedRead() is not { } lCarried)
        {
            return;
        }

        List<string> lFailed = QEditCarriedSave(lPaths, lCarried);
        if (lFailed.Count > 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Persistent Edit state could not be saved for {lFailed.Count} file(s)", string.Join(", ", lFailed));
        }
    }

    // Merges the carried plan into each path's saved plan; answers the names of the paths that failed.
    private List<string> QEditCarriedSave(IEnumerable<string> lPaths, LEditPlan lCarried)
    {
        bool lCropPersistent = qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent;
        bool lSkipPersistent = qEditInspector.QInspectorSkip.QSkipPersistent;
        var lFailed = new List<string>();
        foreach (string lPath in lPaths)
        {
            bool lSaved = LEditSidecar.LEditSidecarSave(
                lPath,
                LEditPlan.LEditPlanResolve(
                    LEditSidecar.LEditSidecarRead(lPath, LLibrarian.LLibrarianEditLoad),
                    lCarried,
                    lCropPersistent,
                    lSkipPersistent),
                LLibrarian.LLibrarianEditSave);
            if (!lSaved)
            {
                lFailed.Add(qEditUsher.CUsherNameRead(lPath));
            }
        }

        return lFailed;
    }
}
