using Cadroue.Application;
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
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PControlBar;

public sealed class PWorkspace
{
    private readonly LHistory lWorkspaceHistory = new();
    private string pWorkspaceLosslesscutPath = string.Empty;

    public PWorkspace(
        string pTabLayoutKey,
        LPreset? lExportSpecificState = null,
        LSceneTabRecord? lPreferenceTabLayout = null)
    {
        PWorkspaceExportState = lExportSpecificState ?? LPreset.LPresetInitialCreate(pTabLayoutKey);
        PWorkspacePresetOwner = new LPresetSelection(
            PWorkspaceExportState.LPresetRecordCreate(), PWorkspaceExportState.LPresetName);
        PWorkspacePresetOwner.LPresetSelectionChange += PWorkspacePresetHandle;
        PWorkspaceSurface = PWorkspaceSurfaceCreate(
            pTabLayoutKey, PWorkspacePresetOwner, lPreferenceTabLayout);
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

    internal LPresetSelection PWorkspacePresetOwner { get; }

    public PFlowControl? PWorkspaceFlow { get; }

    public PViewer? PWorkspaceViewer { get; }

    public PList? PWorkspaceList { get; }

    public bool PWorkspaceMediaClear(IReadOnlySet<Guid> pWorkspaceActiveBatches)
    {
        IReadOnlySet<string> pWorkspaceProtectedPaths = PWorkspaceList?.PListProtectedRead(pWorkspaceActiveBatches)
            ?? (IReadOnlySet<string>)new HashSet<string>();

        bool pWorkspaceViewerProtected =
            !string.IsNullOrWhiteSpace(PWorkspaceViewer?.PViewerSourcePath)
            && pWorkspaceProtectedPaths.Contains(PWorkspaceViewer!.PViewerSourcePath!);

        bool pWorkspaceCleared = false;
        if (!pWorkspaceViewerProtected)
        {
            pWorkspaceCleared |= PWorkspaceViewer?.PViewerMediaClose(true) == true;
            pWorkspaceCleared |= PWorkspaceFlow?.PFlowClear() == true;
        }

        if (PWorkspaceList is { } pList && pList.PListPathsRead().Count > 0)
        {
            pWorkspaceCleared |= pList.PListStaleClear(pWorkspaceActiveBatches) > 0;
        }

        PWorkspaceSurface.PTabGroup?.PGroupClear(pWorkspaceProtectedPaths);
        return pWorkspaceCleared;
    }

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
        PWorkspacePresetOwner.LPresetSelectionChange -= PWorkspacePresetHandle;
        PWorkspaceSurface.PTabClose();
        PWorkspaceFlow?.PFlowClose();
        PWorkspaceViewer?.PViewerClose();
    }

    private LHistoryEntry PWorkspaceStateRead() => new(
        PWorkspaceFlow?.PFlowSectionsRead() ?? Array.Empty<LPiece>(),
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

    private void PWorkspaceSectionHandle(IReadOnlyList<LPiece> pSections, int? pSectionSelect)
        => PWorkspaceHistoryAdd();

    private void PWorkspaceExportHandle() => PWorkspaceHistoryAdd();

    private void PWorkspacePresetHandle()
        => PWorkspaceExportState.LPresetCopy(LPreset.LPresetStateCreate(PWorkspacePresetOwner.LPresetSelectionValue));

    private void PWorkspaceHistoryAdd()
        => lWorkspaceHistory.LHistoryAdd(PWorkspaceStateRead());

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
            PWorkspacePresetOwner.LPresetSelectionValue = lHistoryEntry.LHistoryExport;
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
        IReadOnlyList<LPiece> lRelaySections = Array.Empty<LPiece>();
        int? lRelaySectionIndex = null;
        if (PWorkspaceFlow is { } pFlow)
        {
            LSegment lRelaySegment = pFlow.PFlowSegment;
            lRelaySections = lRelaySegment.LSegmentListRead();
            lRelaySectionIndex = lRelaySegment.LSegmentSelectionRead();
        }

        return LRelayPayload.LRelayCreate(
            pTabRecord.PTabLayoutKey,
            pTabRecord.PTabNameCustom,
            PWorkspaceExportState.LPresetRecordCreate(),
            PWorkspaceLayoutRead(),
            PWorkspaceViewer?.PViewerSourcePath ?? string.Empty,
            pDropLeft,
            pDropTop,
            lRelaySections,
            lRelaySectionIndex);
    }

    public void PWorkspaceRelayApply(LRelay lRelay)
    {
        if (PWorkspaceViewer is null || string.IsNullOrWhiteSpace(lRelay.LRelaySourcePath))
        {
            return;
        }

        IReadOnlyList<LPiece> lRelaySections = LRelayPayload.LRelaySegmentsCreate(lRelay.LRelaySections);
        int? lRelaySectionSelect = lRelay.LRelaySectionIndex;
        PViewer pRelayViewer = PWorkspaceViewer;

        void PWorkspaceRelayHandle(LCargo lMediaStatus)
        {
            pRelayViewer.PViewerMediaChange -= PWorkspaceRelayHandle;
            if (lMediaStatus.LCargoMediaInfo is null || PWorkspaceFlow is null || lRelaySections.Count == 0)
            {
                return;
            }

            LSegment pRelaySegment = PWorkspaceFlow.PFlowSegment;
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => pRelaySegment.LSegmentSet(lRelaySections, lRelaySectionSelect)));
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
        LPresetSelection lPresetOwner,
        LSceneTabRecord? lPreferenceTabLayout)
    {
        return pTabLayoutKey switch
        {
            "Edit" => new PEditTab(lPresetOwner, lPreferenceTabLayout),
            "Audio" => new PAudioTab(lPresetOwner, lPreferenceTabLayout),
            "Convert" => new PConvertTab(lPresetOwner, lPreferenceTabLayout),
            "Merge" => new PMergeTab(lPresetOwner, lPreferenceTabLayout),
            "Funnel" => new PFunnelTab(lPreferenceTabLayout),
            "Worklist" => new PWorklistTab(lPreferenceTabLayout),
            _ => new PSplitTab(lPresetOwner, lPreferenceTabLayout)
        };
    }
}
