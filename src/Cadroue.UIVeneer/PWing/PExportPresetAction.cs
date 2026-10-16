using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PExport
{
    public static void PExportIncompatibleShow() => PSAnnouncement.PSAnnouncementShow(
        null,
        QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Incompatible.Title"),
        QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Incompatible.Body"));

    internal static string PExportMarkRead(string lPresetText, bool lPresetUnsupported) =>
        lPresetUnsupported
            ? $"{lPresetText} {QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Incompatible")}"
            : lPresetText;

    public static void PExportMissingShow() => PSAnnouncement.PSAnnouncementShow(
        null,
        QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Missing.Title"),
        QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Missing.Body"));

    private UIElement PExportActionBuild()
    {
        var pGrid = new Grid { Margin = new Thickness(10, 4, 10, 0) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PExportButtonBuild(
            PExportPlusIcon,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.AddTooltip"),
            PExportPresetAdd));
        pLeftPanel.Children.Add(PExportButtonBuild(
            PExportMinusIcon,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.DeleteTooltip"),
            PExportPresetDelete));
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
            PExportDialogShow));
        pRightPanel.Children.Add(PExportButtonBuild(
            PExportExportIcon,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.ExportTooltip"),
            PExportPresetSave));
        pRightPanel.Children.Add(PExportButtonBuild(
            PExportImportIcon,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.ImportTooltip"),
            PExportPresetLoad));
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
                Width = 20,
                Height = 20,
                Source = QIcon.QIconRead(pIconPath, pEnabled ? PExportTextBrush : PExportMutedBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            IsEnabled = pEnabled
        };
        if (pClick is not null)
        {
            pButton.Click += pClick;
        }

        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }
}
