using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PSplitTab : PTabSurface
{
    private const string PSplitBlankIcon = "/PAssets/PPanels/PProcessingBlank.svg";
    private const string PSplitSceneIcon = "/PAssets/PPanels/PProcessingScene.svg";
    private const string PSplitStillIcon = "/PAssets/PPanels/PProcessingStill.svg";
    private const string PSplitLuminanceIcon = "/PAssets/PPanels/PProcessingLuminance.svg";
    private const string PSplitSilenceIcon = "/PAssets/PPanels/PProcessingSilence.svg";
    private const string PSplitVolumeIcon = "/PAssets/PPanels/PProcessingReducedVolume.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PSection pSection = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly PInspector pInspector = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private System.Threading.CancellationTokenSource? pSplitSweepSource;
    private bool pSplitDetectorLoading;

    public PSplitTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            if (pList.PListEditableRead() is not { } pSplitSelected)
            {
                return;
            }

            LMessenger.LMessengerSplitDescribe(
                lPriority,
                pSplitSelected.LDocketEntryPath,
                pFlow.PFlowSplitRead(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                pSplitSelected.LDocketEntryBatch);
        };
        pAction.PActionAllAdd += () =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            _ = LMessenger.LMessengerSplitDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionItemsAdd += pSplitPaths =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            _ = LMessenger.LMessengerSplitDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Where(pItem => pSplitPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionSelectionSource = () => pList.PListSelectionRead();
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.AddAll.SplitTooltip"));

        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindBlank), PSplitBlankIcon, "Processing.Step.Blank");
        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindScene), PSplitSceneIcon, "Processing.Step.Scene");
        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindStill), PSplitStillIcon, "Processing.Step.Still");
        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindLuminance), PSplitLuminanceIcon, "Processing.Step.Luminance");
        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindSilence), PSplitSilenceIcon, "Processing.Step.Silence");
        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindVolume), PSplitVolumeIcon, "Processing.Step.Volume");
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pInspector.PSensorChange += PSplitActiveUpdate;
        pInspector.PSensorChange += PSplitDetectorSave;

        pInspector.PSensorRunShow();
        pInspector.PSensorRun += PSplitSweepRun;
        pInspector.PSensorStop += () => pSplitSweepSource?.Cancel();
        pInspector.PSensorPersistentChange += PSplitPersistentHandle;
        pInspector.PBlankPickChange += pArmed =>
            pViewer.PViewerNeutralSet(pArmed, LNeutralTarget.LNeutralTargetGrey);
        pViewer.PViewerNeutralChange += pSample =>
            pInspector.PBlankSampleApply(pSample.LNeutralRed, pSample.LNeutralGreen, pSample.LNeutralBlue);

        pFlow.PFlowSectionShow(true);
        pSection.PSectionAttach(pFlow);
        pList.PListPathChange += PSplitPathShow;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner, pExportSmartAllowed: true);
        PTabLockAttach(pList, pSection, pProcessing, pInspector, pExport);
        pList.PListLockChange += pLocked => pFlow.PFlowEditSet(!pLocked);
        pFlow.PFlowEditSet(!pList.PListLockCheck());
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pSection, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, true), pAction, pFlow, lPreferenceTabLayout);
        if (lPreferenceTabLayout is null)
        {
            pProcessing.PProcessingMinimizeSet(true);
            pInspector.PInspectorMinimizeSet(true);
        }

        Content = pTabGrid;
        PSplitDetectorRestore(lPreferenceTabLayout);
    }

    private void PSplitPathShow(string? pSourcePath)
    {
        if (!string.IsNullOrWhiteSpace(pSourcePath))
        {
            pViewer.PViewerSourceOpen(pSourcePath);
            PSplitDetectorLoad(pSourcePath);
        }
    }

    private void PSplitActiveUpdate()
    {
        foreach (LDetectorKind pDetectorKind in LDetector.LDetectorKinds)
        {
            pProcessing.PProcessingActiveSet(
                PInspector.PSensorNameRead(pDetectorKind),
                pInspector.PSensorStepRead(pDetectorKind).LDetectorStepEnabled);
        }
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override bool PTabSectionVisible => true;
    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        lPreferenceTabLayout.LSceneDetectors = PSplitDetectorRead();
        lPreferenceTabLayout.LSceneDetectPersistent = pInspector.PSensorPersistentCheck();
        return lPreferenceTabLayout;
    }
}
