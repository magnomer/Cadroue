using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PSensorSweep
{
    private readonly CheckBox pSensorRunPersistent;
    private readonly Button pSensorRunButton;
    private readonly ProgressBar pSensorProgress;

    internal PSensorSweep(QSensorSweep lSweep)
    {
        pSensorRunPersistent = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Detect.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Detect.PersistentTooltip"));
        pSensorRunPersistent.VerticalAlignment = VerticalAlignment.Center;

        pSensorRunButton = new Button
        {
            Content = lSweep.QSensorRunText,
            Height = 28,
            MinWidth = 90,
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            VerticalAlignment = VerticalAlignment.Center
        };
        QButton.QButtonAttach(pSensorRunButton, QButtonLook.QButtonLookWhite);

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

        PSensorRunRow = new Border
        {
            Padding = new Thickness(12, 6, 12, 8),
            BorderBrush = QPanel.QPanelLineRead(),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pSensorRunStack,
            Visibility = Visibility.Collapsed
        };
        PSensorRunAttach(lSweep);
    }

    internal Border PSensorRunRow { get; }

    private void PSensorRunAttach(QSensorSweep pSweep)
    {
        PInspectorSwitch.PInspectorSwitchAttach(pSensorRunPersistent, pSweep.QSensorPersistentSet);
        pSweep.QSensorPersistentChanged += pPersistent =>
            PInspectorSwitch.PInspectorSwitchUpdate(pSensorRunPersistent, pPersistent);
        pSweep.QSensorRunningChanged += _ => pSensorRunButton.Content = pSweep.QSensorRunText;
        pSensorRunButton.Click += (_, _) => pSweep.QSensorRunHandle();
    }

    public void PSensorRunShow() => PSensorRunRow.Visibility = Visibility.Visible;

    public void PSensorProgressSet(bool pSensorShown) => pSensorProgress.Visibility = PLook.PLookVisible[pSensorShown];

    public void PSensorProgressApply(double pSensorProgressValue) => pSensorProgress.Value = pSensorProgressValue;

    internal static ProgressBar PSensorProgressBuild() =>
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
