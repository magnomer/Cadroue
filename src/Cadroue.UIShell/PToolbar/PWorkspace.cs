using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PDeck;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PHouse;
using Cadroue.UIShell.PPanel;
using Cadroue.UIShell.PFlow;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PToolbar;

public sealed class PWorkspace
{
    private readonly LHistory lWorkspaceHistory = new();
    private string pWorkspaceLosslesscutPath = string.Empty;
    private LRelay? pWorkspaceRelay;

    public PWorkspace(
        string pTabLayoutKey,
        LPreset? lExportSpecificState = null,
        LSceneTabRecord? lPreferenceTabLayout = null)
    {
        PWorkspaceExportState = lExportSpecificState ?? LPreset.LPresetInitialCreate(pTabLayoutKey);
        PWorkspacePresetOwner = new LPresetSelection(
            PWorkspaceExportState.LPresetRecordCreate(), PWorkspaceExportState.LPresetName);
        PWorkspacePresetOwner.LPresetSelectionChange += PWorkspacePresetHandle;
        PWorkspacePresetHandle();
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
        if (PWorkspaceViewer is not null && PWorkspaceFlow is not null && PWorkspaceSurface.PTabSectionVisible)
        {
            PWorkspaceViewer.PViewerMediaChange += PWorkspaceMediaHandle;
        }
        PWorkspaceInfo?.PInfoAttach(PWorkspaceViewer);
        PWorkspaceRoot = PWorkspaceRootCreate();

        lWorkspaceHistory.LHistoryReset(PWorkspaceStateRead());
        if (PWorkspaceFlow is not null)
        {
            PWorkspaceFlow.PFlowSectionChange += PWorkspaceSectionHandle;
            PWorkspaceFlow.PFlowMediaChange += PWorkspaceHistoryReset;
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
            PWorkspaceViewer?.PViewerProtectedCheck(pWorkspaceProtectedPaths) == true;

        bool pWorkspaceCleared = false;
        if (!pWorkspaceViewerProtected)
        {
            pWorkspaceCleared |= PWorkspaceViewer?.PViewerMediaClose(true) == true;
            pWorkspaceCleared |= PWorkspaceFlow?.PFlowClear() == true;
        }
        else if (PWorkspaceViewer!.PViewerPendingPath is { } pWorkspacePending
            && !pWorkspaceProtectedPaths.Contains(pWorkspacePending))
        {
            pWorkspaceCleared |= PWorkspaceViewer.PViewerLoadCancel();
        }

        if (PWorkspaceList is { } pList && pList.PListPathsRead().Count > 0)
        {
            pWorkspaceCleared |= pList.PListStaleClear(pWorkspaceActiveBatches) > 0;
        }

        return pWorkspaceCleared;
    }

    public PSection? PWorkspaceSection { get; }

    public PSource? PWorkspaceSource { get; }

    public PInfo? PWorkspaceInfo { get; }

    public void PWorkspaceClose()
    {
        PWorkspaceStepRun("subscriptions", PWorkspaceDetach);
        PWorkspaceStepRun("preset owner", PWorkspacePresetOwner.LPresetSelectionClose);
        PWorkspaceStepRun("surface", PWorkspaceSurface.PTabClose);
        if (PWorkspaceFlow is { } pFlow)
        {
            PWorkspaceStepRun("flow", pFlow.PFlowClose);
        }

        if (PWorkspaceViewer is { } pViewer)
        {
            PWorkspaceStepRun("viewer", pViewer.PViewerClose);
        }
    }

    private void PWorkspaceDetach()
    {
        if (PWorkspaceFlow is not null)
        {
            PWorkspaceFlow.PFlowSectionChange -= PWorkspaceSectionHandle;
            PWorkspaceFlow.PFlowMediaChange -= PWorkspaceHistoryReset;
        }

        if (PWorkspaceViewer is not null)
        {
            PWorkspaceViewer.PViewerMediaChange -= PWorkspaceMediaHandle;
            PWorkspaceViewer.PViewerMediaChange -= PWorkspaceRelayHandle;
        }

        pWorkspaceRelay = null;
        PWorkspaceExportState.LPresetChange -= PWorkspaceExportHandle;
        PWorkspacePresetOwner.LPresetSelectionChange -= PWorkspacePresetHandle;
    }

    private static void PWorkspaceStepRun(string pStepName, Action pStep)
    {
        try
        {
            pStep();
        }
        catch (Exception pStepException)
        {
            LTraceLog.LTraceErrorRecord($"Workspace close step '{pStepName}' failed; teardown continues", pStepException);
        }
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

    private void PWorkspaceHistoryReset()
        => lWorkspaceHistory.LHistoryReset(PWorkspaceStateRead());

    private bool PWorkspaceHistoryCheck()
        => PWorkspaceFlow is null || PWorkspaceFlow.PFlowEditCheck();

    public bool PWorkspaceUndo()
        => PWorkspaceHistoryCheck() && PWorkspaceHistoryApply(lWorkspaceHistory.LHistoryUndo());

    public bool PWorkspaceRedo()
        => PWorkspaceHistoryCheck() && PWorkspaceHistoryApply(lWorkspaceHistory.LHistoryRedo());

    private bool PWorkspaceHistoryApply(LHistoryEntry? lHistoryEntry)
    {
        if (lHistoryEntry is null)
        {
            return false;
        }

        lWorkspaceHistory.LHistoryApplying = true;
        try
        {
            PWorkspaceFlow?.PFlowSectionsSet(lHistoryEntry.LHistorySections, lHistoryEntry.LHistorySectionIndex);
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
        LRelay lRelay = LRelayPayload.LRelayCreate(
            pTabRecord.PTabLayoutKey,
            pTabRecord.PTabNameCustom,
            PWorkspaceExportState.LPresetRecordCreate(),
            PWorkspaceLayoutRead(),
            pDropLeft,
            pDropTop);
        if (PWorkspaceSurface.PTabList is { } pList)
        {
            lRelay.LRelayPaths.AddRange(pList.PListPathsRead());
        }

        if (PWorkspaceViewer is { } pViewer)
        {
            lRelay.LRelaySourcePath = pViewer.PViewerSourcePath ?? string.Empty;
            lRelay.LRelayPositionTicks = pViewer.PViewerPositionRead().Ticks;
            lRelay.LRelayVolume = pViewer.PViewerVolumeCurrent;
        }

        if (PWorkspaceFlow is { } pFlow)
        {
            LSegment lRelaySegment = pFlow.PFlowSegment;
            lRelay.LRelaySections = LRelayPayload.LRelayRecordsCreate(lRelaySegment.LSegmentListRead());
            lRelay.LRelaySectionIndex = lRelaySegment.LSegmentSelectionRead();
            if (pFlow.PFlowRangeRead() is var (lRelayOrigin, lRelayLimit))
            {
                lRelay.LRelayOriginTicks = lRelayOrigin.Ticks;
                lRelay.LRelayLimitTicks = lRelayLimit.Ticks;
            }
        }

        return lRelay;
    }

    public async void PWorkspaceRelayApply(LRelay lRelay)
    {
        if (PWorkspaceViewer is { } pRelayViewer && !string.IsNullOrWhiteSpace(lRelay.LRelaySourcePath))
        {
            pRelayViewer.PViewerMediaChange += PWorkspaceRelayHandle;
            pWorkspaceRelay = lRelay;
        }

        PList? pRelayList = PWorkspaceSurface.PTabList;
        if (pRelayList is not null && lRelay.LRelayPaths.Count > 0)
        {
            await pRelayList.PListPathsAdd(lRelay.LRelayPaths);
        }

        if (string.IsNullOrWhiteSpace(lRelay.LRelaySourcePath) || PWorkspaceViewer is null)
        {
            return;
        }

        if (pRelayList is not null && pRelayList.PListPathsRead().Contains(lRelay.LRelaySourcePath, StringComparer.OrdinalIgnoreCase))
        {
            pRelayList.PListSelect(lRelay.LRelaySourcePath);
            return;
        }

        PWorkspaceViewer.PViewerSourceOpen(lRelay.LRelaySourcePath);
    }

    private void PWorkspaceRelayHandle(LCargo lMediaStatus)
    {
        if (pWorkspaceRelay is not { } lRelay
            || PWorkspaceViewer is not { } pRelayViewer
            || !string.Equals(lMediaStatus.LCargoSourcePath, lRelay.LRelaySourcePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        pRelayViewer.PViewerMediaChange -= PWorkspaceRelayHandle;
        pWorkspaceRelay = null;
        if (lMediaStatus.LCargoMediaInfo is not { } lRelayMedia)
        {
            return;
        }

        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => PWorkspaceRelayRestore(lRelay, pRelayViewer, lRelayMedia.LMediaInfoDuration)));
    }

    private void PWorkspaceRelayRestore(LRelay lRelay, PViewer pRelayViewer, TimeSpan lRelayDuration)
    {
        if (PWorkspaceFlow is { } pRelayFlow)
        {
            IReadOnlyList<LPiece> lRelaySections = LRelayPayload.LRelaySegmentsCreate(lRelay.LRelaySections);
            if (lRelaySections.Count > 0)
            {
                pRelayFlow.PFlowSegment.LSegmentBoundSet(lRelaySections, lRelay.LRelaySectionIndex, lRelayDuration);
            }

            if (lRelay.LRelayOriginTicks is { } lRelayOrigin && lRelay.LRelayLimitTicks is { } lRelayLimit)
            {
                pRelayFlow.PFlowRangeSet(TimeSpan.FromTicks(lRelayOrigin), TimeSpan.FromTicks(lRelayLimit));
            }
        }

        if (lRelay.LRelayVolume is { } lRelayVolume)
        {
            pRelayViewer.PViewerVolumeSet(lRelayVolume);
        }

        if (lRelay.LRelayPositionTicks > 0)
        {
            pRelayViewer.PViewerSeek(TimeSpan.FromTicks(lRelay.LRelayPositionTicks));
        }
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
        Source = PIcon.PIconRead("/PAsset/PPanel/PExportToggle.svg")
    };

    private static PTabSurface PWorkspaceSurfaceCreate(
        string pTabLayoutKey,
        LPresetSelection lPresetOwner,
        LSceneTabRecord? lPreferenceTabLayout)
    {
        return pTabLayoutKey switch
        {
            "Edit" => new PEditTab(lPresetOwner, lPreferenceTabLayout),
            "Fix" => new PFixTab(lPresetOwner, lPreferenceTabLayout),
            "Audio" => new PAudioTab(lPresetOwner, lPreferenceTabLayout),
            "Convert" => new PConvertTab(lPresetOwner, lPreferenceTabLayout),
            "Merge" => new PMergeTab(lPresetOwner, lPreferenceTabLayout),
            "Funnel" => new PFunnelTab(lPreferenceTabLayout),
            "Worklist" => new PWorklistTab(lPreferenceTabLayout),
            _ => new PSplitTab(lPresetOwner, lPreferenceTabLayout)
        };
    }
}
