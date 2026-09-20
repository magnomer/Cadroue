using Cadroue.Core;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PEditTab : PTabSurface
{
    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new(pEditEligible: true, pColorPreview: true);
    private readonly PInspector pInspector = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
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
        LEditTab = new LEditTab(
            lPresetOwner,
            pInspector.LInspector,
            pViewer.LViewer,
            pViewer.LCrop,
            pList.LList,
            pList.PListDocketRead(),
            pProcessing.LProcessing);
        var pAction = new PAction();
        PTabAction = pAction;
        LAction lAction = pAction.LAction;
        lAction.LActionRun += lPriority =>
            LEditTab.LEditRun(lPriority, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionAllAdd += () => LEditTab.LEditAllRun(lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionItemsAdd += pEditPaths =>
            LEditTab.LEditItemsRun(pEditPaths, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        pAction.PActionListAttach(pList);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.EditAll.Tooltip"));

        LEditTab.LEditRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pInspector.LWhitebalance.LWhitebalanceToolChange += pViewer.PViewerNeutralSet;
        pViewer.PViewerToolChange += pInspector.LWhitebalance.LWhitebalanceToolSet;
        pViewer.PViewerNeutralChange += LEditTab.LEditColor.LEditNeutralHandle;
        pViewer.PViewerClockTick += _ => PEditHistogramDefer();
        pViewer.PViewerEngineChange += LEditTab.LEditColor.LEditCapableHandle;
        pViewer.PViewerEngineChange += pViewer.PViewerNeutralCancel;
        pViewer.PCropVideoChange += LEditTab.LEditCropShow;
        pViewer.PDropPathsChange += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
        pList.PListPathChange += LEditTab.LEditPathHandle;
        pList.PListItemsAdd += LEditTab.LEditStore.LEditItemsHandle;
        pList.PListLockChange += LEditTab.LEditLockHandle;

        LEditTab.LEditPresetMissing += PExport.PExportMissingShow;
        LEditTab.LEditPresetIncompatible += PExport.PExportIncompatibleShow;
        LEditTab.LEditRotateApply += pViewer.PViewerRotateSet;
        LEditTab.LEditRectApply += pViewer.PCropboxSet;
        LEditTab.LEditActiveApply += pViewer.PCropActiveSet;
        LEditTab.LEditLockApply += pViewer.PCropLockSet;
        LEditTab.LEditToolApply += pViewer.PCropToolSet;
        LEditTab.LEditNeutralCancel += pViewer.PViewerNeutralCancel;
        LEditTab.LEditHistogramDefer += PEditHistogramDefer;
        LEditTab.LEditColorDefer += PEditColorDefer;
        LEditTab.LEditColor.LEditPreviewApply += pViewer.PViewerColorSet;
        LEditTab.LEditColor.LEditEstimateRead += PEditEstimateRead;
        LEditTab.LEditColor.LEditHistogramRead += PEditHistogramRead;
        pEditColorTimer.Tick += PEditColorTick;
        pEditHistogramTimer.Tick += PEditHistogramTick;
        LEditTab.LEditColor.LEditCapableHandle();

        var pExport = new PExport(lPresetOwner, LWorkKind.LWorkKindEdit);
        PTabLockAttach(pList, pProcessing, pInspector, pExport);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
        LEditTab.LEditLayoutApply(lPreferenceTabLayout);
        LEditTab.LEditStart();
    }

    public LEditTab LEditTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        pEditColorTimer.Stop();
        pEditHistogramTimer.Stop();
        pViewer.PViewerEngineChange -= LEditTab.LEditColor.LEditCapableHandle;
        pViewer.PViewerEngineChange -= pViewer.PViewerNeutralCancel;
        LEditTab.LEditClose();
        base.PTabClose();
    }

    public override LSceneTabRecord PTabLayoutRead() => LEditTab.LEditLayoutRead(PTabLayoutCreate());

    private void PEditEstimateRead(LWhitebalanceMethod pMethod) =>
        pViewer.PViewerEstimateRead(pMethod, LEditTab.LEditColor.LEditEstimateApply);

    private void PEditHistogramRead() => pViewer.PViewerFrameRead(LEditTab.LEditColor.LEditHistogramApply);

    private void PEditHistogramDefer()
    {
        pEditHistogramTimer.Stop();
        pEditHistogramTimer.Start();
    }

    private void PEditColorDefer()
    {
        pEditColorTimer.Stop();
        pEditColorTimer.Start();
    }

    private void PEditColorTick(object? pSender, EventArgs pEvent)
    {
        pEditColorTimer.Stop();
        LEditTab.LEditColor.LEditColorApply();
    }

    private void PEditHistogramTick(object? pSender, EventArgs pEvent)
    {
        pEditHistogramTimer.Stop();
        LEditTab.LEditColor.LEditHistogramRun();
    }
}
