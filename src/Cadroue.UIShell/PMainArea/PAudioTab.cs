using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PAudioTab : PTabSurface
{
    private const string PAudioVolumeIcon = "/PAssets/PPanels/PProcessingVolume.svg";
    private const string PAudioNormalizeIcon = "/PAssets/PPanels/PProcessingNormalize.svg";
    private const string PAudioNoiseIcon = "/PAssets/PPanels/PProcessingNoiseReduction.svg";
    private const string PAudioHighIcon = "/PAssets/PPanels/PProcessingHighPass.svg";
    private const string PAudioLowIcon = "/PAssets/PPanels/PProcessingLowPass.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new();
    private readonly PProcessing pProcessing = new();
    private readonly PInspector pInspector = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private bool pAudioPlanLoading;

    public PAudioTab(LPreset lExportSpecificState, LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        pProcessing.PProcessingOrderedSet(true);
        pProcessing.PProcessingStepAdd("High Pass", PAudioHighIcon, "Processing.Step.HighPass");
        pProcessing.PProcessingStepAdd("Low Pass", PAudioLowIcon, "Processing.Step.LowPass");
        pProcessing.PProcessingStepAdd("Noise Reduction", PAudioNoiseIcon, "Processing.Step.NoiseReduction");
        pProcessing.PProcessingStepAdd("Volume", PAudioVolumeIcon, "Processing.Step.Volume");
        pProcessing.PProcessingStepAdd("Normalize", PAudioNormalizeIcon, "Processing.Step.Normalize");
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepChange += PAudioStepHandle;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pProcessing.PProcessingOrderChange += PAudioPlanSave;
        pInspector.PSkipActiveChange += PAudioSkipHandle;
        pInspector.PInspectorPlanChange += PAudioPersistentWrite;
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
                lExportSpecificState,
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
        PAudioActiveUpdate();
    }

    private void PAudioSkipHandle()
    {
        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PAudioPlanSave();
    }

    private void PAudioPersistentWrite()
    {
        if (pAudioPlanLoading || !pInspector.PInspectorPersistentCheck())
        {
            return;
        }

        LWorkAudio pAudioPersistent = pInspector.PInspectorPersistentRead();
        foreach (string pAudioPath in pList.PListPathsRead())
        {
            LAudio.LAudioPlanSave(pAudioPath, LAudio.LAudioPlanResolve(LAudio.LAudioPlanRead(pAudioPath), pAudioPersistent));
        }
    }

    private void PAudioStepHandle(string? pStepName)
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

    private void PAudioActiveUpdate()
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
        PAudioActiveUpdate();
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

        return new LWorkAudio(pSteps) { LWorkAudioSkip = pInspector.PSkipActiveCheck() };
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
            LWorkAudio? pPersistent = pInspector.PInspectorPersistentCheck()
                ? pInspector.PInspectorPersistentRead()
                : null;
            LWorkAudio pResolved = LAudio.LAudioPlanResolve(pSaved, pPersistent);
            pInspector.PInspectorPlanApply(pResolved);
            pInspector.PSkipApply(pResolved.LWorkAudioSkip);
        }
        finally
        {
            pAudioPlanLoading = false;
        }

        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PAudioActiveUpdate();
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
        PAudioPersistentWrite();
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
