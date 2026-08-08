using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed class PEditTab : PTabSurface
{
    private const string PEditCropIcon = "/PAssets/PPanels/PProcessingCrop.svg";
    private const string PEditBrightnessIcon = "/PAssets/PPanels/PProcessingBrightness.svg";
    private const string PEditContrastIcon = "/PAssets/PPanels/PProcessingContrast.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new() { PViewerColorPreview = true };
    private readonly PInspector pInspector = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly LCropboxState pCropOwner = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private bool pEditPlanLoading;

    public PEditTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority =>
        {
            if (pList.PListEditableRead() is not { } pEditSelected)
            {
                return;
            }

            LMessenger.LMessengerEditDescribe(
                lPriority,
                pEditSelected.LDocketEntryPath,
                pViewer.PViewerDurationRead(),
                pInspector.PSkipActiveCheck() ? LWorkCrop.LWorkCropCreate() : pCropOwner.LCropboxStateCrop,
                pInspector.PSkipActiveCheck() ? LWorkVideo.LWorkVideoCreate() : PEditVideoRead(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                pEditSelected.LDocketEntryBatch);
        };

        pAction.PActionAllAdd += () => _ = LMessenger.LMessengerEditDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListUnlockedRead()
                .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                .ToArray(),
            lPresetOwner,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab);
        pAction.PActionItemsAdd += pEditPaths => _ = LMessenger.LMessengerEditDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListUnlockedRead()
                .Where(pItem => pEditPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                .ToArray(),
            lPresetOwner,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab);
        pAction.PActionAllSet(
            true,
            LLocalization.LLocalizationTextRead("Action.EditAll.Tooltip"));

        pProcessing.PProcessingOrderedSet(false);
        pProcessing.PProcessingStepAdd("Crop", PEditCropIcon, "Processing.Step.Crop");
        pProcessing.PProcessingStepAdd("Brightness", PEditBrightnessIcon, "Processing.Step.Brightness");
        pProcessing.PProcessingStepAdd("Contrast", PEditContrastIcon, "Processing.Step.Contrast");
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepChange += PEditStepHandle;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pInspector.PSkipActiveChange += PEditSkipHandle;
        pInspector.PInspectorPlanChange += PEditPersistentWrite;

        pInspector.PInspectorToolChange += pViewer.PCropToolSet;
        pInspector.PInspectorRatioChange += pViewer.PCropRatioSet;
        pInspector.PInspectorRatioChange += _ => PEditRatioWrite();
        pInspector.PInspectorRatioChange += _ => PEditPlanSave();
        pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
        pInspector.PInspectorCropChange += _ => PEditCropWrite();
        pInspector.PInspectorRotateChange += PEditRotateHandle;
        pInspector.PInspectorCropChange += _ => PEditPlanSave();
        pInspector.PInspectorRotateChange += _ => PEditCropWrite();
        pInspector.PInspectorRotateChange += _ => PEditPlanSave();
        pInspector.PInspectorPersistentChange += pPersistent => pViewer.PCropPersistent = pPersistent;
        pInspector.PInspectorPersistentChange += pCropOwner.LCropboxPersistentSet;
        pInspector.PCropActiveChange += PEditActiveWrite;
        pInspector.PCropActiveChange += PEditActiveUpdate;
        pInspector.PCropActiveChange += PEditPlanSave;
        pInspector.PInspectorVideoChange += PEditChangeHandle;
        pViewer.PCropVideoChange += PEditCropShow;
        pViewer.PViewerMediaChange += _ => PEditCropRestore();
        pList.PListPathChange += PEditPathShow;
        pList.PListItemsAdd += PEditItemsHandle;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);

        var pExport = new PExport(lPresetOwner, true);
        PTabLockAttach(pList, pProcessing, pInspector, pExport);
        pList.PListLockChange += pLocked =>
            pViewer.PCropToolSet(!pLocked && pInspector.PInspectorToolCheck());
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        if (lPreferenceTabLayout is null)
        {
            pInspector.PInspectorMinimizeSet(true);
        }

        Content = pTabGrid;
        PEditPersistentRestore(lPreferenceTabLayout);
        PEditActiveUpdate();
    }

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
                pInspector.PCropPlanApply(pEditPlan.LEditCrop, pEditPlan.LEditCropApply);
                pInspector.PInspectorRatioApply(pEditPlan.LEditRatioFixed, pEditPlan.LEditRatioLenient, pEditPlan.LEditRatioWidth, pEditPlan.LEditRatioHeight);
                pInspector.PCropPersistentApply(true);
                pCropOwner.LCropboxStateSet(
                    pEditPlan.LEditCrop,
                    pEditPlan.LEditCropApply,
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

    private void PEditSkipHandle()
    {
        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PEditPlanSave();
    }

    private void PEditPersistentWrite()
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

    private void PEditStepHandle(string? pStepName)
    {
        if (string.IsNullOrEmpty(pStepName) || pStepName == "No Processing")
        {
            return;
        }

        if (pInspector.PSkipPersistentCheck())
        {
            pInspector.PSkipPersistentApply(false);
        }

        if (pInspector.PSkipActiveCheck())
        {
            pInspector.PSkipApply(false);
        }
    }

    private void PEditCropWrite() => pCropOwner.LCropboxCropSet(pInspector.PInspectorCropRead());

    private void PEditRatioWrite()
    {
        (bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) = pInspector.PInspectorRatioRead();
        pCropOwner.LCropboxRatioSet(pRatioFixed, pRatioLenient, pRatioWidth, pRatioHeight);
    }

    private void PEditActiveWrite() => pCropOwner.LCropboxApplySet(pInspector.PCropActiveCheck());

    private void PEditActiveUpdate()
    {
        pProcessing.PProcessingActiveSet("Crop", pCropOwner.LCropboxStateApply);
        pProcessing.PProcessingActiveSet("Brightness",
            pInspector.PToneStepRead(LColorKind.LColorKindBrightness).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Contrast",
            pInspector.PToneStepRead(LColorKind.LColorKindContrast).LWorkStepActive);
    }

    private void PEditChangeHandle()
    {
        PEditActiveUpdate();
        PEditColorApply();
        PEditPlanSave();
    }

    private void PEditPathShow(string? pSourcePath)
    {
        if (string.IsNullOrWhiteSpace(pSourcePath))
        {
            LTraceLog.LTraceInfoRecord("Edit click: no file selected");
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Edit click '{System.IO.Path.GetFileName(pSourcePath)}': "
            + $"persistent {(pInspector.PCropPersistentCheck() ? "on" : "off")}, "
            + $"inspector now {PEditCropFormat(pInspector.PInspectorCropRead())}");
        pViewer.PViewerSourceOpen(pSourcePath);
    }

    private void PEditCropShow(System.Windows.Rect? pCropVideo)
    {
        if (pViewer.PCropSourceRead() is System.Windows.Size pCropSource)
        {
            pInspector.PInspectorSourceSet(pCropSource.Width, pCropSource.Height);
        }
        else
        {
            pInspector.PInspectorSourceSet(0, 0);
        }

        LTraceLog.LTraceInfoRecord($"Edit crop from viewer: {PEditRectFormat(pCropVideo)}");
        (int pCropDrive, int pCropAnchorX, int pCropAnchorY) = pViewer.PCropAnchorRead();
        pInspector.PInspectorCropSet(pCropVideo, pCropDrive, pCropAnchorX, pCropAnchorY);
    }

    private void PEditCropRestore()
    {
        string pEditName = pViewer.PViewerSourcePath is { } pEditPath
            ? System.IO.Path.GetFileName(pEditPath)
            : "(no media)";

        pEditPlanLoading = true;
        pInspector.PInspectorCropChange -= pViewer.PCropVideoSet;
        pInspector.PInspectorRotateChange -= PEditRotateHandle;
        try
        {
            System.Windows.Size? pCropSource = pViewer.PCropSourceRead();
            if (pCropSource is { } pCropSize)
            {
                pInspector.PInspectorSourceSet(pCropSize.Width, pCropSize.Height);
            }
            else
            {
                pInspector.PInspectorSourceSet(0, 0);
            }

            LEditPlan? pEditPersistent = PEditCarriedRead();
            LEditPlan? pEditSaved = pViewer.PViewerSourcePath is { } pEditSourcePath
                ? LEdit.LEditPlanRead(pEditSourcePath, LLibrarian.LLibrarianEditLoad)
                : null;

            LTraceLog.LTraceInfoRecord(
                $"Edit media ready '{pEditName}': "
                + $"display {(pCropSource is { } pLogSize ? $"{pLogSize.Width:0}x{pLogSize.Height:0}" : "unknown")}, "
                + $"persistent {(pEditPersistent is null ? "off" : "on")}, "
                + $"carried {PEditPlanFormat(pEditPersistent)}, "
                + $"sidecar {PEditPlanFormat(pEditSaved)}");

            pInspector.PCropMediaReset();

            bool pEditCarryWins = pEditPersistent is not null;
            LEditPlan pEditPlan = LEdit.LEditPlanResolve(pEditSaved, pEditPersistent);

            if (pEditPlan is { LEditPlanActive: true } pEditApply)
            {
                LTraceLog.LTraceInfoRecord(
                    $"Edit applying {(pEditCarryWins ? "persistent" : "sidecar")} plan to '{pEditName}': "
                    + $"{PEditPlanFormat(pEditApply)}");
                pViewer.PViewerRotateSet(PEditRotateResolve(pEditApply.LEditCrop));
                if (pViewer.PCropSourceRead() is { } pEditRotatedSource)
                {
                    pInspector.PInspectorSourceSet(pEditRotatedSource.Width, pEditRotatedSource.Height);
                }

                pInspector.PCropPlanApply(pEditApply.LEditCrop, pEditApply.LEditCropApply);
                pInspector.PInspectorRatioApply(pEditApply.LEditRatioFixed, pEditApply.LEditRatioLenient, pEditApply.LEditRatioWidth, pEditApply.LEditRatioHeight);
                pInspector.PTonePlanApply(pEditApply.LEditVideo);
                pInspector.PSkipApply(pEditApply.LEditSkip);
                pCropOwner.LCropboxStateSet(
                    pEditApply.LEditCrop,
                    pEditApply.LEditCropApply,
                    pEditApply.LEditRatioFixed,
                    pEditApply.LEditRatioLenient,
                    pEditApply.LEditRatioWidth,
                    pEditApply.LEditRatioHeight);
            }
            else
            {
                LTraceLog.LTraceInfoRecord($"Edit applying no plan to '{pEditName}': inspector left cleared");
                pViewer.PViewerRotateSet(LRotateFlip.LRotateDefaultCreate());
                pInspector.PInspectorRatioReset();
                pInspector.PTonePlanApply(LWorkVideo.LWorkVideoCreate());
                pInspector.PSkipApply(false);
                pCropOwner.LCropboxStateReset();
            }
        }
        finally
        {
            pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
            pInspector.PInspectorRotateChange += PEditRotateHandle;
            pEditPlanLoading = false;
        }

        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PEditViewerApply();
        PEditPlanSave();
    }

    private void PEditViewerApply()
    {
        LRotateFlip pEditRotate = pInspector.PInspectorRotateRead();
        System.Windows.Rect? pEditRect = pInspector.PInspectorRectRead();
        LTraceLog.LTraceInfoRecord(
            $"Edit viewer push: rotate {pEditRotate.LRotateKind}, "
            + $"H {pEditRotate.LRotateFlipHorizontal}, V {pEditRotate.LRotateFlipVertical}, "
            + $"{PEditRectFormat(pEditRect)}");

        pViewer.PViewerRotateSet(pEditRotate);
        pViewer.PCropVideoSet(pEditRect);
        PEditColorApply();
    }

    private void PEditRotateHandle(LRotateFlip pRotateFlip)
    {
        pViewer.PViewerRotateSet(pRotateFlip);
        if (pViewer.PCropSourceRead() is { } pRotatedSource)
        {
            pInspector.PInspectorSourceSet(pRotatedSource.Width, pRotatedSource.Height);
        }
    }

    private void PEditColorApply()
    {
        pViewer.PViewerColorSet(LPreview.LPreviewColorResolve(PEditVideoRead()));
    }

    private static string PEditRectFormat(System.Windows.Rect? pEditRect) =>
        pEditRect is { } pRect
            ? $"rect {pRect.X:0},{pRect.Y:0} {pRect.Width:0}x{pRect.Height:0}"
            : "rect none";

    private static string PEditCropFormat(LWorkCrop? pEditCrop)
    {
        if (pEditCrop is not { } pCrop)
        {
            return "none";
        }

        if (!pCrop.LWorkCropActive)
        {
            return "inactive";
        }

        string pEdges = pCrop.LWorkEdgeActive
            ? $"edges {pCrop.LWorkCropLeft}/{pCrop.LWorkCropTop}/{pCrop.LWorkCropRight}/{pCrop.LWorkCropBottom}"
            : "no edges";
        string pFlip = pCrop.LWorkFlipHorizontal || pCrop.LWorkFlipVertical
            ? $"flip {(pCrop.LWorkFlipHorizontal ? "H" : "")}{(pCrop.LWorkFlipVertical ? "V" : "")}"
            : "no flip";
        return $"{pEdges}, rotate {pCrop.LWorkCropRotation}, {pFlip}";
    }

    private static string PEditPlanFormat(LEditPlan? pEditPlan) =>
        pEditPlan is null
            ? "none"
            : $"{PEditCropFormat(pEditPlan.LEditCrop)}, {PEditVideoFormat(pEditPlan.LEditVideo)}";

    private static string PEditVideoFormat(LWorkVideo pEditVideo)
    {
        if (!pEditVideo.LWorkVideoActive)
        {
            return "video inactive";
        }

        return string.Join(", ", pEditVideo.LWorkVideoSteps
            .Where(pStep => pStep.LWorkStepActive)
            .Select(pStep => $"{pStep.LWorkStepKind} {pStep.LWorkStepValue:0.###}"));
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

        bool pCropApply = pCropPersistent && pCropOwner.LCropboxStateApply;
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

    private LWorkVideo PEditVideoRead()
    {
        var pSteps = new List<LWorkVideoStep>();
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (LColor.LColorKindParse(pStepName) is { } pKind)
            {
                pSteps.Add(pInspector.PToneStepRead(pKind));
            }
        }

        return new LWorkVideo(pSteps);
    }

    private static LRotateFlip PEditRotateResolve(LWorkCrop pEditCrop) => new(
        pEditCrop.LWorkCropRotation switch
        {
            90 => LRotateKind.LRotate90,
            180 => LRotateKind.LRotate180,
            270 => LRotateKind.LRotate270,
            _ => LRotateKind.LRotateNone
        },
        pEditCrop.LWorkFlipHorizontal,
        pEditCrop.LWorkFlipVertical);

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
            pCropOwner.LCropboxStateApply)
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
        PEditPersistentWrite();
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        bool pCropPersistent = pCropOwner.LCropboxStatePersistent;
        bool pVideoPersistent = pInspector.PTonePersistentCheck();
        bool pSkipPersistent = pInspector.PSkipPersistentCheck();
        if (pCropPersistent || pVideoPersistent || pSkipPersistent)
        {
            (bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) = pCropOwner.LCropboxStateRatio;
            var pEditCarried = new LEditPlan(
                pCropPersistent ? pCropOwner.LCropboxStateCrop : LWorkCrop.LWorkCropCreate(),
                pInspector.PTonePersistentRead(),
                pCropPersistent && pCropOwner.LCropboxStateApply)
            {
                LEditSkip = pSkipPersistent && pInspector.PSkipActiveCheck(),
                LEditRatioFixed = pCropPersistent && pRatioFixed,
                LEditRatioLenient = pCropPersistent && pRatioLenient,
                LEditRatioWidth = pCropPersistent ? pRatioWidth : 0,
                LEditRatioHeight = pCropPersistent ? pRatioHeight : 0
            };
            lPreferenceTabLayout.LSceneInspector = new LSceneInspectorRecord
            {
                LSceneInspectorEdit = LEdit.LEditPersistentCreate(pEditCarried),
                LSceneInspectorCrop = pCropPersistent,
                LSceneInspectorSkip = pSkipPersistent
            };
        }

        return lPreferenceTabLayout;
    }
}
