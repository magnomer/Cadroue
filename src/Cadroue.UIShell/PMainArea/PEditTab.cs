using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PEditTab : PTabSurface
{
    private const string PEditCropIconPath = "/PAssets/PPanels/PProcessingCrop.svg";
    private const string PEditBrightnessIconPath = "/PAssets/PPanels/PProcessingBrightness.svg";
    private const string PEditContrastIconPath = "/PAssets/PPanels/PProcessingContrast.svg";
    private const double PEditBrightnessPreviewFactor = 2.5;

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new() { PViewerColorPreview = true };
    private readonly PInspector pInspector = new();
    private readonly PList pList = new();
    private readonly PProcessing pProcessing = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private bool pEditPlanLoading;

    public PEditTab(LExportSpecificState lExportSpecificState, LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        pAction.PActionRun += lPriority => LEdit.LEditDescribe(
            lPriority,
            pViewer.PViewerSourcePath,
            pViewer.PViewerDurationRead(),
            pInspector.PInspectorCropRead(),
            PEditVideoRead(),
            lExportSpecificState);

        pAction.PActionAllAdd += () => _ = LEdit.LEditAllDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListPathsRead(),
            lExportSpecificState,
            PEditCarriedRead());
        pAction.PActionAllSet(
            true,
            LLocalization.LLocalizationTextRead("Action.EditAll.Tooltip"));

        pProcessing.PProcessingOrderedSet(false);
        pProcessing.PProcessingStepAdd("Crop", PEditCropIconPath, "Processing.Step.Crop");
        pProcessing.PProcessingStepAdd("Brightness", PEditBrightnessIconPath, "Processing.Step.Brightness");
        pProcessing.PProcessingStepAdd("Contrast", PEditContrastIconPath, "Processing.Step.Contrast");
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);

        pInspector.PInspectorToolChange += pViewer.PCropToolSet;
        pInspector.PInspectorRatioChange += pViewer.PCropRatioSet;
        pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
        pInspector.PInspectorRotateChange += pViewer.PViewerRotateSet;
        pInspector.PInspectorCropChange += _ => PEditPlanSave();
        pInspector.PInspectorRotateChange += _ => PEditPlanSave();
        pInspector.PInspectorPersistentChange += pPersistent => pViewer.PCropPersistent = pPersistent;
        pInspector.PInspectorCropActiveChange += PEditActiveRefresh;
        pInspector.PInspectorCropActiveChange += PEditPlanSave;
        pInspector.PInspectorVideoChange += PEditVideoChangeHandle;
        pViewer.PCropVideoChange += PEditCropShow;
        pViewer.PViewerMediaChange += _ => PEditCropRestore();
        pList.PListPathChange += PEditPathShow;
        PTabViewerAttach(pList, pViewer);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);

        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, new PExport(lExportSpecificState, true) }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        if (lPreferenceTabLayout is null)
        {
            pInspector.PInspectorMinimizeSet(true);
        }

        Content = pTabGrid;
        PEditActiveRefresh();
    }

    private void PEditActiveRefresh()
    {
        pProcessing.PProcessingActiveSet("Crop", pInspector.PInspectorCropActiveCheck());
        pProcessing.PProcessingActiveSet("Brightness",
            pInspector.PInspectorVideoStepRead(LWorkVideoKind.LWorkVideoKindBrightness).LWorkVideoStepActive);
        pProcessing.PProcessingActiveSet("Contrast",
            pInspector.PInspectorVideoStepRead(LWorkVideoKind.LWorkVideoKindContrast).LWorkVideoStepActive);
    }

    private void PEditVideoChangeHandle()
    {
        PEditActiveRefresh();
        PEditViewerColorPush();
        PEditPlanSave();
    }

    private void PEditPathShow(string? pSourcePath)
    {
        if (string.IsNullOrWhiteSpace(pSourcePath))
        {
            LAppLog.LInfo("Edit click: no file selected");
            return;
        }

        LAppLog.LInfo(
            $"Edit click '{System.IO.Path.GetFileName(pSourcePath)}': "
            + $"persistent {(pInspector.PInspectorPersistentCheck() ? "on" : "off")}, "
            + $"inspector now {PEditCropFormat(pInspector.PInspectorCropRead())}");
        pViewer.PViewerSourceOpen(pSourcePath);
    }

    private void PEditCropShow(System.Windows.Rect? pCropVideo)
    {
        if (pViewer.PCropSourceRead() is System.Windows.Size pCropSource)
        {
            pInspector.PInspectorSourceSet(pCropSource.Width, pCropSource.Height);
        }

        LAppLog.LInfo($"Edit crop from viewer: {PEditRectFormat(pCropVideo)}");
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

            LAppLog.LInfo(
                $"Edit media ready '{pEditName}': "
                + $"display {(pCropSource is { } pLogSize ? $"{pLogSize.Width:0}x{pLogSize.Height:0}" : "unknown")}, "
                + $"persistent {(pEditPersistent is null ? "off" : "on")}, "
                + $"carried {PEditPlanFormat(pEditPersistent)}, "
                + $"sidecar {PEditPlanFormat(pEditSaved)}");

            pInspector.PInspectorMediaReset();

            bool pEditCarryWins = pEditPersistent is not null;
            LEditPlan pEditPlan = LEdit.LEditPlanResolve(pEditSaved, pEditPersistent);

            if (pEditPlan is { LEditPlanActive: true } pEditApply)
            {
                LAppLog.LInfo(
                    $"Edit applying {(pEditCarryWins ? "persistent" : "sidecar")} plan to '{pEditName}': "
                    + $"{PEditPlanFormat(pEditApply)}");
                pInspector.PInspectorPlanApply(pEditApply.LEditCrop, pEditApply.LEditCropApply);
                pInspector.PInspectorVideoPlanApply(pEditApply.LEditVideo);
            }
            else
            {
                LAppLog.LInfo($"Edit applying no plan to '{pEditName}': inspector left cleared");
                pInspector.PInspectorVideoPlanApply(LWorkVideo.LWorkVideoNoneCreate());
            }
        }
        finally
        {
            pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
            pInspector.PInspectorRotateChange += pViewer.PViewerRotateSet;
            pEditPlanLoading = false;
        }

        PEditViewerPush();
        PEditPlanSave();
    }

    private void PEditViewerPush()
    {
        LRotateFlip pEditRotate = pInspector.PInspectorRotateRead();
        System.Windows.Rect? pEditRect = pInspector.PInspectorRectRead();
        LAppLog.LInfo(
            $"Edit viewer push: rotate {pEditRotate.LRotateKind}, "
            + $"H {pEditRotate.LRotateFlipHorizontal}, V {pEditRotate.LRotateFlipVertical}, "
            + $"{PEditRectFormat(pEditRect)}");

        pViewer.PViewerRotateSet(pEditRotate);
        pViewer.PCropVideoSet(pEditRect);
        PEditViewerColorPush();
    }

    private void PEditViewerColorPush()
    {
        LWorkVideo pVideo = PEditVideoRead();
        double pBrightness = pVideo.LWorkVideoSteps
            .FirstOrDefault(pStep => pStep.LWorkVideoStepKind == LWorkVideoKind.LWorkVideoKindBrightness
                && pStep.LWorkVideoStepActive)
            ?.LWorkVideoFfmpegValue * PEditBrightnessPreviewFactor ?? 0;
        double pContrast = pVideo.LWorkVideoSteps
            .FirstOrDefault(pStep => pStep.LWorkVideoStepKind == LWorkVideoKind.LWorkVideoKindContrast
                && pStep.LWorkVideoStepActive)
            ?.LWorkVideoFfmpegValue ?? 1;
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

        string pEdges = pCrop.LWorkCropEdgeActive
            ? $"edges {pCrop.LWorkCropLeft}/{pCrop.LWorkCropTop}/{pCrop.LWorkCropRight}/{pCrop.LWorkCropBottom}"
            : "no edges";
        string pFlip = pCrop.LWorkCropFlipHorizontal || pCrop.LWorkCropFlipVertical
            ? $"flip {(pCrop.LWorkCropFlipHorizontal ? "H" : "")}{(pCrop.LWorkCropFlipVertical ? "V" : "")}"
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
            .Where(pStep => pStep.LWorkVideoStepActive)
            .Select(pStep => $"{pStep.LWorkVideoStepKind} {pStep.LWorkVideoStepValue:0.###}"));
    }

    private LEditPlan? PEditCarriedRead()
    {
        bool pCropApply = pInspector.PInspectorPersistentCheck() && pInspector.PInspectorCropActiveCheck();
        LWorkCrop pCrop = pInspector.PInspectorPersistentCheck()
            ? pInspector.PInspectorCropRead()
            : LWorkCrop.LWorkCropNoneCreate();
        LWorkVideo pVideo = pInspector.PInspectorVideoPersistentAnyCheck()
            ? pInspector.PInspectorVideoPersistentRead()
            : LWorkVideo.LWorkVideoNoneCreate();
        return pCropApply || pCrop.LWorkCropActive || pVideo.LWorkVideoActive
            ? new LEditPlan(pCrop, pVideo, pCropApply)
            : null;
    }

    private LWorkVideo PEditVideoRead()
    {
        var pSteps = new List<LWorkVideoStep>();
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (PEditVideoKindRead(pStepName) is LWorkVideoKind pKind)
            {
                pSteps.Add(pInspector.PInspectorVideoStepRead(pKind));
            }
        }

        return new LWorkVideo(pSteps);
    }

    private static LWorkVideoKind? PEditVideoKindRead(string pStepName) => pStepName switch
    {
        "Brightness" => LWorkVideoKind.LWorkVideoKindBrightness,
        "Contrast" => LWorkVideoKind.LWorkVideoKindContrast,
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
            pInspector.PInspectorCropActiveCheck());
        if (!pEditPlan.LEditPlanActive && LEdit.LEditPlanRead(pEditSourcePath) is null)
        {
            return;
        }

        LAppLog.LInfo(
            $"Edit plan saved for '{System.IO.Path.GetFileName(pEditSourcePath)}': {PEditPlanFormat(pEditPlan)}");
        LEdit.LEditPlanSave(pEditSourcePath, pEditPlan);
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
