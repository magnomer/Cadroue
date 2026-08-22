using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private CheckBox pSensorRunPersistent = null!;
    private Button pSensorRunButton = null!;
    private Border pSensorRunRow = null!;

    public event Action? PSensorRun;
    public event Action<bool>? PSensorPersistentChange;

    private UIElement PSensorRunBuild()
    {
        pSensorRunPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Detect.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Detect.PersistentTooltip"));
        pSensorRunPersistent.VerticalAlignment = VerticalAlignment.Center;
        pSensorRunPersistent.Checked += (_, _) => PSensorPersistentChange?.Invoke(true);
        pSensorRunPersistent.Unchecked += (_, _) => PSensorPersistentChange?.Invoke(false);

        pSensorRunButton = new Button
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Detect.Run"),
            Height = 28,
            MinWidth = 90,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonWhiteCreate()
        };
        pSensorRunButton.Click += (_, _) => PSensorRun?.Invoke();

        var pSensorRunGrid = new Grid();
        pSensorRunGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pSensorRunGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pSensorRunButton, 1);
        pSensorRunGrid.Children.Add(pSensorRunPersistent);
        pSensorRunGrid.Children.Add(pSensorRunButton);

        pSensorRunRow = new Border
        {
            Padding = new Thickness(12, 6, 12, 8),
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pSensorRunGrid,
            Visibility = Visibility.Collapsed
        };
        return pSensorRunRow;
    }

    public void PSensorRunShow() => pSensorRunRow.Visibility = Visibility.Visible;

    public bool PSensorPersistentCheck() => pSensorRunPersistent.IsChecked == true;

    public void PSensorPersistentApply(bool pSensorPersistent) =>
        pSensorRunPersistent.IsChecked = pSensorPersistent;
}
