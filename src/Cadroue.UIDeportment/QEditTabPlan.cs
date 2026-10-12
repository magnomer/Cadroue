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
    private readonly LDocket qEditDocket;
    private readonly QEditTabColor qEditColor;

    public QEditTabPlan(CUsher lUsher, QInspector lInspector, QViewer lViewer, LDocket lDocket, QEditTabColor lColor)
    {
        qEditUsher = lUsher;
        qEditInspector = lInspector;
        qEditViewer = lViewer;
        qEditDocket = lDocket;
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
        bool lVideoPersistent = qEditInspector.QInspectorPersistentCheck();
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
            ? qEditInspector.QInspectorPersistentRead()
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
        qEditViewer.QViewerSourcePath is { } lSourcePath
            ? LEdit.LEditPlanRead(lSourcePath, LLibrarian.LLibrarianEditLoad)
            : null;

    public void QEditStateSave() => QEditStateSave(QEditStateRead());

    public void QEditStateSave(LEditPlan lPlan)
    {
        if (qEditInspector.QInspectorSaveSuspended
            || qEditViewer.QViewerSourcePath is not { } lSourcePath
            || qEditDocket.LDocketLockCheck(lSourcePath))
        {
            return;
        }

        if (lPlan.LEditPlanEmpty && LEdit.LEditPlanRead(lSourcePath, LLibrarian.LLibrarianEditLoad) is null)
        {
            return;
        }

        string lName = qEditUsher.CUsherNameRead(lSourcePath);
        string lSummary = QEditPlanFormat(lPlan);
        if (!LEdit.LEditPlanSave(lSourcePath, lPlan, LLibrarian.LLibrarianEditSave))
        {
            LTraceLog.LTraceWarningRecord($"Edit plan could not be saved for '{lName}'", lSummary);
            return;
        }

        LTraceLog.LTraceInfoRecord($"Edit plan saved for '{lName}': {lSummary}");
        QEditPersistentSave();
    }

    public void QEditPersistentSave() =>
        QEditPersistentSave(qEditDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath));

    public void QEditItemsHandle(IReadOnlyList<LDocketEntry> lAdded) =>
        QEditPersistentSave(lAdded.Select(lItem => lItem.LDocketEntryPath));

    private void QEditPersistentSave(IEnumerable<string> lPaths)
    {
        if (qEditInspector.QInspectorSaveSuspended || QEditCarriedRead() is not { } lCarried)
        {
            return;
        }

        bool lCropPersistent = qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent;
        bool lSkipPersistent = qEditInspector.QInspectorSkip.QSkipPersistent;
        var lFailed = new List<string>();
        foreach (string lPath in lPaths)
        {
            bool lSaved = LEdit.LEditPlanSave(
                lPath,
                LEdit.LEditPlanResolve(
                    LEdit.LEditPlanRead(lPath, LLibrarian.LLibrarianEditLoad),
                    lCarried,
                    lCropPersistent,
                    lSkipPersistent),
                LLibrarian.LLibrarianEditSave);
            if (!lSaved)
            {
                lFailed.Add(qEditUsher.CUsherNameRead(lPath));
            }
        }

        if (lFailed.Count > 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Persistent Edit state could not be saved for {lFailed.Count} file(s)", string.Join(", ", lFailed));
        }
    }

    public static string QEditRectFormat(LCropbox? lRect) =>
        lRect is { } lBox
            ? $"rect {lBox.LCropboxX:0},{lBox.LCropboxY:0} {lBox.LCropboxWidth:0}x{lBox.LCropboxHeight:0}"
            : "rect none";

    public static string QEditCropFormat(LWorkCrop? lCrop)
    {
        if (lCrop is not { } lValue)
        {
            return "none";
        }

        if (!lValue.LWorkCropActive)
        {
            return "inactive";
        }

        string lEdges = lValue.LWorkEdgeActive
            ? $"edges {lValue.LWorkCropLeft}/{lValue.LWorkCropTop}/{lValue.LWorkCropRight}/{lValue.LWorkCropBottom}"
            : "no edges";
        string lFlip = lValue.LWorkFlipHorizontal || lValue.LWorkFlipVertical
            ? $"flip {(lValue.LWorkFlipHorizontal ? "H" : "")}{(lValue.LWorkFlipVertical ? "V" : "")}"
            : "no flip";
        return $"{lEdges}, rotate {lValue.LWorkCropRotation}, {lFlip}";
    }

    public static string QEditPlanFormat(LEditPlan? lPlan) =>
        lPlan is null ? "none" : $"{QEditCropFormat(lPlan.LEditCrop)}, {QEditVideoFormat(lPlan.LEditVideo)}";

    public static string QEditVideoFormat(LWorkVideo lVideo) =>
        lVideo.LWorkVideoActive
            ? string.Join(
                ", ",
                lVideo.LWorkVideoSteps
                    .Where(lStep => lStep.LWorkStepActive)
                    .Select(lStep => lStep.LWorkDiagnosticRead()))
            : "video inactive";
}
