using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PAudioTab : PTabSurface
{
    private const string PAudioVolumeIconPath = "/PAssets/PPanels/PProcessingVolume.svg";
    private const string PAudioNormalizeIconPath = "/PAssets/PPanels/PProcessingNormalize.svg";
    private const string PAudioNoiseIconPath = "/PAssets/PPanels/PProcessingNoiseReduction.svg";
    private const string PAudioHighPassIconPath = "/PAssets/PPanels/PProcessingHighPass.svg";
    private const string PAudioLowPassIconPath = "/PAssets/PPanels/PProcessingLowPass.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new();
    private readonly PProcessing pProcessing = new();
    private readonly PInspector pInspector = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PAudioTab(LExportSpecificState lExportSpecificState, LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        pProcessing.PProcessingOrderedSet(true);
        pProcessing.PProcessingStepAdd("High Pass", PAudioHighPassIconPath);
        pProcessing.PProcessingStepAdd("Low Pass", PAudioLowPassIconPath);
        pProcessing.PProcessingStepAdd("Noise Reduction", PAudioNoiseIconPath);
        pProcessing.PProcessingStepAdd("Volume", PAudioVolumeIconPath);
        pProcessing.PProcessingStepAdd("Normalize", PAudioNormalizeIconPath);
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pInspector.PInspectorAudioActiveChange += PAudioActiveRefresh;

        var pAction = new PAction();
        pAction.PActionRun += lPriority => LAudio.LAudioDescribe(
            lPriority,
            pViewer.PViewerSourcePath,
            PAudioProcessingRead(),
            lExportSpecificState);
        pAction.PActionAllAdd += () => LAudio.LAudioAllDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListPathsRead(),
            PAudioProcessingRead(),
            lExportSpecificState);
        pAction.PActionAllSet(true, "Add every loaded file to the worklist");
        pList.PListPathChange += PAudioPathShow;
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, new PExport(lExportSpecificState) }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
        PAudioActiveRefresh();
    }

    private void PAudioActiveRefresh()
    {
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (PAudioKindRead(pStepName) is LWorkAudioKind pStepKind)
            {
                pProcessing.PProcessingActiveSet(pStepName, pInspector.PInspectorStepRead(pStepKind).LWorkAudioStepActive);
            }
        }
    }

    private LWorkAudio PAudioProcessingRead()
    {
        var pSteps = new List<LWorkAudioStep>();
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (PAudioKindRead(pStepName) is LWorkAudioKind pStepKind)
            {
                pSteps.Add(pInspector.PInspectorStepRead(pStepKind));
            }
        }

        return new LWorkAudio(pSteps);
    }

    private static LWorkAudioKind? PAudioKindRead(string pStepName) => pStepName switch
    {
        "Volume" => LWorkAudioKind.LWorkAudioKindVolume,
        "Normalize" => LWorkAudioKind.LWorkAudioKindNormalize,
        "Noise Reduction" => LWorkAudioKind.LWorkAudioKindNoiseReduction,
        "High Pass" => LWorkAudioKind.LWorkAudioKindHighPass,
        "Low Pass" => LWorkAudioKind.LWorkAudioKindLowPass,
        _ => null
    };

    private void PAudioPathShow(string? pSourcePath)
    {
        if (!string.IsNullOrWhiteSpace(pSourcePath))
        {
            pViewer.PViewerSourceOpen(pSourcePath);
        }
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
