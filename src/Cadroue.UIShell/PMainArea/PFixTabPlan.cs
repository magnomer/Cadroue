using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PFixTab
{
    private void PFixPersistentRestore(LSceneTabRecord? lPreferenceTabLayout)
    {
        if (lPreferenceTabLayout?.LSceneInspector is not { LSceneInspectorEdit: { } pFixRecord } pFixPersistent)
        {
            return;
        }

        pFixPlanLoading = true;
        try
        {
            LEditPlan pFixPlan = LEdit.LEditPersistentRead(pFixRecord);
            if (pFixPersistent.LSceneInspectorCrop)
            {
                pInspector.PCropPlanApply(pFixPlan.LEditCrop, pFixPlan.LEditCropActive);
                pInspector.PInspectorRatioApply(pFixPlan.LEditRatioFixed, pFixPlan.LEditRatioLenient, pFixPlan.LEditRatioWidth, pFixPlan.LEditRatioHeight);
                pInspector.PCropPersistentApply(true);
                pCropOwner.LCropboxStateSet(
                    pFixPlan.LEditCrop,
                    pFixPlan.LEditCropActive,
                    pFixPlan.LEditRatioFixed,
                    pFixPlan.LEditRatioLenient,
                    pFixPlan.LEditRatioWidth,
                    pFixPlan.LEditRatioHeight);
                pCropOwner.LCropboxPersistentSet(true);
            }

            pInspector.PTonePlanApply(pFixPlan.LEditVideo);
            pInspector.PTonePersistentApply(pFixPlan.LEditVideo);
            pInspector.PSkipApply(pFixPlan.LEditSkip);
            pInspector.PSkipPersistentApply(pFixPersistent.LSceneInspectorSkip);
        }
        finally
        {
            pFixPlanLoading = false;
        }
    }

    private void PFixPersistentSave()
    {
        if (pFixPlanLoading || PFixCarriedRead() is not { } pFixCarried)
        {
            return;
        }

        bool pFixCropPersistent = pCropOwner.LCropboxStatePersistent;
        bool pFixSkipPersistent = pInspector.PSkipPersistentCheck();
        foreach (string pFixPath in pList.PListUnlockedRead().Select(pItem => pItem.LDocketEntryPath))
        {
            LEdit.LEditPlanSave(
                pFixPath,
                LEdit.LEditPlanResolve(
                    LEdit.LEditPlanRead(pFixPath, LLibrarian.LLibrarianEditLoad),
                    pFixCarried, pFixCropPersistent, pFixSkipPersistent),
                LLibrarian.LLibrarianEditSave);
        }
    }

    private void PFixItemsHandle(IReadOnlyList<LDocketEntry> pFixAddedItems)
    {
        if (pFixPlanLoading || PFixCarriedRead() is not { } pFixCarried)
        {
            return;
        }

        bool pFixCropPersistent = pCropOwner.LCropboxStatePersistent;
        bool pFixSkipPersistent = pInspector.PSkipPersistentCheck();
        foreach (LDocketEntry pFixAddedItem in pFixAddedItems)
        {
            string pFixPath = pFixAddedItem.LDocketEntryPath;
            LEdit.LEditPlanSave(
                pFixPath,
                LEdit.LEditPlanResolve(
                    LEdit.LEditPlanRead(pFixPath, LLibrarian.LLibrarianEditLoad),
                    pFixCarried, pFixCropPersistent, pFixSkipPersistent),
                LLibrarian.LLibrarianEditSave);
        }
    }

    private LEditPlan? PFixCarriedRead()
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

    private void PFixPlanSave()
    {
        if (pFixPlanLoading
            || pViewer.PViewerSourcePath is not { } pFixSourcePath
            || pList.PListLockCheck(pFixSourcePath))
        {
            return;
        }

        (bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) = pCropOwner.LCropboxStateRatio;
        var pFixPlan = new LEditPlan(
            pCropOwner.LCropboxStateCrop,
            PFixVideoRead(),
            pCropOwner.LCropboxStateActive)
        {
            LEditSkip = pInspector.PSkipActiveCheck(),
            LEditRatioFixed = pRatioFixed,
            LEditRatioLenient = pRatioLenient,
            LEditRatioWidth = pRatioWidth,
            LEditRatioHeight = pRatioHeight
        };
        if (!pFixPlan.LEditPlanActive && LEdit.LEditPlanRead(pFixSourcePath, LLibrarian.LLibrarianEditLoad) is null)
        {
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Edit plan saved for '{System.IO.Path.GetFileName(pFixSourcePath)}': {PFixPlanFormat(pFixPlan)}");
        LEdit.LEditPlanSave(pFixSourcePath, pFixPlan, LLibrarian.LLibrarianEditSave);
        PFixPersistentSave();
    }
}
