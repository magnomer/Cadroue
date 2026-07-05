using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PEditTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();

    public PEditTab(LExportSpecificState lExportSpecificState)
    {
        var pAction = new PAction();
        pAction.PActionRun += LEdit.LEditDescribe;
        Content = PTabGridBuild(new UIElement[] { new PProcessing(), new PInspector(), pViewer, new PExport(lExportSpecificState) }, new PCompass(pFlow), pAction, pFlow);
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;

    private static Grid PTabGridBuild(IReadOnlyList<UIElement> pPanels, UIElement pCompass, UIElement pAction, UIElement pFlow)
    {
        var pGrid = new Grid { Margin = new Thickness(8, 0, 8, 0) };
        pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        int pGridColumnCount = (pPanels.Count * 2) - 1;
        for (int index = 0; index < pPanels.Count; index++)
        {
            int pPanelColumn = index * 2;
            pGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
                MinWidth = 120
            });
            Grid.SetColumn(pPanels[index], pPanelColumn);
            pGrid.Children.Add(pPanels[index]);
            if (index >= pPanels.Count - 1)
            {
                continue;
            }

            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            var pSplitter = new GridSplitter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = System.Windows.Media.Brushes.Transparent,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                ResizeDirection = GridResizeDirection.Columns,
                ShowsPreview = false,
                Focusable = false
            };
            Grid.SetColumn(pSplitter, pPanelColumn + 1);
            pGrid.Children.Add(pSplitter);
        }

        var pActionRowContent = new Grid { MinHeight = 72 };
        pActionRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pActionRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pAction, 2);
        pActionRowContent.Children.Add(pCompass);
        pActionRowContent.Children.Add(pAction);

        var pActionRowBox = new Border
        {
            Margin = new Thickness(8, 8, 8, 8),
            MinHeight = 74,
            Padding = new Thickness(10, 0, 10, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD7, 0xDF, 0xEA)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = pActionRowContent,
            SnapsToDevicePixels = true
        };
        Grid.SetRow(pActionRowBox, 1);
        Grid.SetColumnSpan(pActionRowBox, pGridColumnCount);
        Grid.SetRow(pFlow, 2);
        Grid.SetColumnSpan(pFlow, pGridColumnCount);
        pGrid.Children.Add(pActionRowBox);
        pGrid.Children.Add(pFlow);
        return pGrid;
    }
}
