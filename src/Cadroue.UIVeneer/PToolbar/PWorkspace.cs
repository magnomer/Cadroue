using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using System.Windows;
using System.Windows.Controls;
using Cadroue.UIVeneer.PDeck;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PPanel;
using Cadroue.UIVeneer.PFlow;
using PFlowControl = Cadroue.UIVeneer.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIVeneer.PToolbar;

public sealed partial class PWorkspace
{
    private readonly LHistory lWorkspaceHistory = new();

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
        }

        PWorkspaceRelayDetach();
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
            LTraceLog.LTraceErrorRecord(
                $"Workspace close step '{pStepName}' failed; teardown continues",
                pStepException);
        }
    }

    private LHistoryEntry PWorkspaceStateRead() => new(
        PWorkspaceFlow?.PFlowSectionsRead() ?? Array.Empty<LPiece>(),
        PWorkspaceFlow?.PFlowSelectionRead(),
        PWorkspaceExportState.LPresetRecordCreate());


    private void PWorkspaceMediaHandle(LCargo pMediaStatus)
    {
        if (PWorkspaceFlow is null)
        {
            return;
        }

        if (pMediaStatus.LCargoMediaInfo is null || string.IsNullOrWhiteSpace(pMediaStatus.LCargoSourcePath))
        {
            PWorkspaceFlow.LFlow.LFlowLosslesscutSet(string.Empty);
            return;
        }

        if (!PWorkspaceFlow.LFlow.LFlowLosslesscutSet(System.IO.Path.GetFullPath(pMediaStatus.LCargoSourcePath)))
        {
            return;
        }

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
        => PWorkspaceFlow is null || PWorkspaceFlow.LFlow.LFlowSectionEditable;

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
