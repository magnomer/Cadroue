using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport
{
    private static Style? pExportButtonStyle;

    public static void PExportMissingShow() => MessageBox.Show(
        LLocalization.LLocalizationTextRead("ExportPreset.Missing.Body"),
        LLocalization.LLocalizationTextRead("ExportPreset.Missing.Title"),
        MessageBoxButton.OK,
        MessageBoxImage.Information);

    private UIElement PExportActionBuild()
    {
        var pGrid = new Grid { Margin = new Thickness(10, 4, 10, 0) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PExportButtonBuild(PExportPlusIcon, LLocalization.LLocalizationTextRead("ExportPreset.AddTooltip"), PExportPresetAdd));
        pLeftPanel.Children.Add(PExportButtonBuild(PExportMinusIcon, LLocalization.LLocalizationTextRead("ExportPreset.DeleteTooltip"), PExportPresetDelete));
        Grid.SetColumn(pLeftPanel, 0);
        pGrid.Children.Add(pLeftPanel);

        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pRightPanel.Children.Add(PExportButtonBuild(PExportSettingIcon, LLocalization.LLocalizationTextRead("ExportPreset.SettingsTooltip"), PExportDialogShow));
        pRightPanel.Children.Add(PExportButtonBuild(PExportExportIcon, LLocalization.LLocalizationTextRead("ExportPreset.ExportTooltip"), PExportPresetSave));
        pRightPanel.Children.Add(PExportButtonBuild(PExportImportIcon, LLocalization.LLocalizationTextRead("ExportPreset.ImportTooltip"), PExportPresetLoad));
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
                Source = PIcon.PIconRead(pIconPath, pEnabled ? PExportTextBrush : PExportMutedBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PExportStyleRead(),
            IsEnabled = pEnabled
        };
        if (pClick is not null)
        {
            pButton.Click += pClick;
        }

        return pButton;
    }

    private static Style PExportStyleRead()
    {
        pExportButtonStyle ??= PButton.PButtonPanelCreate();
        return pExportButtonStyle;
    }
}
