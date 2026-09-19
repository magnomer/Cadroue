using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private UIElement PInspectorEdgeBuild()
    {
        var pCropGrid = new Grid { Margin = new Thickness(0, 14, 0, 4) };
        for (int pColumn = 0; pColumn < 3; pColumn++)
        {
            pCropGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (int pRow = 0; pRow < 3; pRow++)
        {
            pCropGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        PInspectorCellAdd(
            pCropGrid,
            PInspectorCellBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Top"), pInspectorInsetTop),
            0,
            1);
        PInspectorCellAdd(
            pCropGrid,
            PInspectorCellBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Left"), pInspectorInsetLeft),
            1,
            0);
        PInspectorCellAdd(
            pCropGrid,
            PInspectorCellBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Right"), pInspectorInsetRight),
            1,
            2);
        PInspectorCellAdd(
            pCropGrid,
            PInspectorCellBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Bottom"), pInspectorInsetBottom),
            2,
            1);
        PInspectorCellAdd(pCropGrid, PInspectorResolutionBuild(), 1, 1);
        PInspectorCellAdd(pCropGrid, PInspectorResetBuild(), 2, 2);
        return pCropGrid;
    }

    private UIElement PInspectorResolutionBuild() => new Border
    {
        MinWidth = 84,
        Height = PInspectorFieldHeight,
        Padding = new Thickness(7, 0, 7, 0),
        Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC)),
        BorderBrush = PPanelLineBrush,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(3),
        ToolTip = LLocalization.LLocalizationTextRead("Inspector.Crop.Resolution"),
        Child = pInspectorResolution
    };

    private UIElement PInspectorResetBuild()
    {
        var pResetButton = new Button
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Crop.Reset"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Crop.ResetTooltip"),
            Height = PInspectorFieldHeight,
            MinWidth = 64,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Style = PButton.PButtonPanelCreate(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(3)
        };
        pResetButton.Click += (_, _) => PInspectorEdgesReset();
        return pResetButton;
    }

    private static void PInspectorCellAdd(Grid pCropGrid, UIElement pCell, int pRow, int pColumn)
    {
        Grid.SetRow(pCell, pRow);
        Grid.SetColumn(pCell, pColumn);
        pCropGrid.Children.Add(pCell);
    }

    private static UIElement PInspectorCellBuild(string pCellLabel, TextBox pCellBox)
    {
        var pCellPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 3)
        };
        pCellPanel.Children.Add(new TextBlock
        {
            Text = pCellLabel,
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 3)
        });
        pCellPanel.Children.Add(pCellBox);
        return pCellPanel;
    }
}
