using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

// The noise section's surface wiring: pulls a fresh sheet, maps its five fixed rows to the driver's ordered slots,
// hands every input to the driver, and asks the driver to paint the gate state.
public sealed class QNoiseSection
{
    private readonly QNoise qNoise;

    public QNoiseSection(QNoise lNoise)
    {
        qNoise = lNoise;
        QNoiseSectionBody = QContract.QContractSheetFind<StackPanel>("PNoise");
        QNoiseSectionPersistent = QContract.QContractSheetFind<CheckBox>("PNoisePersistent");

        CheckBox lActive = QContract.QContractFind<CheckBox>(QNoiseSectionBody, "PNoiseActive");
        StackPanel lStack = QContract.QContractFind<StackPanel>(QNoiseSectionBody, "PNoiseStack");
        ComboBox lPreset = QContract.QContractFind<ComboBox>(QNoiseSectionBody, "PNoisePreset");
        StackPanel lRows = QContract.QContractFind<StackPanel>(QNoiseSectionBody, "PNoiseRows");
        ComboBox lType = QContract.QContractFind<ComboBox>(QNoiseSectionBody, "PNoiseType");
        CheckBox lTrack = QContract.QContractFind<CheckBox>(QNoiseSectionBody, "PNoiseTrack");

        lActive.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        lActive.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.ApplyTooltip");
        QNoiseSectionPersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QNoiseSectionPersistent.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.PersistentTooltip");
        ((TextBlock)((Grid)lPreset.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset");
        ((TextBlock)((Grid)lType.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.Noise");
        lTrack.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.Track");
        lTrack.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Noise.TrackTooltip");

        QTool.QToolSwitchAttach(lActive, qNoise.QNoiseActiveSet);
        QTool.QToolSwitchAttach(QNoiseSectionPersistent, qNoise.QNoisePersistentSet);
        QTool.QToolSwitchAttach(lTrack, qNoise.QNoiseTrackSet);

        QDropdown.QDropdownAttach(lPreset);
        foreach (string lName in qNoise.QNoiseChoiceRead().QInspectorChoiceNames)
        {
            lPreset.Items.Add(lName);
        }

        QDropdown.QDropdownAttach(lType);
        foreach (string lName in qNoise.QNoiseTypeNames)
        {
            lType.Items.Add(lName);
        }

        ComboBox[] lChoices = { lPreset, lType };
        Action<int>[] lSelect = { qNoise.QNoiseChoiceSelect, qNoise.QNoiseTypeSelect };
        for (int lChoice = 0; lChoice < lChoices.Length; lChoice++)
        {
            int lIndex = lChoice;
            lChoices[lIndex].SelectionChanged +=
                (_, _) => lSelect[lIndex](lChoices[lIndex].SelectedIndex);
        }

        IReadOnlyList<QInspectorRow> lPlans = qNoise.QNoiseRows;
        for (int lSlot = 0; lSlot < lRows.Children.Count; lSlot++)
        {
            int lIndex = lSlot;
            QInspectorRow lPlan = lPlans[lIndex];
            Grid lRow = (Grid)lRows.Children[lIndex];
            ((TextBlock)lRow.Children[0]).Text = lPlan.QInspectorRowLabel;
            Slider lSlider = (Slider)lRow.Children[1];
            ((TextBlock)lRow.Children[2]).Text = lPlan.QInspectorRowUnit;
            TextBox lNumber = (TextBox)lRow.Children[3];

            lSlider.Minimum = lPlan.QInspectorRowLeast;
            lSlider.Maximum = lPlan.QInspectorRowMost;
            QSlider.QSliderAttach(lSlider);
            QSlider.QSliderResetAttach(lSlider, () => qNoise.QNoiseDefaults[lIndex]);
            QTextbox.QTextboxAttach(lNumber);
            lNumber.PreviewTextInput +=
                (_, lInput) => lInput.Handled = !QInspectorField.QInspectorDecimalCheck(lInput.Text);
            lSlider.ValueChanged += (_, _) => qNoise.QNoiseValueSet(lIndex, lSlider.Value);
            Action lCommit = () =>
            {
                lSlider.Value = QInspectorField.QInspectorValueCommit(
                    lNumber.Text,
                    qNoise.QNoiseValues[lIndex],
                    lSlider.Minimum,
                    lSlider.Maximum);
                lNumber.CaretIndex = lNumber.Text.Length;
            };
            lNumber.LostFocus += (_, _) => lCommit();
            lNumber.KeyDown += (_, lKey) =>
            {
                if (lKey.Key == Key.Enter)
                {
                    lCommit();
                }

                lKey.Handled = lKey.Key == Key.Enter;
            };
        }
    }

    public StackPanel QNoiseSectionBody { get; }

    public CheckBox QNoiseSectionPersistent { get; }

    public void QNoiseSectionAttach() => qNoise.QNoiseChanged += QNoiseSectionUpdate;

    public void QNoiseSectionUpdate()
    {
        StackPanel lRows = QContract.QContractFind<StackPanel>(QNoiseSectionBody, "PNoiseRows");
        qNoise.QNoiseUpdate(
            QContract.QContractFind<CheckBox>(QNoiseSectionBody, "PNoiseActive"),
            QNoiseSectionPersistent,
            QContract.QContractFind<CheckBox>(QNoiseSectionBody, "PNoiseTrack"),
            lRows.Children.Cast<Grid>()
                .Select(lRow => ((Slider)lRow.Children[1], (TextBox)lRow.Children[3]))
                .ToList(),
            QContract.QContractFind<ComboBox>(QNoiseSectionBody, "PNoiseType"),
            QContract.QContractFind<ComboBox>(QNoiseSectionBody, "PNoisePreset"),
            QContract.QContractFind<StackPanel>(QNoiseSectionBody, "PNoiseStack"));
    }
}
