using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PHouse;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PExport
{
    private Border PExportGroupBuild(string pLabel, bool pCollapsed, Action pToggle)
    {
        var pGrid = new Grid();
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        pGrid.Children.Add(new TextBlock
        {
            Text = pLabel,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = PExportMutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        });

        var pToggleButton = new Button
        {
            Content = new Image
            {
                Width = 12,
                Height = 12,
                Source = PIcon.PIconRead(pCollapsed ? PExportExpandIcon : PExportCollapseIcon, PExportMutedBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = LLocalization.LLocalizationTextRead(pCollapsed
                ? "ExportPreset.Group.Expand"
                : "ExportPreset.Group.Collapse"),
            Width = 22,
            Height = 20,
            Style = PExportStyleRead()
        };
        pToggleButton.Click += (_, _) => pToggle();
        Grid.SetColumn(pToggleButton, 1);
        pGrid.Children.Add(pToggleButton);

        return new Border
        {
            Tag = "Header",
            Padding = new Thickness(12, 5, 8, 5),
            Background = Brushes.White,
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = pGrid
        };
    }

    private void PExportGroupToggle(string pGroupName)
    {
        bool pCollapsed = LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(pGroupName);
        LPreference.LPreferenceFoldSet(pGroupName, !pCollapsed);
        PExportPresetRebuild();
    }

    private void PExportUserToggle()
    {
        bool pCollapsed = LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(
            PExportUserGroup,
            false);
        LPreference.LPreferenceFoldSet(PExportUserGroup, !pCollapsed, false);
        PExportPresetRebuild();
    }

}
