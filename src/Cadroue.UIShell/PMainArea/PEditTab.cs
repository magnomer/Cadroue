using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed class PEditTab : PTabSurface
{
    private const string PEditCropIcon = "/PAssets/PPanels/PProcessingCrop.svg";
    private const string PEditBrightnessIcon = "/PAssets/PPanels/PProcessingBrightness.svg";
    private const string PEditContrastIcon = "/PAssets/PPanels/PProcessingContrast.svg";
    private const double PEditPreviewFactor = 2.5;

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new() { PViewerColorPreview = true };
    private readonly PInspector pInspector = new();
    private readonly PList pList = new();
    private readonly PProcessing pProcessing = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private bool pEditPlanLoading;

    public PEditTab(LPreset lExportSpecificState, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority => LEdit.LEditDescribe(
            lPriority,
            pViewer.PViewerSourcePath,
            pViewer.PViewerDurationRead(),
            pInspector.PSkipActiveCheck() ? LWorkCrop.LWorkCropCreate() : pInspector.PInspectorCropRead(),
            pInspector.PSkipActiveCheck() ? LWorkVideo.LWorkVideoCreate() : PEditVideoRead(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab);

        pAction.PActionAllAdd += () => _ = LEdit.LEditAllDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListItemsRead()
                .Select(pItem => new LWorkSource(pItem.PListItemPath, pItem.PListItemRelay))
                .ToArray(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab);
        pAction.PActionItemsAdd += pEditPaths => _ = LEdit.LEditAllDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListItemsRead()
                .Where(pItem => pEditPaths.Contains(pItem.PListItemPath, StringComparer.OrdinalIgnoreCase))
                .Select(pItem => new LWorkSource(pItem.PListItemPath, pItem.PListItemRelay))
                .ToArray(),
            lExportSpecificState,
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
        pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
        pInspector.PInspectorRotateChange += pViewer.PViewerRotateSet;
        pInspector.PInspectorCropChange += _ => PEditPlanSave();
        pInspector.PInspectorRotateChange += _ => PEditPlanSave();
        pInspector.PInspectorPersistentChange += pPersistent => pViewer.PCropPersistent = pPersistent;
        pInspector.PCropActiveChange += PEditActiveUpdate;
        pInspector.PCropActiveChange += PEditPlanSave;
        pInspector.PInspectorVideoChange += PEditChangeHandle;
        pViewer.PCropVideoChange += PEditCropShow;
        pViewer.PViewerMediaChange += _ => PEditCropRestore();
        pList.PListPathChange += PEditPathShow;
        pList.PListItemsAdd += PEditItemsHandle;
        PTabViewerAttach(pList, pViewer);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);

        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, new PExport(lExportSpecificState, true) }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
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
                pInspector.PCropPersistentApply(true);
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

        foreach (string pEditPath in pList.PListPathsRead())
        {
            LEdit.LEditPlanSave(pEditPath, LEdit.LEditPlanResolve(LEdit.LEditPlanRead(pEditPath), pEditCarried));
        }
    }

    private void PEditItemsHandle(IReadOnlyList<PListItem> pEditAddedItems)
    {
        if (pEditPlanLoading || PEditCarriedRead() is not { } pEditCarried)
        {
            return;
        }

        foreach (PListItem pEditAddedItem in pEditAddedItems)
        {
            string pEditPath = pEditAddedItem.PListItemPath;
            LEdit.LEditPlanSave(pEditPath, LEdit.LEditPlanResolve(LEdit.LEditPlanRead(pEditPath), pEditCarried));
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

    private void PEditActiveUpdate()
    {
        pProcessing.PProcessingActiveSet("Crop", pInspector.PCropActiveCheck());
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

        LTraceLog.LTraceInfoRecord($"Edit crop from viewer: {PEditRectFormat(pCropVideo)}");
        pInspector.PInspectorCropSet(pCropVideo);
    }

    private void PEditCropRestore()
    {
        string pEditName = pViewer.PViewerSourcePath is { } pEditPath
            ? System.IO.Path.GetFileName(pEditPath)
            : "(no media)";

        pEditPlanLoading = true;
        pInspector.PInspectorCropChange -= pViewer.PCropVideoSet;
        pInspector.PInspectorRotateChange -= pViewer.PViewerRotateSet;
        try
        {
            System.Windows.Size? pCropSource = pViewer.PCropSourceRead();
            if (pCropSource is { } pCropSize)
            {
                pInspector.PInspectorSourceSet(pCropSize.Width, pCropSize.Height);
            }

            LEditPlan? pEditPersistent = PEditCarriedRead();
            LEditPlan? pEditSaved = pViewer.PViewerSourcePath is { } pEditSourcePath
                ? LEdit.LEditPlanRead(pEditSourcePath)
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
                pInspector.PCropPlanApply(pEditApply.LEditCrop, pEditApply.LEditCropApply);
                pInspector.PTonePlanApply(pEditApply.LEditVideo);
                pInspector.PSkipApply(pEditApply.LEditSkip);
            }
            else
            {
                LTraceLog.LTraceInfoRecord($"Edit applying no plan to '{pEditName}': inspector left cleared");
                pInspector.PTonePlanApply(LWorkVideo.LWorkVideoCreate());
                pInspector.PSkipApply(false);
            }
        }
        finally
        {
            pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
            pInspector.PInspectorRotateChange += pViewer.PViewerRotateSet;
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

    private void PEditColorApply()
    {
        LWorkVideo pVideo = PEditVideoRead();
        double pBrightness = pVideo.LWorkVideoSteps
            .FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindBrightness
                && pStep.LWorkStepActive)
            ?.LWorkFfmpegValue * PEditPreviewFactor ?? 0;
        double pContrast = pVideo.LWorkVideoSteps
            .FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindContrast
                && pStep.LWorkStepActive)
            ?.LWorkFfmpegValue ?? 1;
        pViewer.PViewerColorSet(new LColor(pBrightness, pContrast, 1, 0));
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
        bool pCropPersistent = pInspector.PCropPersistentCheck();
        bool pVideoPersistent = pInspector.PTonePersistentCheck();
        bool pSkipPersistent = pInspector.PSkipPersistentCheck();
        if (!pCropPersistent && !pVideoPersistent && !pSkipPersistent)
        {
            return null;
        }

        bool pCropApply = pCropPersistent && pInspector.PCropActiveCheck();
        LWorkCrop pCrop = pCropPersistent
            ? pInspector.PInspectorCropRead()
            : LWorkCrop.LWorkCropCreate();
        LWorkVideo pVideo = pVideoPersistent
            ? pInspector.PTonePersistentRead()
            : LWorkVideo.LWorkVideoCreate();
        bool pSkip = pSkipPersistent && pInspector.PSkipActiveCheck();
        return new LEditPlan(pCrop, pVideo, pCropApply) { LEditSkip = pSkip };
    }

    private LWorkVideo PEditVideoRead()
    {
        var pSteps = new List<LWorkVideoStep>();
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (PEditKindRead(pStepName) is LColorKind pKind)
            {
                pSteps.Add(pInspector.PToneStepRead(pKind));
            }
        }

        return new LWorkVideo(pSteps);
    }

    private static LColorKind? PEditKindRead(string pStepName) => pStepName switch
    {
        "Brightness" => LColorKind.LColorKindBrightness,
        "Contrast" => LColorKind.LColorKindContrast,
        _ => null
    };

    private void PEditPlanSave()
    {
        if (pEditPlanLoading || pViewer.PViewerSourcePath is not { } pEditSourcePath)
        {
            return;
        }

        var pEditPlan = new LEditPlan(
            pInspector.PInspectorCropRead(),
            PEditVideoRead(),
            pInspector.PCropActiveCheck()) { LEditSkip = pInspector.PSkipActiveCheck() };
        if (!pEditPlan.LEditPlanActive && LEdit.LEditPlanRead(pEditSourcePath) is null)
        {
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Edit plan saved for '{System.IO.Path.GetFileName(pEditSourcePath)}': {PEditPlanFormat(pEditPlan)}");
        LEdit.LEditPlanSave(pEditSourcePath, pEditPlan);
        PEditPersistentWrite();
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        bool pCropPersistent = pInspector.PCropPersistentCheck();
        bool pVideoPersistent = pInspector.PTonePersistentCheck();
        bool pSkipPersistent = pInspector.PSkipPersistentCheck();
        if (pCropPersistent || pVideoPersistent || pSkipPersistent)
        {
            var pEditCarried = new LEditPlan(
                pCropPersistent ? pInspector.PInspectorCropRead() : LWorkCrop.LWorkCropCreate(),
                pInspector.PTonePersistentRead(),
                pCropPersistent && pInspector.PCropActiveCheck())
            {
                LEditSkip = pSkipPersistent && pInspector.PSkipActiveCheck()
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
