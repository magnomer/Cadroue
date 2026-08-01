using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public abstract class PTabSurface : UserControl
{
    public abstract PFlowControl? PTabFlow { get; }
    public abstract PViewer? PTabViewer { get; }
    public virtual PList? PTabList => null;
    public virtual PGroup? PTabGroup => null;
    public PAction? PTabAction { get; protected set; }
    public virtual bool PTabBusyCheck() => false;
    public virtual void PTabClose() { }
    public abstract LSceneTabRecord PTabLayoutRead();

    protected const double PTabWidthPadding = 16;

    public event Action? PTabWidthChange;

    public void PTabExportToggle()
    {
        if (PTabStateRead() is { } pState)
        {
            pState.PExportToggle();
            PTabWidthRaise();
        }
    }

    public virtual double PTabWidthRead() =>
        PTabStateRead() is { } pState ? pState.PTabLayout.PColumnTotalRead() + PTabWidthPadding : 0;

    protected void PTabWidthRaise() => PTabWidthChange?.Invoke();

    protected static void PTabViewerAttach(PList pList, PViewer pViewer)
    {
        pList.PListClearChange += pRemovedPaths =>
        {
            if (pViewer.PViewerSourcePath is { } pLoadedPath
                && pRemovedPaths.Any(pRemoved => string.Equals(pRemoved, pLoadedPath, StringComparison.OrdinalIgnoreCase)))
            {
                pViewer.PViewerMediaClose();
            }
        };
    }

    protected Grid PTabGridBuild(
        IReadOnlyList<UIElement> pPanels,
        UIElement pCompass,
        UIElement pAction,
        UIElement pFlow,
        LSceneTabRecord? lPreferenceTabLayout)
    {
        var pGrid = new Grid
        {
            Margin = new Thickness(8, 0, 8, 0),
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var pPanelGrid = new Grid
        {
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var pColumnItems = new List<ColumnDefinition>(pPanels.Count);
        var pColumnCompactFlags = new List<bool>(pPanels.Count);
        var pSplitterElements = new List<UIElement>(Math.Max(0, pPanels.Count - 1));
        var pSplitterColumnDefinitions = new List<ColumnDefinition>(Math.Max(0, pPanels.Count - 1));
        var pSplitterColumns = new List<int>(Math.Max(0, pPanels.Count - 1));
        for (int index = 0; index < pPanels.Count; index++)
        {
            int pPanelColumn = index * 2;
            var pPanelDefinition = new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
                MinWidth = PTabPanelRead(pPanels[index])
            };
            pPanelGrid.ColumnDefinitions.Add(pPanelDefinition);
            pColumnItems.Add(pPanelDefinition);
            pColumnCompactFlags.Add(pPanels[index] is PList or PExport or PProcessing or PInspector or PSection);
            Grid.SetColumn(pPanels[index], pPanelColumn);
            pPanelGrid.Children.Add(pPanels[index]);
            if (index >= pPanels.Count - 1)
            {
                continue;
            }

            var pSplitterDefinition = new ColumnDefinition { Width = new GridLength(6) };
            pPanelGrid.ColumnDefinitions.Add(pSplitterDefinition);
            pSplitterColumnDefinitions.Add(pSplitterDefinition);
            pSplitterColumns.Add(pPanelColumn + 1);
        }

        int pTabViewerIndex = -1;
        for (int index = 0; index < pPanels.Count; index++)
        {
            if (pPanels[index] is PViewer)
            {
                pTabViewerIndex = index;
                break;
            }
        }

        var pPanelLayout = PColumn.PColumnAttach(
            pPanelGrid,
            pColumnItems,
            lPreferenceTabLayout?.LScenePanelWidths,
            pColumnCompactFlags,
            pTabViewerIndex);
        for (int index = 0; index < pSplitterColumns.Count; index++)
        {
            var pSplitter = pPanelLayout.PColumnSplitterBuild(index);
            Grid.SetColumn(pSplitter, pSplitterColumns[index]);
            pPanelGrid.Children.Add(pSplitter);
            pSplitterElements.Add(pSplitter);
        }

        Grid.SetRow(pPanelGrid, 0);
        pGrid.Children.Add(pPanelGrid);
        PTabGridState pTabState = PTabGridState.PTabStateCreate(
            pPanelLayout, pPanels, pColumnItems, pSplitterColumnDefinitions, pSplitterElements);
        if (pAction is PAction pTabAction)
        {
            pTabState.PTabAction = pTabAction;
            pTabAction.PActionAutoApply(lPreferenceTabLayout?.LSceneAutoRelay ?? false);
        }

        pGrid.Tag = pTabState;
        if (lPreferenceTabLayout?.LSceneExportHidden == true)
        {
            pTabState.PExportSet(true);
        }

        for (int index = 0; index < pPanels.Count; index++)
        {
            PTabCollapseAttach(pPanels[index], index, pPanelLayout, PTabWidthRaise);
        }

        if (lPreferenceTabLayout?.LScenePanelsCollapsed is { } pCollapsedIndexes)
        {
            foreach (int pCollapsedIndex in pCollapsedIndexes)
            {
                if (pCollapsedIndex >= 0 && pCollapsedIndex < pPanels.Count)
                {
                    PTabCollapseSet(pPanels[pCollapsedIndex], true);
                }
            }
        }

        var pActionRowContent = new Grid { MinHeight = 72 };
        pActionRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pAction, 1);
        pActionRowContent.Children.Add(pCompass);
        pActionRowContent.Children.Add(pAction);

        var pActionRowBox = new Border
        {
            Margin = new Thickness(8, 8, 8, 8),
            MinHeight = 74,
            Padding = new Thickness(10, 0, 10, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD7, 0xDF, 0xEA)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = pActionRowContent,
            SnapsToDevicePixels = true
        };
        Grid.SetRow(pActionRowBox, 1);
        Grid.SetRow(pFlow, 2);
        pGrid.Children.Add(pActionRowBox);
        pGrid.Children.Add(pFlow);
        return pGrid;
    }

    protected static LSceneTabRecord PTabLayoutRead(Grid pGrid)
    {
        var lPreferenceTabLayout = new LSceneTabRecord();
        if (pGrid.Tag is not PTabGridState pState)
        {
            return lPreferenceTabLayout;
        }

        lPreferenceTabLayout.LSceneExportHidden = pState.PExportHidden;
        lPreferenceTabLayout.LSceneAutoRelay = pState.PTabAction?.PActionAutoRelay ?? false;
        for (int index = 0; index < pState.PTabPanels.Count; index++)
        {
            if (PTabCollapseCheck(pState.PTabPanels[index]))
            {
                lPreferenceTabLayout.LScenePanelsCollapsed.Add(index);
            }
        }

        foreach (double pWeight in pState.PTabLayout.PColumnWeightsRead())
        {
            lPreferenceTabLayout.LScenePanelWidths.Add(pWeight);
        }

        return lPreferenceTabLayout;
    }

    private PTabGridState? PTabStateRead()
    {
        return Content is Grid pGrid && pGrid.Tag is PTabGridState pState ? pState : null;
    }

    private sealed class PTabGridState
    {
        private readonly ColumnDefinition? pExportColumn;
        private readonly ColumnDefinition? pExportSplitterColumn;
        private readonly UIElement? pExportPanel;
        private readonly UIElement? pExportSplitter;
        private readonly int? pExportPanelIndex;
        private bool pExportVisible = true;

        private PTabGridState(
            PColumn pTabLayout,
            IReadOnlyList<UIElement> pTabPanels,
            int? pExportPanelIndex,
            ColumnDefinition? pExportColumn,
            UIElement? pExportPanel,
            ColumnDefinition? pExportSplitterColumn,
            UIElement? pExportSplitter)
        {
            PTabLayout = pTabLayout;
            PTabPanels = pTabPanels;
            this.pExportPanelIndex = pExportPanelIndex;
            this.pExportColumn = pExportColumn;
            this.pExportPanel = pExportPanel;
            this.pExportSplitterColumn = pExportSplitterColumn;
            this.pExportSplitter = pExportSplitter;
        }

        public PColumn PTabLayout { get; }

        public IReadOnlyList<UIElement> PTabPanels { get; }

        public PAction? PTabAction { get; set; }

        public static PTabGridState PTabStateCreate(
            PColumn pTabLayout,
            IReadOnlyList<UIElement> pPanels,
            IReadOnlyList<ColumnDefinition> pColumnItems,
            IReadOnlyList<ColumnDefinition> pSplitterColumns,
            IReadOnlyList<UIElement> pSplitters)
        {
            for (int index = pPanels.Count - 1; index >= 0; index--)
            {
                if (pPanels[index] is PExport)
                {
                    int pSplitterIndex = index - 1;
                    return new PTabGridState(
                        pTabLayout,
                        pPanels,
                        index,
                        pColumnItems[index],
                        pPanels[index],
                        pSplitterIndex >= 0 ? pSplitterColumns[pSplitterIndex] : null,
                        pSplitterIndex >= 0 ? pSplitters[pSplitterIndex] : null);
                }
            }

            return new PTabGridState(pTabLayout, pPanels, null, null, null, null, null);
        }

        public bool PExportHidden => pExportPanelIndex is not null && !pExportVisible;

        public void PExportToggle() => PExportSet(pExportVisible);

        public void PExportSet(bool pExportHide)
        {
            if (pExportColumn is null || pExportPanel is null || pExportPanelIndex is null)
            {
                return;
            }

            if (pExportHide == !pExportVisible)
            {
                return;
            }

            if (pExportHide)
            {
                PTabLayout.PColumnHide(pExportPanelIndex.Value);
                pExportPanel.Visibility = Visibility.Collapsed;
                if (pExportSplitterColumn is not null)
                {
                    pExportSplitterColumn.Width = new GridLength(0);
                }

                if (pExportSplitter is not null)
                {
                    pExportSplitter.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                PTabLayout.PColumnShow(pExportPanelIndex.Value);
                pExportPanel.Visibility = Visibility.Visible;
                if (pExportSplitterColumn is not null)
                {
                    pExportSplitterColumn.Width = new GridLength(6);
                }

                if (pExportSplitter is not null)
                {
                    pExportSplitter.Visibility = Visibility.Visible;
                }
            }

            pExportVisible = !pExportHide;
        }
    }

    internal static bool PTabCollapseCheck(UIElement pPanel) => pPanel switch
    {
        PList pListPanel => pListPanel.PListMinimizedCheck(),
        PProcessing pProcessingPanel => pProcessingPanel.PProcessingMinimizedCheck(),
        PInspector pInspectorPanel => pInspectorPanel.PInspectorMinimizedCheck(),
        PSection pSectionPanel => pSectionPanel.PSectionMinimizedCheck(),
        _ => false
    };

    internal static void PTabCollapseSet(UIElement pPanel, bool pCollapsed)
    {
        switch (pPanel)
        {
            case PList pListPanel: pListPanel.PListMinimizeSet(pCollapsed); break;
            case PProcessing pProcessingPanel: pProcessingPanel.PProcessingMinimizeSet(pCollapsed); break;
            case PInspector pInspectorPanel: pInspectorPanel.PInspectorMinimizeSet(pCollapsed); break;
            case PSection pSectionPanel: pSectionPanel.PSectionMinimizeSet(pCollapsed); break;
        }
    }

    private static double PTabCollapseRead(UIElement pPanel) => pPanel switch
    {
        PList => PList.PListStripWidth,
        PProcessing => PProcessing.PProcessingStripWidth,
        PInspector => PInspector.PInspectorStripWidth,
        PSection => PSection.PSectionStripWidth,
        _ => 0
    };

    private static void PTabCollapseAttach(
        UIElement pPanel,
        int pPanelIndex,
        PColumn pPanelLayout,
        Action pCollapseNotify)
    {
        double pStripWidth = PTabCollapseRead(pPanel);
        if (pStripWidth <= 0)
        {
            return;
        }

        void pCollapseApply(bool pCollapsed)
        {
            pPanelLayout.PColumnWidthSet(pPanelIndex, pCollapsed ? pStripWidth : 0);
            pCollapseNotify();
        }

        switch (pPanel)
        {
            case PList pListPanel: pListPanel.PListMinimizeChange += pCollapseApply; break;
            case PProcessing pProcessingPanel: pProcessingPanel.PProcessingMinimizeChange += pCollapseApply; break;
            case PInspector pInspectorPanel: pInspectorPanel.PInspectorMinimizeChange += pCollapseApply; break;
            case PSection pSectionPanel: pSectionPanel.PSectionMinimizeChange += pCollapseApply; break;
        }
    }

    private static double PTabPanelRead(UIElement pPanel) => pPanel switch
    {
        FrameworkElement { MinWidth: > 0 } pElement => pElement.MinWidth,
        PExport => 300,
        PViewer => 320,
        PSection => 300,
        PGroup => 260,
        PList => 300,
        PProcessing => 184,
        PInspector => 300,
        _ => 180
    };
}
