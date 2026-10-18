using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PExportPresetName
{
    private const string PExportCheckIcon = "/PAsset/PPanel/PExportCheck.svg";
    private const string PExportCancelIcon = "/PAsset/PPanel/PExportCancel.svg";
    private static readonly Brush PExportApplyBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0xA3, 0x66));
    private static readonly Brush PExportCancelBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0x43, 0x43));

    internal static UIElement PExportDisplayBuild(
        string lPresetName,
        bool pPresetModified,
        bool pPresetUnsupported,
        RoutedEventHandler pApply,
        RoutedEventHandler pRestore)
    {
        UIElement pNameText = PExportNameBuild(lPresetName, pPresetModified, pPresetUnsupported);
        if (!pPresetModified)
        {
            return pNameText;
        }

        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pNameText, 0);
        pGrid.Children.Add(pNameText);

        var pButtonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pButtonPanel.Children.Add(PExportPresetInline.PExportInlineBuild(
            PExportCheckIcon,
            PExportApplyBrush,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.ApplyTooltip"),
            pApply));
        pButtonPanel.Children.Add(PExportPresetInline.PExportInlineBuild(
            PExportCancelIcon,
            PExportCancelBrush,
            QLocalizationCatalog.QLocalizationTextRead("ExportPreset.DiscardTooltip"),
            pRestore));
        Grid.SetColumn(pButtonPanel, 1);
        pGrid.Children.Add(pButtonPanel);
        return pGrid;
    }

    private static UIElement PExportNameBuild(string lPresetName, bool pPresetModified, bool pPresetUnsupported)
    {
        TextBlock pNameText = PExportTextBuild(lPresetName, pPresetModified, pPresetUnsupported);
        if (!LPresetTable.LPresetNativeCheck(lPresetName))
        {
            return pNameText;
        }

        var pPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pPanel.Children.Add(pNameText);
        pPanel.Children.Add(PExportBadgeBuild());
        return pPanel;
    }

    private static TextBlock PExportTextBuild(
        string lPresetName,
        bool pPresetModified,
        bool pPresetUnsupported)
    {
        string pTitle = PExportTitleFormat(lPresetName, pPresetModified, pPresetUnsupported);
        FontStyle pStyle = PExportStyleResolve(pPresetModified);
        return PExportLabelBuild(pTitle, pStyle);
    }

    private static string PExportTitleFormat(string lPresetName, bool pPresetModified, bool pPresetUnsupported) =>
        PExportMarkRead(
            pPresetModified
                ? $"{LPresetNaming.LPresetDisplayRead(lPresetName)} (Modified)"
                : LPresetNaming.LPresetDisplayRead(lPresetName),
            pPresetUnsupported);

    private static string PExportMarkRead(string lPresetText, bool lPresetUnsupported) =>
        lPresetUnsupported
            ? $"{lPresetText} {QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Incompatible")}"
            : lPresetText;

    private static FontStyle PExportStyleResolve(bool pPresetModified) =>
        pPresetModified ? FontStyles.Italic : FontStyles.Normal;

    private static TextBlock PExportLabelBuild(string pTitle, FontStyle pStyle) => new()
    {
        Text = pTitle,
        FontSize = 12,
        FontStyle = pStyle,
        Foreground = PExportTheme.PExportTextBrush,
        Padding = new Thickness(2, 0, 2, 1),
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Border PExportBadgeBuild() => new()
    {
        Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xEE, 0xF6)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(0xC8, 0xD4, 0xE2)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(3),
        Padding = new Thickness(5, 1, 5, 2),
        Margin = new Thickness(7, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Native"),
            FontSize = 10,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x5E, 0x75)),
            VerticalAlignment = VerticalAlignment.Center
        }
    };
}
