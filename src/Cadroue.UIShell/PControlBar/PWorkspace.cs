using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PMainArea;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;
using Cadroue.UIShell.PFlow;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PControlBar;

public sealed class PWorkspace
{
    private const int PWorkspaceHistoryMaximum = 100;

    private readonly LHistory lWorkspaceHistory = new();
    private string pWorkspaceLosslessCutPath = string.Empty;

    public PWorkspace(
        string pTabLayoutKey,
        LExportSpecificState? lExportSpecificState = null,
        LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        PWorkspaceExportState = lExportSpecificState ?? LExportSpecificState.LPresetInitialCreate(pTabLayoutKey);
        PWorkspaceSurface = PWorkspaceSurfaceCreate(pTabLayoutKey, PWorkspaceExportState, lPreferenceTabLayout);
        bool pHasSourceInfo = pTabLayoutKey is not ("Merge" or "Worklist");
        bool pAudioOnlyAllowed = pTabLayoutKey == "Audio";
        PWorkspaceSource = pHasSourceInfo ? new PSource(pAudioOnlyAllowed) : null;
        PWorkspaceInfo = pHasSourceInfo ? new PInfo() : null;
        PWorkspaceFlow = PWorkspaceSurface.PTabFlow;
        PWorkspaceViewer = PWorkspaceSurface.PTabViewer;
        PWorkspaceList = PWorkspaceSurface.PTabList;
        PWorkspaceViewer?.PViewerAudioSet(pAudioOnlyAllowed);
        PWorkspaceSource?.PSourceAttach(PWorkspaceViewer);
        if (PWorkspaceViewer is not null && PWorkspaceFlow is not null && !pAudioOnlyAllowed)
        {
            PWorkspaceViewer.PViewerMediaChange += PWorkspaceMediaHandle;
        }
        PWorkspaceInfo?.PInfoAttach(PWorkspaceViewer);
        PWorkspaceRoot = PWorkspaceRootCreate();

        lWorkspaceHistory.LHistoryReset(PWorkspaceStateRead());
        if (PWorkspaceFlow is not null)
        {
            PWorkspaceFlow.PFlowSectionChange += PWorkspaceSectionHandle;
        }

        PWorkspaceExportState.LPresetChange += PWorkspaceExportHandle;
    }

    public FrameworkElement PWorkspaceRoot { get; }

    public PTabSurface PWorkspaceSurface { get; }

    internal LExportSpecificState PWorkspaceExportState { get; }

    public PFlowControl? PWorkspaceFlow { get; }

    public PViewer? PWorkspaceViewer { get; }

    public PList? PWorkspaceList { get; }

    public PSection? PWorkspaceSection { get; }

    public PSource? PWorkspaceSource { get; }

    public PInfo? PWorkspaceInfo { get; }

    public void PWorkspaceClose()
    {
        if (PWorkspaceFlow is not null)
        {
            PWorkspaceFlow.PFlowSectionChange -= PWorkspaceSectionHandle;
        }

        if (PWorkspaceViewer is not null)
        {
            PWorkspaceViewer.PViewerMediaChange -= PWorkspaceMediaHandle;
        }

        PWorkspaceExportState.LPresetChange -= PWorkspaceExportHandle;
        PWorkspaceFlow?.PFlowClose();
        PWorkspaceViewer?.PViewerClose();
    }

    private LHistoryEntry PWorkspaceStateRead() => new(
        PWorkspaceFlow?.PFlowSectionsRead() ?? Array.Empty<LSegment>(),
        PWorkspaceFlow?.PFlowSectionSelectRead(),
        LExportSpecificPresetRecord.LPresetRecordCreate(PWorkspaceExportState));


    private void PWorkspaceMediaHandle(LMediaOpenStatus pMediaStatus)
    {
        if (PWorkspaceFlow is null
            || pMediaStatus.LMediaOpenMediaInfo is null
            || string.IsNullOrWhiteSpace(pMediaStatus.LMediaOpenSourcePath))
        {
            pWorkspaceLosslessCutPath = string.Empty;
            return;
        }

        string pMediaPath = System.IO.Path.GetFullPath(pMediaStatus.LMediaOpenSourcePath);
        if (string.Equals(pWorkspaceLosslessCutPath, pMediaPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        pWorkspaceLosslessCutPath = pMediaPath;
        PFlowControl pLosslessCutFlow = PWorkspaceFlow;
        Application.Current?.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(pLosslessCutFlow.PFlowLosslessCutDetect));
    }

    private void PWorkspaceSectionHandle(IReadOnlyList<LSegment> pSections, int? pSectionSelect)
        => PWorkspaceHistoryMark();

    private void PWorkspaceExportHandle() => PWorkspaceHistoryMark();

    private void PWorkspaceHistoryMark()
        => lWorkspaceHistory.LHistoryPush(PWorkspaceStateRead(), PWorkspaceHistoryMaximum);

    public bool PWorkspaceUndo() => PWorkspaceHistoryApply(lWorkspaceHistory.LHistoryUndo());

    public bool PWorkspaceRedo() => PWorkspaceHistoryApply(lWorkspaceHistory.LHistoryRedo());

    private bool PWorkspaceHistoryApply(LHistoryEntry? lHistoryEntry)
    {
        if (lHistoryEntry is null)
        {
            return false;
        }

        lWorkspaceHistory.LHistoryApplying = true;
        try
        {
            PWorkspaceFlow?.PFlowSectionsSet(lHistoryEntry.LHistorySections, lHistoryEntry.LHistorySectionSelect);
            PWorkspaceExportState.LPresetCopy(lHistoryEntry.LHistoryExport.LPresetStateCreate());
        }
        finally
        {
            lWorkspaceHistory.LHistoryApplying = false;
        }

        return true;
    }

    public LPreferenceTabLayoutRecord PWorkspaceLayoutRead() => PWorkspaceSurface.PTabLayoutRead();

    public void PWorkspaceRelayApply(LRelay lRelay)
    {
        if (PWorkspaceViewer is null || string.IsNullOrWhiteSpace(lRelay.SourcePath))
        {
            return;
        }

        IReadOnlyList<PFlow.LSegment> lRelaySections = lRelay.LRelaySectionsCreate();
        int? lRelaySectionSelect = lRelay.SectionSelectIndex;
        PViewer pRelayViewer = PWorkspaceViewer;

        void PWorkspaceRelayMediaHandle(LMediaOpenStatus lMediaStatus)
        {
            pRelayViewer.PViewerMediaChange -= PWorkspaceRelayMediaHandle;
            if (lMediaStatus.LMediaOpenMediaInfo is null || PWorkspaceFlow is null || lRelaySections.Count == 0)
            {
                return;
            }

            PFlowControl pRelayFlow = PWorkspaceFlow;
            Application.Current?.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => pRelayFlow.PFlowSectionsSet(lRelaySections, lRelaySectionSelect)));
        }

        pRelayViewer.PViewerMediaChange += PWorkspaceRelayMediaHandle;
        pRelayViewer.PViewerSourceOpen(lRelay.SourcePath);
    }

    private FrameworkElement PWorkspaceRootCreate()
    {
        if (PWorkspaceSource is null || PWorkspaceInfo is null)
        {
            return PWorkspaceSurface;
        }

        var pRoot = new Grid
        {
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        pRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(PWorkspaceSource, 0);
        UIElement pInfoRow = PWorkspaceInfoRowBuild(PWorkspaceInfo);
        Grid.SetRow(pInfoRow, 1);
        Grid.SetRow(PWorkspaceSurface, 2);
        pRoot.Children.Add(PWorkspaceSource);
        pRoot.Children.Add(pInfoRow);
        pRoot.Children.Add(PWorkspaceSurface);
        return pRoot;
    }

    private UIElement PWorkspaceInfoRowBuild(PInfo pInfo)
    {
        var pToggleButton = new Button
        {
            Content = PWorkspaceExportIconCreate(),
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

    private static Image PWorkspaceExportIconCreate() => new()
    {
        Width = 18,
        Height = 18,
        Stretch = System.Windows.Media.Stretch.Uniform,
        Source = PIcon.PIconRead("/PAssets/PPanels/PExportToggle.svg")
    };

    private static PTabSurface PWorkspaceSurfaceCreate(
        string pTabLayoutKey,
        LExportSpecificState lExportSpecificState,
        LPreferenceTabLayoutRecord? lPreferenceTabLayout)
    {
        return pTabLayoutKey switch
        {
            "Edit" => new PEditTab(lExportSpecificState, lPreferenceTabLayout),
            "Audio" => new PAudioTab(lExportSpecificState, lPreferenceTabLayout),
            "Convert" => new PConvertTab(lExportSpecificState, lPreferenceTabLayout),
            "Merge" => new PMergeTab(lExportSpecificState, lPreferenceTabLayout),
            "Worklist" => new PWorklistTab(lPreferenceTabLayout),
            _ => new PSplitTab(lExportSpecificState, lPreferenceTabLayout)
        };
    }
}
