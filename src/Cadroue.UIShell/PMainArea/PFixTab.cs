using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PFixTab : PTabSurface
{
    private const string PFixCropIcon = "/PAssets/PPanels/PProcessingCrop.svg";
    private const string PFixBrightnessIcon = "/PAssets/PPanels/PProcessingBrightness.svg";
    private const string PFixContrastIcon = "/PAssets/PPanels/PProcessingContrast.svg";
    private const string PFixSaturationIcon = "/PAssets/PPanels/PProcessingSaturation.svg";
    private const string PFixGammaIcon = "/PAssets/PPanels/PProcessingGamma.svg";
    private const string PFixExposureIcon = "/PAssets/PPanels/PProcessingExposure.svg";
    private const string PFixCurveIcon = "/PAssets/PPanels/PProcessingCurve.svg";
    private const string PFixWhitebalanceIcon = "/PAssets/PPanels/PProcessingWhitebalance.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new() { PViewerColorPreview = true, PViewerEditEligible = true };
    private readonly PInspector pInspector = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly LCropboxState pCropOwner = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private readonly System.Windows.Threading.DispatcherTimer pFixColorTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(80)
    };
    private readonly System.Windows.Threading.DispatcherTimer pFixHistogramTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(220)
    };
    private bool pFixPlanLoading;

    public PFixTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
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

            if (pList.PListEditableRead() is not { } pFixSelected)
            {
                return;
            }

            LMessenger.LMessengerEditDescribe(
                lPriority,
                pFixSelected.LDocketEntryPath,
                pViewer.PViewerDurationRead(),
                pInspector.PSkipActiveCheck() ? LWorkCrop.LWorkCropCreate() : pCropOwner.LCropboxStateCrop,
                pInspector.PSkipActiveCheck() ? LWorkVideo.LWorkVideoCreate() : PFixVideoRead(PFixMpvCheck()),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                pFixSelected.LDocketEntryBatch);
        };

        pAction.PActionAllAdd += () =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            _ = LMessenger.LMessengerEditDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                PFixMpvCheck());
        };
        pAction.PActionItemsAdd += pFixPaths =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            _ = LMessenger.LMessengerEditDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Where(pItem => pFixPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                PFixMpvCheck());
        };
        pAction.PActionSelectionSource = () => pList.PListSelectionRead();
        pAction.PActionAllSet(
            true,
            LLocalization.LLocalizationTextRead("Action.EditAll.Tooltip"));

        pProcessing.PProcessingOrderedSet(false);
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepChange += PFixStepHandle;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pProcessing.PProcessingStepOpen += pStep =>
        {
            if (pStep == "Curve")
            {
                PFixHistogramDefer();
            }
        };
        pInspector.PSkipActiveChange += PFixSkipHandle;
        pInspector.PInspectorPlanChange += PFixPersistentSave;

        pInspector.PInspectorToolChange += pViewer.PCropToolSet;
        pInspector.PInspectorRatioChange += pViewer.PCropRatioSet;
        pInspector.PInspectorRatioChange += _ => PFixRatioSet();
        pInspector.PInspectorRatioChange += _ => PFixPlanSave();
        pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
        pInspector.PInspectorCropChange += _ => PFixCropSet();
        pInspector.PInspectorRotateChange += PFixRotateHandle;
        pInspector.PInspectorCropChange += _ => PFixPlanSave();
        pInspector.PInspectorRotateChange += _ => PFixCropSet();
        pInspector.PInspectorRotateChange += _ => PFixPlanSave();
        pInspector.PInspectorPersistentChange += pPersistent => pViewer.PCropPersistent = pPersistent;
        pInspector.PInspectorPersistentChange += pCropOwner.LCropboxPersistentSet;
        pInspector.PCropActiveChange += PFixActiveSet;
        pInspector.PCropActiveChange += PFixCropUpdate;
        pInspector.PCropActiveChange += PFixPlanSave;
        pInspector.PInspectorVideoChange += PFixChangeHandle;
        pInspector.PWhitebalanceToolChange += pViewer.PViewerNeutralSet;
        pViewer.PViewerToolChange += pInspector.PWhitebalanceToolSet;
        pViewer.PViewerNeutralChange += PFixNeutralHandle;
        pInspector.PWhitebalanceEstimateChange += PFixEstimateHandle;
        pViewer.PViewerMediaChange += _ =>
            PFixEstimateHandle(pInspector.PWhitebalanceMethodRead());
        pViewer.PViewerMediaChange += _ => PFixHistogramDefer();
        pViewer.PViewerClockTick += _ => PFixHistogramDefer();
        pFixColorTimer.Tick += (_, _) =>
        {
            pFixColorTimer.Stop();
            PFixColorApply();
        };
        pFixHistogramTimer.Tick += (_, _) =>
        {
            pFixHistogramTimer.Stop();
            PFixHistogramHandle();
        };
        PFixCapabilityHandle();
        pViewer.PViewerEngineChange += PFixCapabilityHandle;
        pViewer.PViewerEngineChange += pViewer.PViewerNeutralCancel;
        pViewer.PCropVideoChange += PFixCropShow;
        pViewer.PViewerMediaChange += _ => PFixCropRestore();
        pList.PListPathChange += PFixPathShow;
        pList.PListItemsAdd += PFixItemsHandle;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);

        var pExport = new PExport(lPresetOwner, pExportSmartAllowed: true);
        PTabLockAttach(pList, pProcessing, pInspector, pExport);
        pList.PListLockChange += pLocked =>
        {
            if (pLocked)
            {
                pViewer.PViewerNeutralCancel();
            }

            pViewer.PCropToolSet(!pLocked && pInspector.PInspectorToolCheck());
        };
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        if (lPreferenceTabLayout is null)
        {
            pInspector.PInspectorMinimizeSet(true);
        }

        Content = pTabGrid;
        PFixPersistentRestore(lPreferenceTabLayout);
        PFixCropUpdate();
        PFixColorUpdate();
        pViewer.PCropActiveSet(pInspector.PCropActiveCheck());
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;

    public override void PTabClose()
    {
        pFixColorTimer.Stop();
        pFixHistogramTimer.Stop();
        pViewer.PViewerEngineChange -= PFixCapabilityHandle;
        pViewer.PViewerEngineChange -= pViewer.PViewerNeutralCancel;
        base.PTabClose();
    }
    public override PList? PTabList => pList;
    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        bool pCropPersistent = pCropOwner.LCropboxStatePersistent;
        bool pVideoPersistent = pInspector.PTonePersistentCheck();
        bool pSkipPersistent = pInspector.PSkipPersistentCheck();
        if (pCropPersistent || pVideoPersistent || pSkipPersistent)
        {
            (bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) = pCropOwner.LCropboxStateRatio;
            var pFixCarried = new LEditPlan(
                pCropPersistent ? pCropOwner.LCropboxStateCrop : LWorkCrop.LWorkCropCreate(),
                pInspector.PTonePersistentRead(),
                pCropPersistent && pCropOwner.LCropboxStateActive)
            {
                LEditSkip = pSkipPersistent && pInspector.PSkipActiveCheck(),
                LEditRatioFixed = pCropPersistent && pRatioFixed,
                LEditRatioLenient = pCropPersistent && pRatioLenient,
                LEditRatioWidth = pCropPersistent ? pRatioWidth : 0,
                LEditRatioHeight = pCropPersistent ? pRatioHeight : 0
            };
            lPreferenceTabLayout.LSceneInspector = new LSceneInspectorRecord
            {
                LSceneInspectorEdit = LEdit.LEditPersistentCreate(pFixCarried),
                LSceneInspectorCrop = pCropPersistent,
                LSceneInspectorSkip = pSkipPersistent
            };
        }

        return lPreferenceTabLayout;
    }
}
