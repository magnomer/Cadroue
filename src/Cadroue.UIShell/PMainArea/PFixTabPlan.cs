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
        if (lPreferenceTabLayout?.LSceneInspector is not { LSceneInspectorFix: { } pFixRecord } pFixPersistent)
        {
            return;
        }

        pFixPlanLoading = true;
        try
        {
            LFixPlan pFixPlan = LFix.LFixPersistentRead(pFixRecord);
            if (pFixPersistent.LSceneInspectorCrop)
            {
                pInspector.PCropPlanApply(pFixPlan.LFixCrop, pFixPlan.LFixCropActive);
                pInspector.PInspectorRatioApply(pFixPlan.LFixRatioFixed, pFixPlan.LFixRatioLenient, pFixPlan.LFixRatioWidth, pFixPlan.LFixRatioHeight);
                pInspector.PCropPersistentApply(true);
                pCropOwner.LCropboxStateSet(
                    pFixPlan.LFixCrop,
                    pFixPlan.LFixCropActive,
                    pFixPlan.LFixRatioFixed,
                    pFixPlan.LFixRatioLenient,
                    pFixPlan.LFixRatioWidth,
                    pFixPlan.LFixRatioHeight);
                pCropOwner.LCropboxPersistentSet(true);
            }

            pInspector.PTonePlanApply(pFixPlan.LFixVideo);
            pInspector.PTonePersistentApply(pFixPlan.LFixVideo);
            pInspector.PSkipApply(pFixPlan.LFixSkip);
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
            LFix.LFixPlanSave(
                pFixPath,
                LFix.LFixPlanResolve(
                    LFix.LFixPlanRead(pFixPath, LLibrarian.LLibrarianFixLoad),
                    pFixCarried, pFixCropPersistent, pFixSkipPersistent),
                LLibrarian.LLibrarianFixSave);
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
            LFix.LFixPlanSave(
                pFixPath,
                LFix.LFixPlanResolve(
                    LFix.LFixPlanRead(pFixPath, LLibrarian.LLibrarianFixLoad),
                    pFixCarried, pFixCropPersistent, pFixSkipPersistent),
                LLibrarian.LLibrarianFixSave);
        }
    }

    private LFixPlan? PFixCarriedRead()
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
        return new LFixPlan(pCrop, pVideo, pCropApply)
        {
            LFixSkip = pSkip,
            LFixRatioFixed = pCropPersistent && pRatioFixed,
            LFixRatioLenient = pCropPersistent && pRatioLenient,
            LFixRatioWidth = pCropPersistent ? pRatioWidth : 0,
            LFixRatioHeight = pCropPersistent ? pRatioHeight : 0
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
        var pFixPlan = new LFixPlan(
            pCropOwner.LCropboxStateCrop,
            PFixVideoRead(),
            pCropOwner.LCropboxStateActive)
        {
            LFixSkip = pInspector.PSkipActiveCheck(),
            LFixRatioFixed = pRatioFixed,
            LFixRatioLenient = pRatioLenient,
            LFixRatioWidth = pRatioWidth,
            LFixRatioHeight = pRatioHeight
        };
        if (!pFixPlan.LFixPlanActive && LFix.LFixPlanRead(pFixSourcePath, LLibrarian.LLibrarianFixLoad) is null)
        {
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Fix plan saved for '{System.IO.Path.GetFileName(pFixSourcePath)}': {PFixPlanFormat(pFixPlan)}");
        LFix.LFixPlanSave(pFixSourcePath, pFixPlan, LLibrarian.LLibrarianFixSave);
        PFixPersistentSave();
    }
}
