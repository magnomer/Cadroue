using Cadroue.Core;
using Cadroue.Infrastructure;
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
    private string pWorkspaceLosslesscutPath = string.Empty;

    public PWorkspace(
        string pTabLayoutKey,
        LPreset? lExportSpecificState = null,
        LSceneTabRecord? lPreferenceTabLayout = null)
    {
        PWorkspaceExportState = lExportSpecificState ?? LPreset.LPresetInitialCreate(pTabLayoutKey);
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

    internal LPreset PWorkspaceExportState { get; }

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
        PWorkspaceSurface.PTabClose();
        PWorkspaceFlow?.PFlowClose();
        PWorkspaceViewer?.PViewerClose();
    }

    private LHistoryEntry PWorkspaceStateRead() => new(
        PWorkspaceFlow?.PFlowSectionsRead() ?? Array.Empty<LSegment>(),
        PWorkspaceFlow?.PFlowSelectionRead(),
        PWorkspaceExportState.LPresetRecordCreate());


    private void PWorkspaceMediaHandle(LCargo pMediaStatus)
    {
        if (PWorkspaceFlow is null
            || pMediaStatus.LCargoMediaInfo is null
            || string.IsNullOrWhiteSpace(pMediaStatus.LCargoSourcePath))
        {
            pWorkspaceLosslesscutPath = string.Empty;
            return;
        }

        string pMediaPath = System.IO.Path.GetFullPath(pMediaStatus.LCargoSourcePath);
        if (string.Equals(pWorkspaceLosslesscutPath, pMediaPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        pWorkspaceLosslesscutPath = pMediaPath;
        PFlowControl pLosslesscutFlow = PWorkspaceFlow;
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(pLosslesscutFlow.PFlowLosslesscutFind));
    }

    private void PWorkspaceSectionHandle(IReadOnlyList<LSegment> pSections, int? pSectionSelect)
        => PWorkspaceHistoryAdd();

    private void PWorkspaceExportHandle() => PWorkspaceHistoryAdd();

    private void PWorkspaceHistoryAdd()
        => lWorkspaceHistory.LHistoryAdd(PWorkspaceStateRead(), PWorkspaceHistoryMaximum);

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
            PWorkspaceExportState.LPresetCopy(LPreset.LPresetStateCreate(lHistoryEntry.LHistoryExport));
        }
        finally
        {
            lWorkspaceHistory.LHistoryApplying = false;
        }

        return true;
    }

    public LSceneTabRecord PWorkspaceLayoutRead() => PWorkspaceSurface.PTabLayoutRead();

    public LRelay PWorkspaceRelayCreate(PTabRecord pTabRecord, double pDropLeft, double pDropTop)
    {
        var lRelay = new LRelay
        {
            LRelayLayoutKey = pTabRecord.PTabLayoutKey,
            LRelayCustomName = pTabRecord.PTabNameCustom,
            LRelayExport = PWorkspaceExportState.LPresetRecordCreate(),
            LRelayLayout = PWorkspaceLayoutRead(),
            LRelaySourcePath = PWorkspaceViewer?.PViewerSourcePath ?? string.Empty,
            LRelayDropLeft = pDropLeft,
            LRelayDropTop = pDropTop,
            LRelaySenderProcess = Environment.ProcessId,
            LRelayId = Guid.NewGuid().ToString("N")
        };

        if (PWorkspaceFlow is { } pFlow)
        {
            lRelay.LRelaySections = pFlow.PFlowSectionsRead()
                .Select(lSegment => new LRelaySectionRecord
                {
                    LRelayStartTicks = lSegment.LSegmentStart.Ticks,
                    LRelayEndTicks = lSegment.LSegmentEnd.Ticks,
                    LRelayColorIndex = lSegment.LSegmentColorIndex,
                    LRelayName = lSegment.LSegmentName,
                    LRelayPrefix = lSegment.LSegmentPrefix,
                    LRelaySuffix = lSegment.LSegmentSuffix,
                    LRelayHidden = lSegment.LSegmentHidden
                })
                .ToList();
            lRelay.LRelaySectionIndex = pFlow.PFlowSelectionRead();
        }

        return lRelay;
    }

    public void PWorkspaceRelayApply(LRelay lRelay)
    {
        if (PWorkspaceViewer is null || string.IsNullOrWhiteSpace(lRelay.LRelaySourcePath))
        {
            return;
        }

        IReadOnlyList<PFlow.LSegment> lRelaySections = lRelay.LRelaySections
            .Select(lSection => new PFlow.LSegment(
                TimeSpan.FromTicks(lSection.LRelayStartTicks),
                TimeSpan.FromTicks(lSection.LRelayEndTicks),
                lSection.LRelayColorIndex,
                lSection.LRelayName)
            {
                LSegmentPrefix = lSection.LRelayPrefix,
                LSegmentSuffix = lSection.LRelaySuffix,
                LSegmentHidden = lSection.LRelayHidden
            })
            .ToList();
        int? lRelaySectionSelect = lRelay.LRelaySectionIndex;
        PViewer pRelayViewer = PWorkspaceViewer;

        void PWorkspaceRelayHandle(LCargo lMediaStatus)
        {
            pRelayViewer.PViewerMediaChange -= PWorkspaceRelayHandle;
            if (lMediaStatus.LCargoMediaInfo is null || PWorkspaceFlow is null || lRelaySections.Count == 0)
            {
                return;
            }

            PFlowControl pRelayFlow = PWorkspaceFlow;
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => pRelayFlow.PFlowSectionsSet(lRelaySections, lRelaySectionSelect)));
        }

        pRelayViewer.PViewerMediaChange += PWorkspaceRelayHandle;
        pRelayViewer.PViewerSourceOpen(lRelay.LRelaySourcePath);
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
        UIElement pInfoRow = PWorkspaceInfoBuild(PWorkspaceInfo);
        Grid.SetRow(pInfoRow, 1);
        Grid.SetRow(PWorkspaceSurface, 2);
        pRoot.Children.Add(PWorkspaceSource);
        pRoot.Children.Add(pInfoRow);
        pRoot.Children.Add(PWorkspaceSurface);
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
        Source = PIcon.PIconRead("/PAssets/PPanels/PExportToggle.svg")
    };

    private static PTabSurface PWorkspaceSurfaceCreate(
        string pTabLayoutKey,
        LPreset lExportSpecificState,
        LSceneTabRecord? lPreferenceTabLayout)
    {
        return pTabLayoutKey switch
        {
            "Edit" => new PEditTab(lExportSpecificState, lPreferenceTabLayout),
            "Audio" => new PAudioTab(lExportSpecificState, lPreferenceTabLayout),
            "Convert" => new PConvertTab(lExportSpecificState, lPreferenceTabLayout),
            "Merge" => new PMergeTab(lExportSpecificState, lPreferenceTabLayout),
            "Funnel" => new PFunnelTab(lPreferenceTabLayout),
            "Worklist" => new PWorklistTab(lPreferenceTabLayout),
            _ => new PSplitTab(lExportSpecificState, lPreferenceTabLayout)
        };
    }
}
