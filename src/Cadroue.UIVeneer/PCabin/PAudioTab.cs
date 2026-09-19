using Cadroue.Core;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PAudioTab : PTabSurface
{
    private const string PAudioVolumeIcon = "/PAsset/PPanel/PProcessingVolume.svg";
    private const string PAudioNormalizeIcon = "/PAsset/PPanel/PProcessingNormalize.svg";
    private const string PAudioNoiseIcon = "/PAsset/PPanel/PProcessingNoiseReduction.svg";
    private const string PAudioHighIcon = "/PAsset/PPanel/PProcessingHighPass.svg";
    private const string PAudioLowIcon = "/PAsset/PPanel/PProcessingLowPass.svg";
    private const string PAudioEqualizerIcon = "/PAsset/PPanel/PProcessingEqualizer.svg";

    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new(pAudioEligible: true);
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly PInspector pInspector = new();
    private readonly LSMonitor pAudioMonitor = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private System.Windows.Threading.DispatcherTimer? pAudioViewerTimer;

    public PAudioTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        pProcessing.PProcessingOrderedSet(true);
        pProcessing.PProcessingStepAdd("High Pass", PAudioHighIcon, "Processing.Step.HighPass");
        pProcessing.PProcessingStepAdd("Low Pass", PAudioLowIcon, "Processing.Step.LowPass");
        pProcessing.PProcessingStepAdd("Noise Reduction", PAudioNoiseIcon, "Processing.Step.NoiseReduction");
        pProcessing.PProcessingStepAdd("Equalizer", PAudioEqualizerIcon, "Processing.Step.Equalizer");
        pProcessing.PProcessingStepAdd("Volume", PAudioVolumeIcon, "Processing.Step.Volume");
        pProcessing.PProcessingStepAdd("Normalize", PAudioNormalizeIcon, "Processing.Step.Normalize");
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pProcessing.PProcessingOrderChange += PAudioPlanSave;
        pProcessing.PProcessingMonitorShow += PAudioMonitorShow;
        pProcessing.PProcessingMonitorSet();
        pInspector.PSkipActiveChange += PAudioSkipHandle;
        pInspector.PInspectorPlanChange += PAudioPersistentSave;
        pInspector.PInspectorAudioChange += PAudioChangeHandle;

        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            if (!PExport.PExportSupportCheck(lPresetOwner, LWorkKind.LWorkKindAudio))
            {
                PExport.PExportIncompatibleShow();
                return;
            }

            if (pList.PListEditableRead() is not { } pAudioSelected)
            {
                return;
            }

            PAudioPlanSave();
            LMessenger.LMessengerAudioDescribe(
                lPriority,
                pAudioSelected.LDocketEntryPath,
                PAudioProcessingRead(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                pAudioSelected.LDocketEntryBatch);
        };
        pAction.PActionAllAdd += () =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            if (!PExport.PExportSupportCheck(lPresetOwner, LWorkKind.LWorkKindAudio))
            {
                PExport.PExportIncompatibleShow();
                return;
            }

            PAudioPlanSave();
            LMessenger.LMessengerAudioDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionItemsAdd += pAudioPaths =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            if (!PExport.PExportSupportCheck(lPresetOwner, LWorkKind.LWorkKindAudio))
            {
                PExport.PExportIncompatibleShow();
                return;
            }

            PAudioPlanSave();
            LMessenger.LMessengerAudioDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Where(pItem => pAudioPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionListAttach(pList);
        pAction.PActionAllSet(
            true,
            LLocalization.LLocalizationTextRead("Action.AudioAll.Tooltip"));
        pList.PListPathChange += PAudioPathShow;
        pList.PListItemsAdd += PAudioItemsHandle;
        pViewer.LViewer.LViewerMediaChange += PAudioMediaHandle;
        pViewer.PDropPathsChange += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner, LWorkKind.LWorkKindAudio);
        PTabLockAttach(pList, pProcessing, pInspector, pExport);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
        PAudioPersistentRestore(lPreferenceTabLayout);
        PAudioActiveUpdate();
    }

    public override void PTabClose()
    {
        base.PTabClose();
        pAudioMonitor.Dispose();
    }

    public override PFlow PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutCreate();
        if (pInspector.PInspectorPersistentCheck())
        {
            lPreferenceTabLayout.LSceneInspector = new LSceneInspectorRecord
            {
                LSceneInspectorAudio = LAudio.LAudioPersistentCreate(pInspector.PInspectorPersistentRead()),
                LSceneInspectorSkip = pInspector.PSkipPersistentCheck()
            };
        }

        return lPreferenceTabLayout;
    }
}
