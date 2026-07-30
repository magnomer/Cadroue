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
    private bool pAudioPlanLoading;

    public PAudioTab(LExportSpecificState lExportSpecificState, LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        pProcessing.PProcessingOrderedSet(true);
        pProcessing.PProcessingStepAdd("High Pass", PAudioHighPassIconPath, "Processing.Step.HighPass");
        pProcessing.PProcessingStepAdd("Low Pass", PAudioLowPassIconPath, "Processing.Step.LowPass");
        pProcessing.PProcessingStepAdd("Noise Reduction", PAudioNoiseIconPath, "Processing.Step.NoiseReduction");
        pProcessing.PProcessingStepAdd("Volume", PAudioVolumeIconPath, "Processing.Step.Volume");
        pProcessing.PProcessingStepAdd("Normalize", PAudioNormalizeIconPath, "Processing.Step.Normalize");
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pProcessing.PProcessingOrderChange += PAudioPlanSave;
        pInspector.PInspectorAudioActiveChange += PAudioChangeHandle;

        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority =>
        {
            PAudioPlanSave();
            LAudio.LAudioDescribe(
                lPriority,
                pViewer.PViewerSourcePath,
                PAudioProcessingRead(),
                lExportSpecificState,
                pAction.PActionRelayTarget);
        };
        pAction.PActionAllAdd += () =>
        {
            PAudioPlanSave();
            LAudio.LAudioAllDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListPathsRead(),
                PAudioProcessingRead(),
                lExportSpecificState,
                pInspector.PInspectorAudioPersistentAnyCheck()
                    ? pInspector.PInspectorAudioPersistentRead()
                    : null,
                pAction.PActionRelayTarget);
        };
        pAction.PActionAllSet(
            true,
            LLocalization.LLocalizationTextRead("Action.AudioAll.Tooltip"));
        pList.PListPathChange += PAudioPathShow;
        PTabViewerAttach(pList, pViewer);
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

    private void PAudioChangeHandle()
    {
        PAudioActiveRefresh();
        PAudioPlanSave();
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
            PAudioPlanSave();
            pViewer.PViewerSourceOpen(pSourcePath);
            PAudioPlanRestore();
        }
    }

    private void PAudioPlanRestore()
    {
        pAudioPlanLoading = true;
        try
        {
            LWorkAudio? pSaved = pViewer.PViewerSourcePath is { } pSourcePath
                ? LAudio.LAudioPlanRead(pSourcePath)
                : null;
            LWorkAudio? pPersistent = pInspector.PInspectorAudioPersistentAnyCheck()
                ? pInspector.PInspectorAudioPersistentRead()
                : null;
            pInspector.PInspectorAudioPlanApply(LAudio.LAudioPlanResolve(pSaved, pPersistent));
        }
        finally
        {
            pAudioPlanLoading = false;
        }

        PAudioActiveRefresh();
    }

    private void PAudioPlanSave()
    {
        if (pAudioPlanLoading || pViewer.PViewerSourcePath is not { } pSourcePath)
        {
            return;
        }

        LWorkAudio pAudioPlan = PAudioProcessingRead();
        if (!pAudioPlan.LWorkAudioActive && LAudio.LAudioPlanRead(pSourcePath) is null)
        {
            return;
        }

        LAudio.LAudioPlanSave(pSourcePath, pAudioPlan);
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
