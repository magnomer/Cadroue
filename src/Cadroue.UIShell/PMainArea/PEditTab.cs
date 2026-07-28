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
            lExportSpecificState);
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
        if (!string.IsNullOrWhiteSpace(pSourcePath))
        {
            pViewer.PViewerSourceOpen(pSourcePath);
        }
    }

    private void PEditCropShow(System.Windows.Rect? pCropVideo)
    {
        if (pViewer.PCropSourceRead() is System.Windows.Size pCropSource)
        {
            pInspector.PInspectorSourceSet(pCropSource.Width, pCropSource.Height);
        }

        pInspector.PInspectorCropSet(pCropVideo);
    }

    private void PEditCropRestore()
    {
        pEditPlanLoading = true;
        try
        {
            if (pViewer.PCropSourceRead() is System.Windows.Size pCropSource)
            {
                pInspector.PInspectorSourceSet(pCropSource.Width, pCropSource.Height);
            }

            pInspector.PInspectorMediaReset();
            if (pViewer.PViewerSourcePath is { } pEditSourcePath
                && LEdit.LEditPlanRead(pEditSourcePath) is { LWorkCropActive: true } pEditPlan)
            {
                pInspector.PInspectorPlanApply(pEditPlan);
            }
        }
        finally
        {
            pEditPlanLoading = false;
        }
    }

    private void PEditPlanSave()
    {
        if (pEditPlanLoading || pViewer.PViewerSourcePath is not { } pEditSourcePath)
        {
            return;
        }

        LEdit.LEditPlanSave(pEditSourcePath, pInspector.PInspectorCropRead());
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
