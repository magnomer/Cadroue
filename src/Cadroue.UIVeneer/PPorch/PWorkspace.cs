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
    private static readonly IReadOnlyDictionary<string, Func<QWorkspace, LSceneTabEntry?, PTabSurface>>
        pWorkspaceSurfaces = new Dictionary<string, Func<QWorkspace, LSceneTabEntry?, PTabSurface>>
        {
            ["Split"] = (lWorkspace, lLayout) => new PSplitTab(lWorkspace, lLayout),
            ["Edit"] = (lWorkspace, lLayout) => new PEditTab(lWorkspace, lLayout),
            ["Fix"] = (lWorkspace, lLayout) => new PFixTab(lWorkspace, lLayout),
            ["Audio"] = (lWorkspace, lLayout) => new PAudioTab(lWorkspace, lLayout),
            ["Convert"] = (lWorkspace, lLayout) => new PConvertTab(lWorkspace, lLayout),
            ["Merge"] = (lWorkspace, lLayout) => new PMergeTab(lWorkspace, lLayout),
            ["Funnel"] = (lWorkspace, lLayout) => new PFunnelTab(lWorkspace, lLayout),
            ["Worklist"] = (lWorkspace, lLayout) => new PWorklistTab(lWorkspace.QWorkspaceRoster!, lLayout),
        };

    private static readonly IReadOnlyDictionary<bool, Func<PWorkspace, FrameworkElement>> pWorkspaceRoots =
        new Dictionary<bool, Func<PWorkspace, FrameworkElement>>
        {
            [true] = PWorkspaceGridBuild,
            [false] = pWorkspace => pWorkspace.PWorkspaceSurface,
        };

    private static readonly IReadOnlyDictionary<bool, Action<PWorkspace>> pWorkspacePairCloses =
        new Dictionary<bool, Action<PWorkspace>>
        {
            [true] = PWorkspacePairClose,
            [false] = pWorkspace => { },
        };

    public PWorkspace(
        QStrip lStrip,
        QStripTab lStripTab,
        LSceneTabEntry? lPreferenceTabLayout = null)
    {
        PWorkspaceTab = lStripTab;
        QWorkspace = lStripTab.QStripTabWorkspace;
        PWorkspaceSurface = pWorkspaceSurfaces[lStripTab.QStripTabKey](QWorkspace, lPreferenceTabLayout);
        PWorkspaceFlow = PWorkspaceSurface.PTabFlow;
        PWorkspaceViewer = PWorkspaceSurface.PTabViewer;
        PWorkspaceList = PWorkspaceSurface.PTabList;
        QWorkspace.QWorkspaceAttach(PWorkspaceSurface.PTabLayoutRead);
        PWorkspaceSurface.PTabStripAttach(lStrip, lStripTab);
        QWorkspace.QWorkspaceFlowAttached += PWorkspaceFlowAttach;
        QWorkspace.QWorkspaceFlowCleared += PWorkspaceFlowClear;
        QWorkspace.QWorkspaceMediaClosed += PWorkspaceMediaClose;
        QWorkspace.QWorkspaceLosslesscutFound += PWorkspaceLosslesscutDefer;
        QWorkspace.QWorkspaceRelay.QWorkspaceRelayDeferred += PWorkspaceRelayDefer;
        QWorkspace.QWorkspaceRelay.QWorkspaceRangeApplied += PWorkspaceRangeApply;
        QWorkspace.QWorkspaceRelay.QWorkspaceVolumeApplied += PWorkspaceVolumeApply;
        QWorkspace.QWorkspaceRelay.QWorkspaceSeekApplied += PWorkspaceSeekApply;
        QWorkspace.QWorkspaceRelay.QWorkspacePathsAdded += PWorkspacePathsAdd;
        QWorkspace.QWorkspaceRelay.QWorkspaceSourceSelected += PWorkspaceSourceSelect;
        QWorkspace.QWorkspaceRelay.QWorkspaceSourceOpened += PWorkspaceSourceOpen;
        PWorkspaceSurface.PTabWidthChanged += PWorkspaceWidthRaise;
        PWorkspaceRoot = pWorkspaceRoots[QWorkspace.QWorkspaceFacts.QWorkspaceSourcePresent](this);
    }

    public QStripTab PWorkspaceTab { get; }

    public QWorkspace QWorkspace { get; }

    public FrameworkElement PWorkspaceRoot { get; }

    public PTabSurface PWorkspaceSurface { get; }

    public PFlow? PWorkspaceFlow { get; }

    public PViewer? PWorkspaceViewer { get; }

    public PList? PWorkspaceList { get; }

    private event Action? PWorkspaceWidthChanged;

    public void PWorkspaceWidthAttach(Action pHandler) => PWorkspaceWidthChanged += pHandler;

    public void PWorkspaceWidthDetach(Action pHandler) => PWorkspaceWidthChanged -= pHandler;

    private void PWorkspaceWidthRaise() => PWorkspaceWidthChanged?.Invoke();

    public void PWorkspaceCommandApply(double pFlowHeight)
    {
        PWorkspaceFlow?.QFlow.QFlowCommandSet(true);
        PWorkspaceFlow?.QFlow.QFlowSectionSet(QWorkspace.QWorkspaceFacts.QWorkspaceSectionVisible);
        PWorkspaceFlow?.PFlowHeightSet(pFlowHeight);
        PWorkspaceFlow?.PFlowOrderApply();
        PWorkspaceViewer?.QViewer.QViewerMedia.QViewerCommandApply(true);
    }

    public void PWorkspaceCommandReset()
    {
        PWorkspaceViewer?.QViewer.QViewerPlayback.QViewerDragSet(false);
        PWorkspaceFlow?.QFlow.QFlowSectionSet(false);
        PWorkspaceFlow?.QFlow.QFlowCommandSet(false);
        PWorkspaceViewer?.QViewer.QViewerMedia.QViewerCommandApply(false);
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
        PWorkspaceStepRun("deportment", QWorkspace.QWorkspaceClose);
        PWorkspaceStepRun("surface", PWorkspaceSurface.PTabClose);
        pWorkspacePairCloses[QWorkspace.QWorkspaceFlowPresent](this);
    }

    private void PWorkspaceDetach()
    {
        QWorkspace.QWorkspaceFlowAttached -= PWorkspaceFlowAttach;
        QWorkspace.QWorkspaceFlowCleared -= PWorkspaceFlowClear;
        QWorkspace.QWorkspaceMediaClosed -= PWorkspaceMediaClose;
        QWorkspace.QWorkspaceLosslesscutFound -= PWorkspaceLosslesscutDefer;
        QWorkspace.QWorkspaceRelay.QWorkspaceRelayDeferred -= PWorkspaceRelayDefer;
        QWorkspace.QWorkspaceRelay.QWorkspaceRangeApplied -= PWorkspaceRangeApply;
        QWorkspace.QWorkspaceRelay.QWorkspaceVolumeApplied -= PWorkspaceVolumeApply;
        QWorkspace.QWorkspaceRelay.QWorkspaceSeekApplied -= PWorkspaceSeekApply;
        QWorkspace.QWorkspaceRelay.QWorkspacePathsAdded -= PWorkspacePathsAdd;
        QWorkspace.QWorkspaceRelay.QWorkspaceSourceSelected -= PWorkspaceSourceSelect;
        QWorkspace.QWorkspaceRelay.QWorkspaceSourceOpened -= PWorkspaceSourceOpen;
        PWorkspaceSurface.PTabWidthChanged -= PWorkspaceWidthRaise;
    }

    private static void PWorkspacePairClose(PWorkspace pWorkspace)
    {
        PWorkspaceStepRun("flow", pWorkspace.PWorkspaceFlow!.PFlowClose);
        PWorkspaceStepRun("viewer", pWorkspace.PWorkspaceViewer!.PViewerClose);
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
        PWorkspaceViewer?.QViewer.QViewerMedia.QViewerMediaClose(true);
        PWorkspaceFlow?.PFlowClear();
    }

    private void PWorkspaceLosslesscutDefer() =>
        PWorkspaceSurface.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(PWorkspaceLosslesscutFind));

    private void PWorkspaceLosslesscutFind() => PWorkspaceFlow?.QFlow.QFlowLosslesscut.QFlowLosslesscutFind();

    private void PWorkspaceRelayDefer(LRelay lRelay, TimeSpan lDuration) =>
        PWorkspaceSurface.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => QWorkspace.QWorkspaceRelay.QWorkspaceRelayRestore(lRelay, lDuration)));

    private void PWorkspaceRangeApply(TimeSpan lOrigin, TimeSpan lLimit) =>
        PWorkspaceFlow?.QFlow.QFlowSpool.QFlowRangeSet(lOrigin, lLimit);

    private void PWorkspaceVolumeApply(double lVolume) =>
        PWorkspaceViewer?.QViewer.QViewerPlayback.QViewerVolumeSet(lVolume);

    private void PWorkspaceSeekApply(TimeSpan lPosition) =>
        PWorkspaceViewer?.QViewer.QViewerPlayback.QViewerSeek(lPosition);

    private async void PWorkspacePathsAdd(IReadOnlyList<string> lPaths)
    {
        await PWorkspaceList!.QList.QListPathsAdd(lPaths);
        QWorkspace.QWorkspaceRelay.QWorkspaceSourceRun();
    }

    private void PWorkspaceSourceSelect(string lPath) => PWorkspaceList?.QList.QListListedSelect(lPath);

    private void PWorkspaceSourceOpen(string lPath) => PWorkspaceViewer?.QViewer.QViewerSource.QViewerSourceOpen(lPath);

    private static FrameworkElement PWorkspaceGridBuild(PWorkspace pWorkspace)
    {
        var pSource = new PSource(pWorkspace.QWorkspace.QWorkspaceSource!);
        var pInfo = new PInfo(pWorkspace.QWorkspace.QWorkspaceInfo!);
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
