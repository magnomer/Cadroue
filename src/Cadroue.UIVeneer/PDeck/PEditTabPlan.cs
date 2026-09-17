using Cadroue.Core;
using Cadroue.UIVeneer.PPanel;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;
using Cadroue.Infrastructure;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PEditTab
{
    private void PEditPersistentRestore(LSceneTabRecord? lPreferenceTabLayout)
    {
        if (lPreferenceTabLayout?.LSceneInspector is not { LSceneInspectorEdit: { } pEditRecord } pEditPersistent)
        {
            return;
        }

        pEditPlanLoading = true;
        try
        {
            LEditPlan pEditPlan = LEdit.LEditPersistentRead(pEditRecord);
            if (pEditPersistent.LSceneInspectorCrop)
            {
                pInspector.PCropPlanApply(pEditPlan.LEditCrop, pEditPlan.LEditCropActive);
                pInspector.PInspectorRatioApply(
                    pEditPlan.LEditRatioFixed,
                    pEditPlan.LEditRatioLenient,
                    pEditPlan.LEditRatioWidth,
                    pEditPlan.LEditRatioHeight);
                pInspector.PCropPersistentApply(true);
                pCropOwner.LCropboxStateSet(
                    pEditPlan.LEditCrop,
                    pEditPlan.LEditCropActive,
                    pEditPlan.LEditRatioFixed,
                    pEditPlan.LEditRatioLenient,
                    pEditPlan.LEditRatioWidth,
                    pEditPlan.LEditRatioHeight);
                pCropOwner.LCropboxPersistentSet(true);
            }

            pInspector.PTonePlanApply(pEditPlan.LEditVideo);
            pInspector.PTonePersistentApply(pEditPlan.LEditVideo);
            pInspector.PSkipApply(pEditPlan.LEditSkip);
            pInspector.PSkipPersistentApply(pEditPersistent.LSceneInspectorSkip);
        }
        finally
        {
            pEditPlanLoading = false;
        }
    }

    private void PEditPersistentSave() =>
        PEditPersistentSave(pList.PListUnlockedRead().Select(pItem => pItem.LDocketEntryPath));

    private void PEditItemsHandle(IReadOnlyList<LDocketEntry> pEditAddedItems) =>
        PEditPersistentSave(pEditAddedItems.Select(pEditItem => pEditItem.LDocketEntryPath));

    private void PEditPersistentSave(IEnumerable<string> pEditPaths)
    {
        if (pEditPlanLoading || PEditCarriedRead() is not { } pEditCarried)
        {
            return;
        }

        bool pEditCropPersistent = pCropOwner.LCropboxStatePersistent;
        bool pEditSkipPersistent = pInspector.PSkipPersistentCheck();
        var pEditFailed = new List<string>();
        foreach (string pEditPath in pEditPaths)
        {
            bool pEditSaved = LEdit.LEditPlanSave(
                pEditPath,
                LEdit.LEditPlanResolve(
                    LEdit.LEditPlanRead(pEditPath, LLibrarian.LLibrarianEditLoad),
                    pEditCarried, pEditCropPersistent, pEditSkipPersistent),
                LLibrarian.LLibrarianEditSave);
            if (!pEditSaved)
            {
                pEditFailed.Add(System.IO.Path.GetFileName(pEditPath));
            }
        }

        if (pEditFailed.Count > 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Persistent Edit state could not be saved for {pEditFailed.Count} file(s)",
                string.Join(", ", pEditFailed));
        }
    }

    private LEditPlan? PEditCarriedRead()
    {
        bool pCropPersistent = pCropOwner.LCropboxStatePersistent;
        bool pVideoPersistent = pInspector.PTonePersistentCheck();
        bool pSkipPersistent = pInspector.PSkipPersistentCheck();
        if (!pCropPersistent && !pVideoPersistent && !pSkipPersistent)
        {
            return null;
        }

        bool pCropApply = pCropPersistent && pCropOwner.LCropboxStateActive;
        LWorkCrop pCrop = pCropPersistent
            ? pCropOwner.LCropboxStateCrop
            : LWorkCrop.LWorkCropCreate();
        LWorkVideo pVideo = pVideoPersistent
            ? pInspector.PTonePersistentRead()
            : LWorkVideo.LWorkVideoCreate();
        bool pSkip = pSkipPersistent && pInspector.PSkipActiveCheck();
        (bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) = pCropOwner.LCropboxStateRatio;
        return new LEditPlan(pCrop, pVideo, pCropApply)
        {
            LEditSkip = pSkip,
            LEditRatioFixed = pCropPersistent && pRatioFixed,
            LEditRatioLenient = pCropPersistent && pRatioLenient,
            LEditRatioWidth = pCropPersistent ? pRatioWidth : 0,
            LEditRatioHeight = pCropPersistent ? pRatioHeight : 0
        };
    }

    private LEditPlan PEditPlanRead()
    {
        (bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) = pCropOwner.LCropboxStateRatio;
        return new LEditPlan(
            pCropOwner.LCropboxStateCrop,
            PEditVideoRead(),
            pCropOwner.LCropboxStateActive)
        {
            LEditSkip = pInspector.PSkipActiveCheck(),
            LEditRatioFixed = pRatioFixed,
            LEditRatioLenient = pRatioLenient,
            LEditRatioWidth = pRatioWidth,
            LEditRatioHeight = pRatioHeight
        };
    }

    private void PEditPlanSave() => PEditPlanSave(PEditPlanRead());

    private void PEditPlanSave(LEditPlan pEditPlan)
    {
        if (pEditPlanLoading
            || pViewer.PViewerSourcePath is not { } pEditSourcePath
            || pList.PListLockCheck(pEditSourcePath))
        {
            return;
        }

        if (pEditPlan.LEditPlanEmpty && LEdit.LEditPlanRead(pEditSourcePath, LLibrarian.LLibrarianEditLoad) is null)
        {
            return;
        }

        string pEditName = System.IO.Path.GetFileName(pEditSourcePath);
        string pEditSummary = PEditPlanFormat(pEditPlan);
        if (!LEdit.LEditPlanSave(pEditSourcePath, pEditPlan, LLibrarian.LLibrarianEditSave))
        {
            LTraceLog.LTraceWarningRecord($"Edit plan could not be saved for '{pEditName}'", pEditSummary);
            return;
        }

        LTraceLog.LTraceInfoRecord($"Edit plan saved for '{pEditName}': {pEditSummary}");
        PEditPersistentSave();
    }
}
