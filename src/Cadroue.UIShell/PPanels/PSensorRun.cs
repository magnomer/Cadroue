using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private CheckBox pSensorRunPersistent = null!;
    private Button pSensorRunButton = null!;
    private Border pSensorRunRow = null!;
    private ProgressBar pSensorProgress = null!;
    private bool pSensorRunning;

    public event Action? PSensorRun;
    public event Action? PSensorStop;
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
        pSensorRunButton.Click += (_, _) =>
        {
            if (pSensorRunning)
            {
                PSensorStop?.Invoke();
            }
            else
            {
                PSensorRun?.Invoke();
            }
        };

        var pSensorRunGrid = new Grid();
        pSensorRunGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pSensorRunGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pSensorRunButton, 1);
        pSensorRunGrid.Children.Add(pSensorRunPersistent);
        pSensorRunGrid.Children.Add(pSensorRunButton);

        pSensorProgress = PSensorProgressBuild();

        var pSensorRunStack = new StackPanel();
        pSensorRunStack.Children.Add(pSensorProgress);
        pSensorRunStack.Children.Add(pSensorRunGrid);

        pSensorRunRow = new Border
        {
            Padding = new Thickness(12, 6, 12, 8),
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pSensorRunStack,
            Visibility = Visibility.Collapsed
        };
        return pSensorRunRow;
    }

    public void PSensorRunShow() => pSensorRunRow.Visibility = Visibility.Visible;

    public bool PSensorPersistentCheck() => pSensorRunPersistent.IsChecked == true;

    public void PSensorPersistentApply(bool pSensorPersistent) =>
        pSensorRunPersistent.IsChecked = pSensorPersistent;

    public void PSensorLockSet(bool pSensorLocked) =>
        pInspectorSectionsHost.IsEnabled = !pSensorLocked;

    public void PSensorRunningSet(bool pSensorActive)
    {
        pSensorRunning = pSensorActive;
        pSensorRunButton.Content = LLocalization.LLocalizationTextRead(
            pSensorActive ? "Inspector.Detect.Stop" : "Inspector.Detect.Run");
    }

    public void PSensorProgressShow()
    {
        pSensorProgress.Value = 0;
        pSensorProgress.Visibility = Visibility.Visible;
    }

    public void PSensorProgressApply(double pSensorProgressValue) =>
        pSensorProgress.Value = pSensorProgressValue;

    public void PSensorProgressHide() => pSensorProgress.Visibility = Visibility.Collapsed;

    private static ProgressBar PSensorProgressBuild() =>
        new()
        {
            Minimum = 0,
            Maximum = 1,
            Height = 8,
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = null,
            Background = null,
            BorderThickness = new Thickness(0),
            Template = PSensorTemplateBuild(),
            Visibility = Visibility.Collapsed
        };

    private static ControlTemplate PSensorTemplateBuild()
    {
        const string pXaml = @"
<ControlTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 TargetType=""{x:Type ProgressBar}"">
    <Border CornerRadius=""4"" Background=""#E4E9F0"" ClipToBounds=""True"">
        <Grid>
            <Rectangle x:Name=""PART_Track"" />
            <Border x:Name=""PART_Indicator""
                    HorizontalAlignment=""Left""
                    CornerRadius=""4""
                    Background=""#4C86F7"" />
        </Grid>
    </Border>
</ControlTemplate>";
        return (ControlTemplate)XamlReader.Parse(pXaml);
    }
}
