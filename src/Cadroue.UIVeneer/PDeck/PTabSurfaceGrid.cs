using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Cadroue.UIVeneer.PPanel;
using PFlowControl = Cadroue.UIVeneer.PFlow.PFlow;

namespace Cadroue.UIVeneer.PDeck;

public abstract partial class PTabSurface : UserControl
{
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
            pColumnCompactFlags.Add(
                pPanels[index] is PList or PExport or PProcessing or PInspector or PClinic or PSection);
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
            pTabViewerIndex,
            PTabWidthRaise);
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
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xD7, 0xDF, 0xEA)),
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

    private static double PTabPanelRead(UIElement pPanel) => pPanel switch
    {
        FrameworkElement { MinWidth: > 0 } pElement => pElement.MinWidth,
        PExport => 300,
        PViewer => 320,
        PSection => 300,
        PGroup => 260,
        PList => 300,
        PProcessing => 184,
        PInspector => 360,
        PClinic => 300,
        _ => 180
    };
}
