using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

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
                pInspector.PInspectorRatioApply(pEditPlan.LEditRatioFixed, pEditPlan.LEditRatioLenient, pEditPlan.LEditRatioWidth, pEditPlan.LEditRatioHeight);
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

    private void PEditPersistentSave()
    {
        if (pEditPlanLoading || PEditCarriedRead() is not { } pEditCarried)
        {
            return;
        }

        foreach (string pEditPath in pList.PListUnlockedRead().Select(pItem => pItem.LDocketEntryPath))
        {
            LEdit.LEditPlanSave(
                pEditPath,
                LEdit.LEditPlanResolve(LEdit.LEditPlanRead(pEditPath, LLibrarian.LLibrarianEditLoad), pEditCarried),
                LLibrarian.LLibrarianEditSave);
        }
    }

    private void PEditItemsHandle(IReadOnlyList<LDocketEntry> pEditAddedItems)
    {
        if (pEditPlanLoading || PEditCarriedRead() is not { } pEditCarried)
        {
            return;
        }

        foreach (LDocketEntry pEditAddedItem in pEditAddedItems)
        {
            string pEditPath = pEditAddedItem.LDocketEntryPath;
            LEdit.LEditPlanSave(
                pEditPath,
                LEdit.LEditPlanResolve(LEdit.LEditPlanRead(pEditPath, LLibrarian.LLibrarianEditLoad), pEditCarried),
                LLibrarian.LLibrarianEditSave);
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

    private void PEditPlanSave()
    {
        if (pEditPlanLoading
            || pViewer.PViewerSourcePath is not { } pEditSourcePath
            || pList.PListLockCheck(pEditSourcePath))
        {
            return;
        }

        (bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) = pCropOwner.LCropboxStateRatio;
        var pEditPlan = new LEditPlan(
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
        if (!pEditPlan.LEditPlanActive && LEdit.LEditPlanRead(pEditSourcePath, LLibrarian.LLibrarianEditLoad) is null)
        {
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Edit plan saved for '{System.IO.Path.GetFileName(pEditSourcePath)}': {PEditPlanFormat(pEditPlan)}");
        LEdit.LEditPlanSave(pEditSourcePath, pEditPlan, LLibrarian.LLibrarianEditSave);
        PEditPersistentSave();
    }
}
