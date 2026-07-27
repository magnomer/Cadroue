using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PEditTab : PTabSurface
{
    private const string PEditCropIconPath = "/PAssets/PPanels/PProcessingCrop.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PInspector pInspector = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PEditTab(LExportSpecificState lExportSpecificState, LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        pAction.PActionRun += lPriority => LEdit.LEditDescribe(
            lPriority,
            pViewer.PViewerSourcePath,
            pViewer.PViewerDurationRead(),
            pInspector.PInspectorCropRead(),
            lExportSpecificState);

        var pProcessing = new PProcessing();
        pProcessing.PProcessingStepAdd("Crop", PEditCropIconPath);
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;

        pInspector.PInspectorToolChange += pViewer.PCropToolSet;
        pInspector.PInspectorRatioChange += pViewer.PCropRatioSet;
        pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
        pInspector.PInspectorRotateChange += pViewer.PViewerRotateSet;
        pViewer.PCropVideoChange += PEditCropShow;

        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pProcessing, pInspector, pViewer, new PExport(lExportSpecificState) }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
    }

    private void PEditCropShow(System.Windows.Rect? pCropVideo)
    {
        if (pViewer.PCropSourceRead() is System.Windows.Size pCropSource)
        {
            pInspector.PInspectorSourceSet(pCropSource.Width, pCropSource.Height);
        }

        pInspector.PInspectorCropSet(pCropVideo);
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
