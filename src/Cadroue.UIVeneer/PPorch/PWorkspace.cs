using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Cadroue.UIVeneer.PCabin;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;
using Cadroue.UIVeneer.PBench;

namespace Cadroue.UIVeneer.PPorch;

public sealed class PWorkspace
{
    private static readonly IReadOnlyDictionary<string, Func<LPresetSelection, LSceneTabRecord?, PTabSurface>>
        pWorkspaceSurfaces = new Dictionary<string, Func<LPresetSelection, LSceneTabRecord?, PTabSurface>>
        {
            ["Split"] = (lOwner, lLayout) => new PSplitTab(lOwner, lLayout),
            ["Edit"] = (lOwner, lLayout) => new PEditTab(lOwner, lLayout),
            ["Fix"] = (lOwner, lLayout) => new PFixTab(lOwner, lLayout),
            ["Audio"] = (lOwner, lLayout) => new PAudioTab(lOwner, lLayout),
            ["Convert"] = (lOwner, lLayout) => new PConvertTab(lOwner, lLayout),
            ["Merge"] = (lOwner, lLayout) => new PMergeTab(lOwner, lLayout),
            ["Funnel"] = (lOwner, lLayout) => new PFunnelTab(lLayout),
            ["Worklist"] = (lOwner, lLayout) => new PWorklistTab(lLayout),
        };

    private static readonly IReadOnlyDictionary<bool, Func<PWorkspace, FrameworkElement>> pWorkspaceRoots =
        new Dictionary<bool, Func<PWorkspace, FrameworkElement>>
        {
            [true] = PWorkspaceGridBuild,
            [false] = pWorkspace => pWorkspace.PWorkspaceSurface,
        };

    private static readonly IReadOnlyDictionary<bool, Action<PWorkspace>> pWorkspacePairs =
        new Dictionary<bool, Action<PWorkspace>>
        {
            [true] = PWorkspacePairAttach,
            [false] = pWorkspace => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PWorkspace>> pWorkspacePairCloses =
        new Dictionary<bool, Action<PWorkspace>>
        {
            [true] = PWorkspacePairClose,
            [false] = pWorkspace => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PWorkspace>> pWorkspaceLists =
        new Dictionary<bool, Action<PWorkspace>>
        {
            [true] = PWorkspaceListAttach,
            [false] = pWorkspace => { },
        };

    public PWorkspace(
        LStripTab lStripTab,
        LPreset? lExportSpecificState = null,
        LSceneTabRecord? lPreferenceTabLayout = null)
    {
        PWorkspaceTab = lStripTab;
        LWorkspace = new LWorkspace(lStripTab.LStripTabKey, lExportSpecificState);
        lStripTab.LStripWorkspaceAttach(LWorkspace);
        PWorkspaceSurface = pWorkspaceSurfaces[lStripTab.LStripTabKey](
            LWorkspace.LWorkspacePresetOwner, lPreferenceTabLayout);
        PWorkspaceFlow = PWorkspaceSurface.PTabFlow;
        PWorkspaceViewer = PWorkspaceSurface.PTabViewer;
        PWorkspaceList = PWorkspaceSurface.PTabList;
        PWorkspaceViewer?.PViewerAudioSet(LWorkspace.LWorkspaceAudioOnly);
        LWorkspace.LWorkspaceAttach(
            PWorkspaceList?.PListDocketRead(),
            PWorkspaceFlow?.LFlow.LFlowSection.LFlowSegment,
            PWorkspaceFlow?.LFlow,
            PWorkspaceViewer?.LViewer,
            PWorkspaceSurface.PTabStation,
            PWorkspaceSurface.PTabLayoutRead);
        PWorkspaceSurface.PTabAction?.PActionRelayAttach(lStripTab);
        LWorkspace.LWorkspaceFlowAttach += PWorkspaceFlowAttach;
        LWorkspace.LWorkspaceFlowClear += PWorkspaceFlowClear;
        LWorkspace.LWorkspaceMediaClose += PWorkspaceMediaClose;
        LWorkspace.LWorkspaceLosslesscutFind += PWorkspaceLosslesscutDefer;
        LWorkspace.LWorkspaceRelayDefer += PWorkspaceRelayDefer;
        LWorkspace.LWorkspaceRangeApply += PWorkspaceRangeApply;
        LWorkspace.LWorkspaceVolumeApply += PWorkspaceVolumeApply;
        LWorkspace.LWorkspaceSeekApply += PWorkspaceSeekApply;
        LWorkspace.LWorkspacePathsAdd += PWorkspacePathsAdd;
        LWorkspace.LWorkspaceSourceSelect += PWorkspaceSourceSelect;
        LWorkspace.LWorkspaceSourceOpen += PWorkspaceSourceOpen;
        pWorkspacePairs[LWorkspace.LWorkspaceFlowPresent](this);
        pWorkspaceLists[LWorkspace.LWorkspaceListPresent](this);
        PWorkspaceSurface.PTabWidthChange += PWorkspaceWidthRaise;
        PWorkspaceRoot = pWorkspaceRoots[LWorkspace.LWorkspaceSourcePresent](this);
    }

    public LStripTab PWorkspaceTab { get; }

    public LWorkspace LWorkspace { get; }

    public FrameworkElement PWorkspaceRoot { get; }

    public PTabSurface PWorkspaceSurface { get; }

    public PFlow? PWorkspaceFlow { get; }

    public PViewer? PWorkspaceViewer { get; }

    public PList? PWorkspaceList { get; }

    private event Action? PWorkspaceWidthChange;

    public void PWorkspaceWidthAttach(Action pHandler) => PWorkspaceWidthChange += pHandler;

    public void PWorkspaceWidthDetach(Action pHandler) => PWorkspaceWidthChange -= pHandler;

    private void PWorkspaceWidthRaise() => PWorkspaceWidthChange?.Invoke();

    public void PWorkspaceCommandApply(double pFlowHeight)
    {
        PWorkspaceFlow?.LFlow.LFlowCommandSet(true);
        PWorkspaceFlow?.LFlow.LFlowSectionSet(LWorkspace.LWorkspaceSectionVisible);
        PWorkspaceFlow?.PFlowHeightSet(pFlowHeight);
        PWorkspaceFlow?.PFlowOrderApply();
        PWorkspaceViewer?.PViewerCommandSet(true);
    }

    public void PWorkspaceCommandReset()
    {
        PWorkspaceViewer?.PViewerDragSet(false);
        PWorkspaceFlow?.LFlow.LFlowSectionSet(false);
        PWorkspaceFlow?.LFlow.LFlowCommandSet(false);
        PWorkspaceViewer?.PViewerCommandSet(false);
    }

    public void PWorkspaceFlowApply(double pFlowHeight)
    {
        PWorkspaceFlow?.PFlowHeightSet(pFlowHeight);
        PWorkspaceFlow?.PFlowOrderApply();
        PWorkspaceFlow?.PFlowPaletteApply();
    }

    public void PWorkspaceClose()
    {
        PWorkspaceStepRun("subscriptions", PWorkspaceDetach);
        PWorkspaceStepRun("deportment", LWorkspace.LWorkspaceClose);
        PWorkspaceStepRun("surface", PWorkspaceSurface.PTabClose);
        pWorkspacePairCloses[LWorkspace.LWorkspaceFlowPresent](this);
    }

    private void PWorkspaceDetach()
    {
        LWorkspace.LWorkspaceFlowAttach -= PWorkspaceFlowAttach;
        LWorkspace.LWorkspaceFlowClear -= PWorkspaceFlowClear;
        LWorkspace.LWorkspaceMediaClose -= PWorkspaceMediaClose;
        LWorkspace.LWorkspaceLosslesscutFind -= PWorkspaceLosslesscutDefer;
        LWorkspace.LWorkspaceRelayDefer -= PWorkspaceRelayDefer;
        LWorkspace.LWorkspaceRangeApply -= PWorkspaceRangeApply;
        LWorkspace.LWorkspaceVolumeApply -= PWorkspaceVolumeApply;
        LWorkspace.LWorkspaceSeekApply -= PWorkspaceSeekApply;
        LWorkspace.LWorkspacePathsAdd -= PWorkspacePathsAdd;
        LWorkspace.LWorkspaceSourceSelect -= PWorkspaceSourceSelect;
        LWorkspace.LWorkspaceSourceOpen -= PWorkspaceSourceOpen;
        PWorkspaceSurface.PTabWidthChange -= PWorkspaceWidthRaise;
    }

    private static void PWorkspacePairAttach(PWorkspace pWorkspace)
    {
        PFlow pFlow = pWorkspace.PWorkspaceFlow!;
        PViewer pViewer = pWorkspace.PWorkspaceViewer!;
        pFlow.LFlow.LFlowPlayingAttach(pViewer.PViewerPlayingRead);
        pViewer.PViewerClockTick += pFlow.LFlow.LFlowCursorUpdate;
        pFlow.LFlow.LFlowCursorChange += pViewer.PViewerSeek;
        pFlow.LFlow.LFlowDragChange += pViewer.PViewerDragSet;
        pFlow.LFlow.LFlowPlay += pViewer.PViewerPlay;
        pFlow.LFlow.LFlowPause += pViewer.PViewerPause;
        pFlow.LFlow.LFlowVolumeAdjust += pViewer.PViewerVolumeAdjust;
    }

    private static void PWorkspacePairClose(PWorkspace pWorkspace)
    {
        PFlow pFlow = pWorkspace.PWorkspaceFlow!;
        PViewer pViewer = pWorkspace.PWorkspaceViewer!;
        pViewer.PViewerClockTick -= pFlow.LFlow.LFlowCursorUpdate;
        pFlow.LFlow.LFlowCursorChange -= pViewer.PViewerSeek;
        pFlow.LFlow.LFlowDragChange -= pViewer.PViewerDragSet;
        pFlow.LFlow.LFlowPlay -= pViewer.PViewerPlay;
        pFlow.LFlow.LFlowPause -= pViewer.PViewerPause;
        pFlow.LFlow.LFlowVolumeAdjust -= pViewer.PViewerVolumeAdjust;
        pFlow.LFlow.LFlowPlayingAttach(null);
        PWorkspaceStepRun("flow", pFlow.PFlowClose);
        PWorkspaceStepRun("viewer", pViewer.PViewerClose);
    }

    private static void PWorkspaceListAttach(PWorkspace pWorkspace)
    {
        PList pList = pWorkspace.PWorkspaceList!;
        pList.PListPathChange += pWorkspace.LWorkspace.LWorkspacePathHandle;
        pList.PListClearChange += pWorkspace.LWorkspace.LWorkspaceRemovedHandle;
    }

    private static void PWorkspaceStepRun(string pStepName, Action pStep)
    {
        try
        {
            pStep();
        }
        catch (Exception pStepException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Workspace close step '{pStepName}' failed; teardown continues",
                pStepException);
        }
    }

    private void PWorkspaceFlowAttach(LMediaInfo lMediaInfo, string lSourcePath) =>
        PWorkspaceFlow?.PFlowAttach(lMediaInfo, lSourcePath, TimeSpan.Zero);

    private void PWorkspaceFlowClear() => PWorkspaceFlow?.PFlowClear();

    private void PWorkspaceMediaClose()
    {
        PWorkspaceViewer?.PViewerMediaClose(true);
        PWorkspaceFlow?.PFlowClear();
    }

    private void PWorkspaceLosslesscutDefer() =>
        PWorkspaceSurface.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(PWorkspaceLosslesscutFind));

    private void PWorkspaceLosslesscutFind() => PWorkspaceFlow?.LFlow.LFlowLosslesscut.LFlowLosslesscutFind();

    private void PWorkspaceRelayDefer(LRelay lRelay, TimeSpan lDuration) =>
        PWorkspaceSurface.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => LWorkspace.LWorkspaceRelayRestore(lRelay, lDuration)));

    private void PWorkspaceRangeApply(TimeSpan lOrigin, TimeSpan lLimit) =>
        PWorkspaceFlow?.LFlow.LFlowRangeSet(lOrigin, lLimit);

    private void PWorkspaceVolumeApply(double lVolume) => PWorkspaceViewer?.PViewerVolumeSet(lVolume);

    private void PWorkspaceSeekApply(TimeSpan lPosition) => PWorkspaceViewer?.PViewerSeek(lPosition);

    private async void PWorkspacePathsAdd(IReadOnlyList<string> lPaths)
    {
        await PWorkspaceList!.PListPathsAdd(lPaths);
        LWorkspace.LWorkspaceSourceRun();
    }

    private void PWorkspaceSourceSelect(string lPath) => PWorkspaceList?.PListSelect(lPath);

    private void PWorkspaceSourceOpen(string lPath) => PWorkspaceViewer?.PViewerSourceOpen(lPath);

    private static FrameworkElement PWorkspaceGridBuild(PWorkspace pWorkspace)
    {
        var pSource = new PSource(pWorkspace.LWorkspace.LWorkspaceAudioOnly);
        var pInfo = new PInfo();
        pSource.PSourceAttach(pWorkspace.PWorkspaceViewer);
        pInfo.PInfoAttach(pWorkspace.PWorkspaceViewer);
        var pRoot = new Grid
        {
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        pRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(pSource, 0);
        UIElement pInfoRow = pWorkspace.PWorkspaceInfoBuild(pInfo);
        Grid.SetRow(pInfoRow, 1);
        Grid.SetRow(pWorkspace.PWorkspaceSurface, 2);
        pRoot.Children.Add(pSource);
        pRoot.Children.Add(pInfoRow);
        pRoot.Children.Add(pWorkspace.PWorkspaceSurface);
        return pRoot;
    }

    private UIElement PWorkspaceInfoBuild(PInfo pInfo)
    {
        var pToggleButton = new Button
        {
            Content = PWorkspaceIconCreate(),
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonSourceCreate()
        };
        pToggleButton.Click += (_, _) => PWorkspaceSurface.PTabExportToggle();

        var pRow = new Grid { Margin = new Thickness(16, 0, 16, 6) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pInfo, 0);
        Grid.SetColumn(pToggleButton, 2);
        pRow.Children.Add(pInfo);
        pRow.Children.Add(pToggleButton);
        return pRow;
    }

    private static Image PWorkspaceIconCreate() => new()
    {
        Width = 18,
        Height = 18,
        Stretch = System.Windows.Media.Stretch.Uniform,
        Source = PIcon.PIconRead("/PAsset/PPanel/PExportToggle.svg")
    };
}
