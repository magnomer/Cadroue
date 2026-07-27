using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport
{
    private static Style? pExportButtonStyle;

    private UIElement PExportActionBuild()
    {
        var pGrid = new Grid { Margin = new Thickness(10, 4, 10, 0) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PExportButtonBuild(PExportPlusIconPath, "Add a new preset", PExportPresetAdd));
        pLeftPanel.Children.Add(PExportButtonBuild(PExportMinusIconPath, "Delete the selected preset", PExportPresetDelete));
        Grid.SetColumn(pLeftPanel, 0);
        pGrid.Children.Add(pLeftPanel);

        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pRightPanel.Children.Add(PExportButtonBuild(PExportSettingIconPath, "Settings", PExportDialogShow));
        pRightPanel.Children.Add(PExportButtonBuild(PExportExportIconPath, "Export the selected preset to a file", PExportPresetSave));
        pRightPanel.Children.Add(PExportButtonBuild(PExportImportIconPath, "Import a preset from a file", PExportPresetLoad));
        Grid.SetColumn(pRightPanel, 2);
        pGrid.Children.Add(pRightPanel);
        return pGrid;
    }

    private Button PExportButtonBuild(string pIconPath, string pTooltip, RoutedEventHandler? pClick)
    {
        bool pEnabled = pClick is not null;
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pEnabled ? PTextBrush : PMutedBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PExportButtonStyleRead(),
            IsEnabled = pEnabled
        };
        if (pClick is not null)
        {
            pButton.Click += pClick;
        }

        return pButton;
    }

    private static Style PExportButtonStyleRead()
    {
        pExportButtonStyle ??= PButton.PButtonPanelCreate();
        return pExportButtonStyle;
    }
}
