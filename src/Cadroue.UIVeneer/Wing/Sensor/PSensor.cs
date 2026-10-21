using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed record PSensorSection(
    QSensorPlan PSensorPlan,
    StackPanel PSensorBody,
    StackPanel PSensorStack,
    CheckBox PSensorSwitch,
    List<PInspectorRow> PSensorRows,
    List<RadioButton> PSensorModes,
    List<RadioButton> PSensorSpeeds,
    List<RadioButton> PSensorMetrics,
    ComboBox PSensorPreset);

internal sealed class PSensor
{
    internal PSensor(QSensor lSensor)
    {
        QSensor = lSensor;
        PSensorSections = QSensor.QSensorPlans.Select(PSensorBuild).ToList();
    }

    private QSensor QSensor { get; }

    internal List<PSensorSection> PSensorSections { get; }

    private PSensorSection PSensorBuild(QSensorPlan lPlan)
    {
        PSensorSection pSection = PSensorSectionBuild(lPlan);
        PInspectorSwitch.PInspectorSwitchAttach(
            pSection.PSensorSwitch,
            pEnabled => QSensor.QSensorEnabledSet(lPlan.QSensorPlanKind, pEnabled));
        PSensorSectionUpdate(pSection);
        return pSection;
    }

    private PSensorSection PSensorSectionBuild(QSensorPlan lPlan)
    {
        CheckBox pApply = PInspectorSwitch.PInspectorSwitchBuild(lPlan.QSensorPlanSwitch, lPlan.QSensorPlanTip);
        var pStack = new StackPanel();
        List<RadioButton> pMetrics = PSensorRadiosBuild(pStack, lPlan.QSensorPlanMetric, QSensor.QSensorMetricSelect);
        ComboBox pPreset = PInspectorChoice.PInspectorChoiceBuild(
            QSensor.QSensorPreset.QSensorChoiceRead(lPlan.QSensorPlanKind).QInspectorChoiceNames,
            pIndex => QSensor.QSensorPreset.QSensorChoiceSelect(lPlan.QSensorPlanKind, pIndex));
        UIElement pPresetRow = PInspectorField.PInspectorFieldBuild(lPlan.QSensorPlanChoice, pPreset);
        pPresetRow.Visibility = PLook.PLookVisible[lPlan.QSensorPlanPreset];
        pStack.Children.Add(pPresetRow);
        List<RadioButton> pSpeeds = PSensorRadiosBuild(pStack, lPlan.QSensorPlanSpeed, QSensor.QSensorSpeedSelect);
        List<PInspectorRow> pRows = lPlan.QSensorPlanRows.Select(lRow => PSensorRowBuild(lPlan, lRow)).ToList();
        pRows.ForEach(pRow => pStack.Children.Add(pRow.PInspectorRowGrid));
        List<RadioButton> pModes = PSensorRadiosBuild(pStack, lPlan.QSensorPlanMode, QSensor.QSensorModeSelect);

        var pBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorField.PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);
        return new PSensorSection(lPlan, pBody, pStack, pApply, pRows, pModes, pSpeeds, pMetrics, pPreset);
    }

    private PInspectorRow PSensorRowBuild(QSensorPlan lPlan, QInspectorRow lRow) => PInspectorRow.PInspectorRowBuild(
        lRow,
        () => QSensor.QSensorValueRead(lPlan.QSensorPlanKind, lRow.QInspectorRowIndex),
        () => QSensor.QSensorDefaultRead(lPlan.QSensorPlanKind, lRow.QInspectorRowIndex),
        pNumber => QSensor.QSensorValueSet(lPlan.QSensorPlanKind, lRow.QInspectorRowIndex, pNumber));

    internal void PSensorUpdate() => PSensorSections.ForEach(PSensorSectionUpdate);

    private void PSensorSectionUpdate(PSensorSection pSection)
    {
        QSensorPlan lPlan = pSection.PSensorPlan;
        bool lEnabled = QSensor.QSensorEnabledRead(lPlan.QSensorPlanKind);
        PInspectorSwitch.PInspectorSwitchUpdate(pSection.PSensorSwitch, lEnabled);
        pSection.PSensorRows.ForEach(pRow => PSensorRowUpdate(lPlan, pRow));
        pSection.PSensorModes[QSensor.QSensorModeIndex].IsChecked = true;
        pSection.PSensorSpeeds[QSensor.QSensorSpeedIndex].IsChecked = true;
        pSection.PSensorMetrics[QSensor.QSensorMetricIndex].IsChecked = true;
        PInspectorChoice.PInspectorChoiceApply(
            pSection.PSensorPreset, QSensor.QSensorPreset.QSensorChoiceRead(lPlan.QSensorPlanKind));
        PInspectorState.PInspectorSectionUpdate(pSection.PSensorStack, lEnabled);
    }

    private void PSensorRowUpdate(QSensorPlan lPlan, PInspectorRow pRow)
    {
        pRow.PInspectorRowUpdate(QSensor.QSensorValueRead(lPlan.QSensorPlanKind, pRow.PInspectorRowIndex));
        pRow.PInspectorRowUnit.Text = QSensor.QSensorUnitRead(lPlan.QSensorPlanKind, pRow.PInspectorRowIndex);
    }

    private static List<RadioButton> PSensorRadiosBuild(StackPanel pStack, QSensorGroup lGroup, Action<int> pSelect)
    {
        string pGroupName = $"PSensorGroup_{Guid.NewGuid():N}";
        List<RadioButton> pRadios = lGroup.QSensorGroupNames
            .Select((pName, pIndex) => PSensorRadioBuild(pName, pGroupName, () => pSelect(pIndex)))
            .ToList();
        Border pRow = QRadio.QRadioSegmentBuild(pRadios.ToArray());
        UIElement pField = PInspectorField.PInspectorFieldBuild(lGroup.QSensorGroupLabel, pRow, true);
        pField.Visibility = PLook.PLookVisible[lGroup.QSensorGroupShown];
        pStack.Children.Add(pField);
        return pRadios;
    }

    internal static RadioButton PSensorRadioBuild(string pText, string pGroup, Action pSelect)
    {
        RadioButton pRadio = PSensorOptionBuild(pText, pGroup);
        PSensorOptionAttach(pRadio, pSelect);
        return pRadio;
    }

    private static RadioButton PSensorOptionBuild(string pText, string pGroup) => new()
    {
        Content = pText,
        GroupName = pGroup,
        FontSize = 12,
        FontFamily = QTool.QToolFontRead(),
        VerticalContentAlignment = VerticalAlignment.Center
    };

    private static void PSensorOptionAttach(RadioButton pRadio, Action pSelect) =>
        pRadio.Checked += (_, _) => pSelect();
}
