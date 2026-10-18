using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal static class PExportPresetGroup
{
    private const string PExportCollapseIcon = "/PAsset/PPanel/PExportCollapse.svg";
    private const string PExportExpandIcon = "/PAsset/PPanel/PExportExpand.svg";

    internal static Border PExportGroupBuild(string pLabel, bool pCollapsed, Action pToggle)
    {
        (string pIconPath, string pTipKey) = PExportFoldResolve(pCollapsed);
        Button pToggleButton = PExportFoldBuild(pIconPath, pTipKey);
        PExportToggleAttach(pToggleButton, pToggle);
        return PExportHeaderBuild(pLabel, pToggleButton);
    }

    private static (string pIconPath, string pIconTipKey) PExportFoldResolve(bool pCollapsed) =>
        pCollapsed
            ? (PExportExpandIcon, "ExportPreset.Group.Expand")
            : (PExportCollapseIcon, "ExportPreset.Group.Collapse");

    private static Button PExportFoldBuild(string pIconPath, string pTipKey)
    {
        var pToggleButton = new Button
        {
            Content = new Image
            {
                Width = 16,
                Height = 16,
                Source = QIcon.QIconRead(pIconPath, PExportTheme.PExportMutedBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = QLocalizationCatalog.QLocalizationTextRead(pTipKey),
            Width = 22,
            Height = 20
        };
        QButton.QButtonAttach(pToggleButton, QButtonLook.QButtonLookPanel);
        return pToggleButton;
    }

    private static void PExportToggleAttach(Button pToggleButton, Action pToggle) =>
        pToggleButton.Click += (_, _) => pToggle();

    private static Border PExportHeaderBuild(string pLabel, Button pToggleButton)
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        pGrid.Children.Add(new TextBlock
        {
            Text = pLabel,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = PExportTheme.PExportMutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        });

        Grid.SetColumn(pToggleButton, 1);
        pGrid.Children.Add(pToggleButton);

        return new Border
        {
            Tag = "Header",
            Padding = new Thickness(12, 5, 8, 5),
            Background = Brushes.White,
            BorderBrush = PExportTheme.PLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = pGrid
        };
    }
}
