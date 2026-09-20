using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class LEditTabPlan
{
    private readonly LInspector lEditInspector;
    private readonly LViewer lEditViewer;
    private readonly LDocket lEditDocket;
    private readonly LEditTabColor lEditColor;

    public LEditTabPlan(LInspector lInspector, LViewer lViewer, LDocket lDocket, LEditTabColor lColor)
    {
        lEditInspector = lInspector;
        lEditViewer = lViewer;
        lEditDocket = lDocket;
        lEditColor = lColor;
    }

    public LEditPlan LEditStateRead()
    {
        (bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight) =
            lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateRatio;
        return new LEditPlan(
            lEditInspector.LInspectorCrop.LInspectorCropRead(),
            lEditColor.LEditVideoRead(),
            lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateActive)
        {
            LEditSkip = lEditInspector.LInspectorSkip.LSkipActive,
            LEditRatioFixed = lRatioFixed,
            LEditRatioLenient = lRatioLenient,
            LEditRatioWidth = lRatioWidth,
            LEditRatioHeight = lRatioHeight
        };
    }

    public LEditPlan? LEditCarriedRead()
    {
        bool lCropPersistent = lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent;
        bool lVideoPersistent = lEditInspector.LInspectorPersistentCheck();
        bool lSkipPersistent = lEditInspector.LInspectorSkip.LSkipPersistent;
        if (!lCropPersistent && !lVideoPersistent && !lSkipPersistent)
        {
            return null;
        }

        (bool lRatioFixed, bool lRatioLenient, int lRatioWidth, int lRatioHeight) =
            lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStateRatio;
        LWorkCrop lCrop = lCropPersistent
            ? lEditInspector.LInspectorCrop.LInspectorCropRead()
            : LWorkCrop.LWorkCropCreate();
        LWorkVideo lVideo = lVideoPersistent
            ? lEditInspector.LInspectorPersistentRead()
            : LWorkVideo.LWorkVideoCreate();
        return new LEditPlan(lCrop, lVideo, lCropPersistent && lEditInspector.LInspectorCrop.LInspectorActive)
        {
            LEditSkip = lSkipPersistent && lEditInspector.LInspectorSkip.LSkipActive,
            LEditRatioFixed = lCropPersistent && lRatioFixed,
            LEditRatioLenient = lCropPersistent && lRatioLenient,
            LEditRatioWidth = lCropPersistent ? lRatioWidth : 0,
            LEditRatioHeight = lCropPersistent ? lRatioHeight : 0
        };
    }

    public LEditPlan? LEditSavedRead() =>
        lEditViewer.LViewerSourcePath is { } lSourcePath
            ? LEdit.LEditPlanRead(lSourcePath, LLibrarian.LLibrarianEditLoad)
            : null;

    public void LEditStateSave() => LEditStateSave(LEditStateRead());

    public void LEditStateSave(LEditPlan lPlan)
    {
        if (lEditInspector.LInspectorSaveSuspended
            || lEditViewer.LViewerSourcePath is not { } lSourcePath
            || lEditDocket.LDocketLockCheck(lSourcePath))
        {
            return;
        }

        if (lPlan.LEditPlanEmpty && LEdit.LEditPlanRead(lSourcePath, LLibrarian.LLibrarianEditLoad) is null)
        {
            return;
        }

        string lName = LUsher.LUsherNameRead(lSourcePath);
        string lSummary = LEditPlanFormat(lPlan);
        if (!LEdit.LEditPlanSave(lSourcePath, lPlan, LLibrarian.LLibrarianEditSave))
        {
            LTraceLog.LTraceWarningRecord($"Edit plan could not be saved for '{lName}'", lSummary);
            return;
        }

        LTraceLog.LTraceInfoRecord($"Edit plan saved for '{lName}': {lSummary}");
        LEditPersistentSave();
    }

    public void LEditPersistentSave() =>
        LEditPersistentSave(lEditDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath));

    public void LEditItemsHandle(IReadOnlyList<LDocketEntry> lAdded) =>
        LEditPersistentSave(lAdded.Select(lItem => lItem.LDocketEntryPath));

    private void LEditPersistentSave(IEnumerable<string> lPaths)
    {
        if (lEditInspector.LInspectorSaveSuspended || LEditCarriedRead() is not { } lCarried)
        {
            return;
        }

        bool lCropPersistent = lEditInspector.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent;
        bool lSkipPersistent = lEditInspector.LInspectorSkip.LSkipPersistent;
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
                lFailed.Add(LUsher.LUsherNameRead(lPath));
            }
        }

        if (lFailed.Count > 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Persistent Edit state could not be saved for {lFailed.Count} file(s)", string.Join(", ", lFailed));
        }
    }

    public static string LEditRectFormat(LCropbox? lRect) =>
        lRect is { } lBox
            ? $"rect {lBox.LCropboxX:0},{lBox.LCropboxY:0} {lBox.LCropboxWidth:0}x{lBox.LCropboxHeight:0}"
            : "rect none";

    public static string LEditCropFormat(LWorkCrop? lCrop)
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

    public static string LEditPlanFormat(LEditPlan? lPlan) =>
        lPlan is null ? "none" : $"{LEditCropFormat(lPlan.LEditCrop)}, {LEditVideoFormat(lPlan.LEditVideo)}";

    public static string LEditVideoFormat(LWorkVideo lVideo) =>
        lVideo.LWorkVideoActive
            ? string.Join(
                ", ",
                lVideo.LWorkVideoSteps
                    .Where(lStep => lStep.LWorkStepActive)
                    .Select(lStep => lStep.LWorkDiagnosticRead()))
            : "video inactive";
}
