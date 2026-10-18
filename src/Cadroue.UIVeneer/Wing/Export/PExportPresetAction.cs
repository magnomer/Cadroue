using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PExportPresetAction
{
    private const string PExportPlusIcon = "/PAsset/PPanel/PExportPlus.svg";
    private const string PExportMinusIcon = "/PAsset/PPanel/PExportMinus.svg";
    private const string PExportSettingIcon = "/PAsset/PPanel/PExportSetting.svg";
    private const string PExportImportIcon = "/PAsset/PPanel/PExportImport.svg";
    private const string PExportExportIcon = "/PAsset/PPanel/PExportExport.svg";

    internal static UIElement PExportActionBuild(PExportPresetCommand pExportCommand, PExportPresetFile pExportFile)
    {
        var pGrid = new Grid { Margin = new Thickness(10, 4, 10, 0) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PExportButtonBuild(
            PExportPlusIcon,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.AddTooltip"),
            pExportCommand.PExportPresetAdd));
        pLeftPanel.Children.Add(PExportButtonBuild(
            PExportMinusIcon,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.DeleteTooltip"),
            pExportCommand.PExportPresetDelete));
        Grid.SetColumn(pLeftPanel, 0);
        pGrid.Children.Add(pLeftPanel);

        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pRightPanel.Children.Add(PExportButtonBuild(
            PExportSettingIcon,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.SettingsTooltip"),
            pExportCommand.PExportDialogShow));
        pRightPanel.Children.Add(PExportButtonBuild(
            PExportExportIcon,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.ExportTooltip"),
            pExportFile.PExportPresetSave));
        pRightPanel.Children.Add(PExportButtonBuild(
            PExportImportIcon,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.ImportTooltip"),
            pExportFile.PExportPresetLoad));
        Grid.SetColumn(pRightPanel, 2);
        pGrid.Children.Add(pRightPanel);
        return pGrid;
    }

    private static Button PExportButtonBuild(string pIconPath, string pTooltip, RoutedEventHandler pClick)
    {
        Button pButton = PExportIconBuild(pIconPath, pTooltip);
        pButton.Click += pClick;
        return pButton;
    }

    private static Button PExportIconBuild(string pIconPath, string pTooltip)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 20,
                Height = 20,
                Source = QIcon.QIconRead(pIconPath, PExportTheme.PExportTextBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            IsEnabled = true
        };
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }
}
