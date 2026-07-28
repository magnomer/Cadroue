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
    public abstract LPreferenceTabLayoutRecord PTabLayoutRead();

    public void PTabExportToggle()
    {
        if (PTabLayoutStateRead() is { } pState)
        {
            pState.PExportToggle();
        }
    }

    protected static Grid PTabGridBuild(
        IReadOnlyList<UIElement> pPanels,
        UIElement pCompass,
        UIElement pAction,
        UIElement pFlow,
        LPreferenceTabLayoutRecord? lPreferenceTabLayout)
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
        var pPanelColumns = new List<ColumnDefinition>(pPanels.Count);
        var pPanelCompactFlags = new List<bool>(pPanels.Count);
        var pSplitterElements = new List<UIElement>(Math.Max(0, pPanels.Count - 1));
        var pSplitterColumnDefinitions = new List<ColumnDefinition>(Math.Max(0, pPanels.Count - 1));
        var pSplitterColumns = new List<int>(Math.Max(0, pPanels.Count - 1));
        for (int index = 0; index < pPanels.Count; index++)
        {
            int pPanelColumn = index * 2;
            var pPanelDefinition = new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
                MinWidth = PTabPanelMinWidthRead(pPanels[index])
            };
            pPanelGrid.ColumnDefinitions.Add(pPanelDefinition);
            pPanelColumns.Add(pPanelDefinition);
            pPanelCompactFlags.Add(pPanels[index] is PList or PExport);
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

        var pPanelLayout = PResizableColumnLayout.PAttach(
            pPanelGrid,
            pPanelColumns,
            lPreferenceTabLayout?.PanelWidths,
            pPanelCompactFlags);
        for (int index = 0; index < pSplitterColumns.Count; index++)
        {
            var pSplitter = pPanelLayout.PSplitterBuild(index);
            Grid.SetColumn(pSplitter, pSplitterColumns[index]);
            pPanelGrid.Children.Add(pSplitter);
            pSplitterElements.Add(pSplitter);
        }

        Grid.SetRow(pPanelGrid, 0);
        pGrid.Children.Add(pPanelGrid);
        PTabGridState pTabState = PTabGridState.PTabGridStateCreate(
            pPanelLayout, pPanels, pPanelColumns, pSplitterColumnDefinitions, pSplitterElements);
        pGrid.Tag = pTabState;
        if (lPreferenceTabLayout?.ExportHidden == true)
        {
            pTabState.PExportSet(true);
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

    protected static LPreferenceTabLayoutRecord PTabLayoutRead(Grid pGrid)
    {
        var lPreferenceTabLayout = new LPreferenceTabLayoutRecord();
        if (pGrid.Tag is not PTabGridState pState)
        {
            return lPreferenceTabLayout;
        }

        lPreferenceTabLayout.ExportHidden = pState.PExportHidden;
        foreach (double pWeight in pState.PTabLayout.PWeightsRead())
        {
            lPreferenceTabLayout.PanelWidths.Add(pWeight);
        }

        return lPreferenceTabLayout;
    }

    private PTabGridState? PTabLayoutStateRead()
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
            PResizableColumnLayout pTabLayout,
            int? pExportPanelIndex,
            ColumnDefinition? pExportColumn,
            UIElement? pExportPanel,
            ColumnDefinition? pExportSplitterColumn,
            UIElement? pExportSplitter)
        {
            PTabLayout = pTabLayout;
            this.pExportPanelIndex = pExportPanelIndex;
            this.pExportColumn = pExportColumn;
            this.pExportPanel = pExportPanel;
            this.pExportSplitterColumn = pExportSplitterColumn;
            this.pExportSplitter = pExportSplitter;
        }

        public PResizableColumnLayout PTabLayout { get; }

        public static PTabGridState PTabGridStateCreate(
            PResizableColumnLayout pTabLayout,
            IReadOnlyList<UIElement> pPanels,
            IReadOnlyList<ColumnDefinition> pPanelColumns,
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
                        index,
                        pPanelColumns[index],
                        pPanels[index],
                        pSplitterIndex >= 0 ? pSplitterColumns[pSplitterIndex] : null,
                        pSplitterIndex >= 0 ? pSplitters[pSplitterIndex] : null);
                }
            }

            return new PTabGridState(pTabLayout, null, null, null, null, null);
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
                PTabLayout.PPanelHide(pExportPanelIndex.Value);
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
                PTabLayout.PPanelShow(pExportPanelIndex.Value);
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

    private static double PTabPanelMinWidthRead(UIElement pPanel) => pPanel switch
    {
        FrameworkElement { MinWidth: > 0 } pElement => pElement.MinWidth,
        PExport => 300,
        PViewer => 320,
        PSection => 280,
        PGroup => 260,
        PList => 300,
        PProcessing => 220,
        PInspector => 220,
        _ => 180
    };
}
