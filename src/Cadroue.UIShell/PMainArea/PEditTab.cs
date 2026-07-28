using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PEditTab : PTabSurface
{
    private const string PEditCropIconPath = "/PAssets/PPanels/PProcessingCrop.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PInspector pInspector = new();
    private readonly PList pList = new();
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
            lExportSpecificState);

        pAction.PActionAllAdd += () => _ = LEdit.LEditAllDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListPathsRead(),
            lExportSpecificState,
            PEditCarriedRead());
        pAction.PActionAllSet(true, "Add every loaded file that has a processing plan saved beside it");

        var pProcessing = new PProcessing();
        pProcessing.PProcessingStepAdd("Crop", PEditCropIconPath);
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;

        pInspector.PInspectorToolChange += pViewer.PCropToolSet;
        pInspector.PInspectorRatioChange += pViewer.PCropRatioSet;
        pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
        pInspector.PInspectorRotateChange += pViewer.PViewerRotateSet;
        pInspector.PInspectorCropChange += _ => PEditPlanSave();
        pInspector.PInspectorRotateChange += _ => PEditPlanSave();
        pInspector.PInspectorPersistentChange += pPersistent => pViewer.PCropPersistent = pPersistent;
        pViewer.PCropVideoChange += PEditCropShow;
        pViewer.PViewerMediaChange += _ => PEditCropRestore();
        pList.PListPathChange += PEditPathShow;
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);

        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, new PExport(lExportSpecificState) }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
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

            bool pEditPersistent = pInspector.PInspectorPersistentCheck();
            LWorkCrop pEditCarried = pInspector.PInspectorCropRead();
            LWorkCrop? pEditSaved = pViewer.PViewerSourcePath is { } pEditSourcePath
                ? LEdit.LEditPlanRead(pEditSourcePath)
                : null;

            LAppLog.LInfo(
                $"Edit media ready '{pEditName}': "
                + $"display {(pCropSource is { } pLogSize ? $"{pLogSize.Width:0}x{pLogSize.Height:0}" : "unknown")}, "
                + $"persistent {(pEditPersistent ? "on" : "off")}, "
                + $"carried {PEditCropFormat(pEditCarried)}, "
                + $"sidecar {PEditCropFormat(pEditSaved)}");

            pInspector.PInspectorMediaReset();

            bool pEditCarryWins = pEditPersistent && pEditCarried.LWorkCropActive;
            LWorkCrop? pEditPlan = pEditCarryWins ? pEditCarried : pEditSaved;

            if (pEditPlan is { LWorkCropActive: true } pEditApply)
            {
                LAppLog.LInfo(
                    $"Edit applying {(pEditCarryWins ? "persistent" : "sidecar")} plan to '{pEditName}': "
                    + $"{PEditCropFormat(pEditApply)}");
                pInspector.PInspectorPlanApply(pEditApply);
            }
            else
            {
                LAppLog.LInfo($"Edit applying no plan to '{pEditName}': inspector left cleared");
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

    private LWorkCrop? PEditCarriedRead()
    {
        if (!pInspector.PInspectorPersistentCheck())
        {
            return null;
        }

        LWorkCrop pEditCarried = pInspector.PInspectorCropRead();
        return pEditCarried.LWorkCropActive ? pEditCarried : null;
    }

    private void PEditPlanSave()
    {
        if (pEditPlanLoading || pViewer.PViewerSourcePath is not { } pEditSourcePath)
        {
            return;
        }

        LWorkCrop pEditCrop = pInspector.PInspectorCropRead();
        if (!pEditCrop.LWorkCropActive && LEdit.LEditPlanRead(pEditSourcePath) is null)
        {
            return;
        }

        LAppLog.LInfo(
            $"Edit plan saved for '{System.IO.Path.GetFileName(pEditSourcePath)}': {PEditCropFormat(pEditCrop)}");
        LEdit.LEditPlanSave(pEditSourcePath, pEditCrop);
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
