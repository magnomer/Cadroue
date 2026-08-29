using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PEditTab : PTabSurface
{
    private const string PEditCropIcon = "/PAssets/PPanels/PProcessingCrop.svg";
    private const string PEditBrightnessIcon = "/PAssets/PPanels/PProcessingBrightness.svg";
    private const string PEditContrastIcon = "/PAssets/PPanels/PProcessingContrast.svg";
    private const string PEditSaturationIcon = "/PAssets/PPanels/PProcessingSaturation.svg";
    private const string PEditGammaIcon = "/PAssets/PPanels/PProcessingGamma.svg";
    private const string PEditExposureIcon = "/PAssets/PPanels/PProcessingExposure.svg";
    private const string PEditCurveIcon = "/PAssets/PPanels/PProcessingCurve.svg";
    private const string PEditWhitebalanceIcon = "/PAssets/PPanels/PProcessingWhitebalance.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new() { PViewerColorPreview = true, PViewerEditEligible = true };
    private readonly PInspector pInspector = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly LCropboxState pCropOwner = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private readonly System.Windows.Threading.DispatcherTimer pEditColorTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(80)
    };
    private readonly System.Windows.Threading.DispatcherTimer pEditHistogramTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(220)
    };
    private bool pEditPlanLoading;

    public PEditTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
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

            if (pList.PListEditableRead() is not { } pEditSelected)
            {
                return;
            }

            LMessenger.LMessengerEditDescribe(
                lPriority,
                pEditSelected.LDocketEntryPath,
                pViewer.PViewerDurationRead(),
                pInspector.PSkipActiveCheck() ? LWorkCrop.LWorkCropCreate() : pCropOwner.LCropboxStateCrop,
                pInspector.PSkipActiveCheck() ? LWorkVideo.LWorkVideoCreate() : PEditVideoRead(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                pEditSelected.LDocketEntryBatch);
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
                pAction.PActionSourceTab);
        };
        pAction.PActionItemsAdd += pEditPaths =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            _ = LMessenger.LMessengerEditDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Where(pItem => pEditPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionSelectionSource = () => pList.PListSelectionRead();
        pAction.PActionAllSet(
            true,
            LLocalization.LLocalizationTextRead("Action.EditAll.Tooltip"));

        pProcessing.PProcessingOrderedSet(false);
        pProcessing.PProcessingStepAdd("Crop", PEditCropIcon, "Processing.Step.Crop");
        pProcessing.PProcessingStepAdd("Whitebalance", PEditWhitebalanceIcon, "Processing.Step.Whitebalance");
        pProcessing.PProcessingStepAdd("Exposure", PEditExposureIcon, "Processing.Step.Exposure");
        pProcessing.PProcessingStepAdd("Brightness", PEditBrightnessIcon, "Processing.Step.Brightness");
        pProcessing.PProcessingStepAdd("Contrast", PEditContrastIcon, "Processing.Step.Contrast");
        pProcessing.PProcessingStepAdd("Gamma", PEditGammaIcon, "Processing.Step.Gamma");
        pProcessing.PProcessingStepAdd("Curve", PEditCurveIcon, "Processing.Step.Curve");
        pProcessing.PProcessingStepAdd("Saturation", PEditSaturationIcon, "Processing.Step.Saturation");
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepChange += PEditStepHandle;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pProcessing.PProcessingStepOpen += pStep =>
        {
            if (pStep == "Curve")
            {
                PEditHistogramDefer();
            }
        };
        pInspector.PSkipActiveChange += PEditSkipHandle;
        pInspector.PInspectorPlanChange += PEditPersistentSave;

        pInspector.PInspectorToolChange += pViewer.PCropToolSet;
        pInspector.PInspectorRatioChange += pViewer.PCropRatioSet;
        pInspector.PInspectorRatioChange += _ => PEditRatioSet();
        pInspector.PInspectorRatioChange += _ => PEditPlanSave();
        pInspector.PInspectorCropChange += pViewer.PCropVideoSet;
        pInspector.PInspectorCropChange += _ => PEditCropSet();
        pInspector.PInspectorRotateChange += PEditRotateHandle;
        pInspector.PInspectorCropChange += _ => PEditPlanSave();
        pInspector.PInspectorRotateChange += _ => PEditCropSet();
        pInspector.PInspectorRotateChange += _ => PEditPlanSave();
        pInspector.PInspectorPersistentChange += pPersistent => pViewer.PCropPersistent = pPersistent;
        pInspector.PInspectorPersistentChange += pCropOwner.LCropboxPersistentSet;
        pInspector.PCropActiveChange += PEditActiveSet;
        pInspector.PCropActiveChange += PEditCropUpdate;
        pInspector.PCropActiveChange += PEditPlanSave;
        pInspector.PInspectorVideoChange += PEditChangeHandle;
        pInspector.PWhitebalanceToolChange += pViewer.PViewerNeutralSet;
        pViewer.PViewerToolChange += pInspector.PWhitebalanceToolSet;
        pViewer.PViewerNeutralChange += PEditNeutralHandle;
        pInspector.PWhitebalanceEstimateChange += PEditEstimateHandle;
        pViewer.PViewerMediaChange += _ =>
            PEditEstimateHandle(pInspector.PWhitebalanceMethodRead());
        pViewer.PViewerMediaChange += _ => PEditHistogramDefer();
        pViewer.PViewerClockTick += _ => PEditHistogramDefer();
        pEditColorTimer.Tick += (_, _) =>
        {
            pEditColorTimer.Stop();
            PEditColorApply();
        };
        pEditHistogramTimer.Tick += (_, _) =>
        {
            pEditHistogramTimer.Stop();
            PEditHistogramHandle();
        };
        PEditCapabilityHandle();
        pViewer.PViewerEngineChange += PEditCapabilityHandle;
        pViewer.PViewerEngineChange += pViewer.PViewerNeutralCancel;
        pViewer.PCropVideoChange += PEditCropShow;
        pViewer.PViewerMediaChange += _ => PEditCropRestore();
        pList.PListPathChange += PEditPathShow;
        pList.PListItemsAdd += PEditItemsHandle;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);

        var pExport = new PExport(lPresetOwner, true);
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
        PEditPersistentRestore(lPreferenceTabLayout);
        PEditCropUpdate();
        PEditColorUpdate();
        pViewer.PCropActiveSet(pInspector.PCropActiveCheck());
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;

    public override void PTabClose()
    {
        pEditColorTimer.Stop();
        pEditHistogramTimer.Stop();
        pViewer.PViewerEngineChange -= PEditCapabilityHandle;
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
            var pEditCarried = new LEditPlan(
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
                LSceneInspectorEdit = LEdit.LEditPersistentCreate(pEditCarried),
                LSceneInspectorCrop = pCropPersistent,
                LSceneInspectorSkip = pSkipPersistent
            };
        }

        return lPreferenceTabLayout;
    }
}
