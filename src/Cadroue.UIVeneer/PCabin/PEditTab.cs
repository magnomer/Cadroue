using Cadroue.Core;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;

using Cadroue.Infrastructure;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PEditTab : PTabSurface
{
    private const string PEditCropIcon = "/PAsset/PPanel/PProcessingCrop.svg";
    private const string PEditBrightnessIcon = "/PAsset/PPanel/PProcessingBrightness.svg";
    private const string PEditContrastIcon = "/PAsset/PPanel/PProcessingContrast.svg";
    private const string PEditSaturationIcon = "/PAsset/PPanel/PProcessingSaturation.svg";
    private const string PEditGammaIcon = "/PAsset/PPanel/PProcessingGamma.svg";
    private const string PEditExposureIcon = "/PAsset/PPanel/PProcessingExposure.svg";
    private const string PEditCurveIcon = "/PAsset/PPanel/PProcessingCurve.svg";
    private const string PEditWhitebalanceIcon = "/PAsset/PPanel/PProcessingWhitebalance.svg";

    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new(pEditEligible: true, pColorPreview: true);
    private readonly PInspector pInspector = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly LCropboxState pCropOwner;
    private readonly System.Windows.Controls.Grid pTabGrid;
    private readonly System.Windows.Threading.DispatcherTimer pEditColorTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(80)
    };
    private readonly System.Windows.Threading.DispatcherTimer pEditHistogramTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(220)
    };

    public PEditTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        pCropOwner = pInspector.LCropboxState;
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            if (!PExport.PExportSupportCheck(lPresetOwner, LWorkKind.LWorkKindEdit))
            {
                PExport.PExportIncompatibleShow();
                return;
            }

            if (pList.PListEditableRead() is not { } pEditSelected)
            {
                return;
            }

            (LWorkCrop pEditCrop, LWorkVideo pEditVideo) =
                LEdit.LEditWorkResolve(PEditPlanRead(), PEditEqCheck());
            LMessenger.LMessengerEditDescribe(
                lPriority,
                pEditSelected.LDocketEntryPath,
                pViewer.PViewerDurationRead(),
                pEditCrop,
                pEditVideo,
                lPresetOwner.LPresetSelectionEncoding,
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

            if (!PExport.PExportSupportCheck(lPresetOwner, LWorkKind.LWorkKindEdit))
            {
                PExport.PExportIncompatibleShow();
                return;
            }

            LMessenger.LMessengerEditDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner.LPresetSelectionEncoding,
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

            if (!PExport.PExportSupportCheck(lPresetOwner, LWorkKind.LWorkKindEdit))
            {
                PExport.PExportIncompatibleShow();
                return;
            }

            LMessenger.LMessengerEditDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Where(pItem => pEditPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionListAttach(pList);
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
        pProcessing.PProcessingStepAdd("Saturation", PEditSaturationIcon, "Processing.Step.Saturation");
        pProcessing.PProcessingStepAdd("Curve", PEditCurveIcon, "Processing.Step.Curve");
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
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
        pCropOwner.LCropboxStateChange += PEditCropHandle;
        pInspector.PInspectorVideoChange += PEditChangeHandle;
        pInspector.PWhitebalanceToolChange += pViewer.PViewerNeutralSet;
        pViewer.PViewerToolChange += pInspector.PWhitebalanceToolSet;
        pViewer.PViewerNeutralChange += PEditNeutralHandle;
        pInspector.LWhitebalance.LWhitebalanceEstimateChange += PEditEstimateHandle;
        pViewer.LViewer.LViewerMediaChange += _ =>
            PEditEstimateHandle(pInspector.PWhitebalanceMethodRead());
        pViewer.LViewer.LViewerMediaChange += _ => PEditHistogramDefer();
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
        pViewer.LViewer.LViewerMediaChange += _ => PEditCropRestore();
        pList.PListPathChange += PEditPathShow;
        pList.PListItemsAdd += PEditItemsHandle;
        pViewer.PDropPathsChange += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);

        var pExport = new PExport(lPresetOwner, LWorkKind.LWorkKindEdit);
        PTabLockAttach(pList, pProcessing, pInspector, pExport);
        pList.PListLockChange += pLocked =>
        {
            if (pLocked)
            {
                pViewer.PViewerNeutralCancel();
            }

            pViewer.PCropLockSet(pLocked);
            pViewer.PCropToolSet(!pLocked && pInspector.PInspectorToolCheck());
        };
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer),
            pAction,
            pFlow,
            lPreferenceTabLayout);
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

    public override PFlow PTabFlow => pFlow;
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
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutCreate();
        bool pCropPersistent = pCropOwner.LCropboxStatePersistent;
        bool pVideoPersistent = pInspector.PTonePersistentCheck();
        bool pSkipPersistent = pInspector.PSkipPersistentCheck();
        if (pCropPersistent || pVideoPersistent || pSkipPersistent)
        {
            (bool pRatioFixed, bool pRatioLenient, int pRatioWidth, int pRatioHeight) = pCropOwner.LCropboxStateRatio;
            var pEditCarried = new LEditPlan(
                pCropPersistent ? pInspector.PInspectorCropRead() : LWorkCrop.LWorkCropCreate(),
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
