using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainArea;

public sealed class PSplitTab : PTabSurface
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
    private readonly HashSet<LDetectorKind> pSplitEnabled = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

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

        pProcessing.PProcessingCheckableSet(true);
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
        pProcessing.PProcessingActiveChange += PSplitMethodHandle;

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
        }
    }

    private void PSplitMethodHandle(string pStepName, bool pActive)
    {
        foreach (LDetectorKind pDetectorKind in LDetector.LDetectorKinds)
        {
            if (PInspector.PSensorNameRead(pDetectorKind) != pStepName)
            {
                continue;
            }

            if (pActive)
            {
                pSplitEnabled.Add(pDetectorKind);
            }
            else
            {
                pSplitEnabled.Remove(pDetectorKind);
            }

            break;
        }
    }

    private List<LSceneDetector> PSplitDetectorRead()
    {
        var pDetectors = new List<LSceneDetector>();
        foreach (LDetectorKind pDetectorKind in LDetector.LDetectorKinds)
        {
            LDetectorStep pStep = pInspector.PSensorStepRead(pDetectorKind, pSplitEnabled.Contains(pDetectorKind));
            pDetectors.Add(new LSceneDetector
            {
                LSceneDetectorKind = (int)pDetectorKind,
                LSceneDetectorEnabled = pStep.LDetectorStepEnabled,
                LSceneDetectorThreshold = pStep.LDetectorStepThreshold,
                LSceneDetectorMinimum = pStep.LDetectorStepMinimum
            });
        }

        return pDetectors;
    }

    private void PSplitDetectorRestore(LSceneTabRecord? lPreferenceTabLayout)
    {
        if (lPreferenceTabLayout is null)
        {
            return;
        }

        foreach (LSceneDetector pDetector in lPreferenceTabLayout.LSceneDetectors)
        {
            if (!Enum.IsDefined(typeof(LDetectorKind), pDetector.LSceneDetectorKind))
            {
                continue;
            }

            var pDetectorKind = (LDetectorKind)pDetector.LSceneDetectorKind;
            pInspector.PSensorApply(new LDetectorStep(
                pDetectorKind,
                pDetector.LSceneDetectorEnabled,
                pDetector.LSceneDetectorThreshold,
                pDetector.LSceneDetectorMinimum));
            if (pDetector.LSceneDetectorEnabled)
            {
                pSplitEnabled.Add(pDetectorKind);
                pProcessing.PProcessingActiveSet(PInspector.PSensorNameRead(pDetectorKind), true);
            }
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
        return lPreferenceTabLayout;
    }
}
