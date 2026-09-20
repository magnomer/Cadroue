using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private sealed record PSensorSection(
        LSensorPlan PSensorPlan,
        StackPanel PSensorBody,
        StackPanel PSensorStack,
        CheckBox PSensorSwitch,
        List<PInspectorRow> PSensorRows,
        List<RadioButton> PSensorModes,
        List<RadioButton> PSensorSpeeds,
        List<RadioButton> PSensorMetrics,
        ComboBox PSensorPreset);

    private readonly List<PSensorSection> pSensorSections;

    private CheckBox pSensorRunPersistent = null!;
    private Button pSensorRunButton = null!;
    private Border pSensorRunRow = null!;
    private ProgressBar pSensorProgress = null!;

    public LSensor LSensor => LInspector.LInspectorSensor;

    public LBlank LBlank => LInspector.LInspectorBlank;

    private PSensorSection PSensorBuild(LSensorPlan lPlan)
    {
        CheckBox pApply = PInspectorSwitchBuild(lPlan.LSensorPlanSwitch, lPlan.LSensorPlanTip);
        PInspectorSwitchAttach(pApply, pEnabled => LSensor.LSensorEnabledSet(lPlan.LSensorPlanKind, pEnabled));
        var pStack = new StackPanel();
        List<RadioButton> pMetrics = PSensorRadiosBuild(pStack, lPlan.LSensorPlanMetric, LSensor.LSensorMetricSelect);
        ComboBox pPreset = PInspectorChoiceBuild(
            LSensor.LSensorChoiceRead(lPlan.LSensorPlanKind).LInspectorChoiceNames,
            pIndex => LSensor.LSensorChoiceSelect(lPlan.LSensorPlanKind, pIndex));
        UIElement pPresetRow = PInspectorFieldBuild(lPlan.LSensorPlanChoice, pPreset);
        pPresetRow.Visibility = PLook.PLookVisible[lPlan.LSensorPlanChoice__B];
        pStack.Children.Add(pPresetRow);
        List<RadioButton> pSpeeds = PSensorRadiosBuild(pStack, lPlan.LSensorPlanSpeed, LSensor.LSensorSpeedSelect);
        List<PInspectorRow> pRows = lPlan.LSensorPlanRows.Select(lRow => PSensorRowBuild(lPlan, lRow)).ToList();
        pRows.ForEach(pRow => pStack.Children.Add(pRow.PInspectorRowGrid));
        List<RadioButton> pModes = PSensorRadiosBuild(pStack, lPlan.LSensorPlanMode, LSensor.LSensorModeSelect);

        var pBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);
        var pSection = new PSensorSection(lPlan, pBody, pStack, pApply, pRows, pModes, pSpeeds, pMetrics, pPreset);
        PSensorSectionUpdate(pSection);
        return pSection;
    }

    private PInspectorRow PSensorRowBuild(LSensorPlan lPlan, LInspectorRow lRow) => PInspectorRowBuild(
        lRow,
        () => LSensor.LSensorValueRead(lPlan.LSensorPlanKind, lRow.LInspectorRowIndex),
        () => LSensor.LSensorDefaultRead(lPlan.LSensorPlanKind, lRow.LInspectorRowIndex),
        pNumber => LSensor.LSensorValueSet(lPlan.LSensorPlanKind, lRow.LInspectorRowIndex, pNumber));

    private (Slider, TextBox) PSensorValueBuild(LDetectorBound lBound, Func<double> pRead, Action<double> pSet)
    {
        Slider pSlider = PInspectorSliderBuild(
            lBound.LDetectorBoundLeast, lBound.LDetectorBoundMost, pRead(), () => lBound.LDetectorBoundDefault);
        TextBox pValue = PInspectorDecimalBuild();
        PInspectorValueAttach(pSlider, pValue, lBound.LDetectorBoundLeast, lBound.LDetectorBoundMost, pRead, pSet);
        return (pSlider, pValue);
    }

    private void PSensorUpdate() => pSensorSections.ForEach(PSensorSectionUpdate);

    private void PSensorSectionUpdate(PSensorSection pSection)
    {
        LSensorPlan lPlan = pSection.PSensorPlan;
        bool lEnabled = LSensor.LSensorEnabledRead(lPlan.LSensorPlanKind);
        PInspectorSwitchUpdate(pSection.PSensorSwitch, lEnabled);
        pSection.PSensorRows.ForEach(pRow => PSensorRowUpdate(lPlan, pRow));
        pSection.PSensorModes[LSensor.LSensorModeIndex].IsChecked = true;
        pSection.PSensorSpeeds[LSensor.LSensorSpeedIndex].IsChecked = true;
        pSection.PSensorMetrics[LSensor.LSensorMetricIndex].IsChecked = true;
        PInspectorChoiceApply(pSection.PSensorPreset, LSensor.LSensorChoiceRead(lPlan.LSensorPlanKind));
        PInspectorSectionUpdate(pSection.PSensorStack, lEnabled);
    }

    private void PSensorRowUpdate(LSensorPlan lPlan, PInspectorRow pRow)
    {
        PInspectorRowUpdate(pRow, LSensor.LSensorValueRead(lPlan.LSensorPlanKind, pRow.PInspectorRowIndex));
        pRow.PInspectorRowUnit.Text = LSensor.LSensorUnitRead(lPlan.LSensorPlanKind, pRow.PInspectorRowIndex);
    }

    private static List<RadioButton> PSensorRadiosBuild(StackPanel pStack, LSensorGroup lGroup, Action<int> pSelect)
    {
        string pGroupName = $"PSensorGroup_{Guid.NewGuid():N}";
        List<RadioButton> pRadios = lGroup.LSensorGroupNames
            .Select((pName, pIndex) => PSensorRadioBuild(pName, pGroupName, () => pSelect(pIndex)))
            .ToList();
        Border pRow = PRadio.PRadioSegmentBuild(pRadios.ToArray());
        UIElement pField = PInspectorFieldBuild(lGroup.LSensorGroupLabel, pRow, true);
        pField.Visibility = PLook.PLookVisible[lGroup.LSensorGroupShown];
        pStack.Children.Add(pField);
        return pRadios;
    }

    private static RadioButton PSensorRadioBuild(string pText, string pGroup, Action pSelect)
    {
        var pRadio = new RadioButton
        {
            Content = pText,
            GroupName = pGroup,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        pRadio.Checked += (_, _) => pSelect();
        return pRadio;
    }

    private UIElement PSensorRunBuild()
    {
        pSensorRunPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Detect.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Detect.PersistentTooltip"));
        pSensorRunPersistent.VerticalAlignment = VerticalAlignment.Center;
        PInspectorSwitchAttach(pSensorRunPersistent, LSensor.LSensorPersistentSet);
        LSensor.LSensorPersistentChange += pPersistent => PInspectorSwitchUpdate(pSensorRunPersistent, pPersistent);
        LSensor.LSensorRunningChange += _ => pSensorRunButton.Content = LSensor.LSensorRunText;

        pSensorRunButton = new Button
        {
            Content = LSensor.LSensorRunText,
            Height = 28,
            MinWidth = 90,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonWhiteCreate()
        };
        pSensorRunButton.Click += (_, _) => LSensor.LSensorRunHandle();

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

    public void PSensorLockSet(bool pSensorLocked) => pInspectorSectionsHost.IsEnabled = !pSensorLocked;

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
